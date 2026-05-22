import time
import threading
from datetime import datetime
from typing import Dict
from sqlalchemy.orm import Session
from ..core.database import SessionLocal
from ..models.alarm_record import AlarmRecord
from ..schemas.alarm import AlarmRecordCreate
from .alarm_mapping import get_mapping
from .virtual_plc import VirtualS7Plc, RealS7Plc


class AlarmCollector:
    def __init__(self, plc_id: int, plc_name: str, plc_ip: str,
                 rack: int, slot: int, db_number: int,
                 start_byte: int, size: int, interval: float = 1.0,
                 is_simulated: bool = True):
        self.plc_id = plc_id
        self.plc_name = plc_name
        self.plc_ip = plc_ip
        self.rack = rack
        self.slot = slot
        self.db_number = db_number
        self.start_byte = start_byte
        self.size = size
        self.interval = interval
        self.running = False
        self.previous_states: Dict = {}
        self.is_simulated = is_simulated
        self.thread = None
        try:
            self.mapping = get_mapping(plc_id)
        except FileNotFoundError as e:
            print(f"[Collector:{plc_name}] Warning: {e}, using empty mapping")
            self.mapping = {}
        alarm_bits = list(self.mapping.keys()) if self.mapping else []
        if self.is_simulated:
            self.client = VirtualS7Plc(db_number, size, alarm_bits)
        else:
            self.client = RealS7Plc(plc_ip, rack, slot, db_number, size)

    def _read_alarm_bits(self):
        data = self.client.read_db(self.start_byte, self.size)
        states = {}
        for (byte, bit), (code, msg) in self.mapping.items():
            if byte < len(data):
                byte_val = data[byte]
                state = (byte_val >> bit) & 1
                states[(byte, bit)] = bool(state)
        if not self.is_simulated:
            for byte_idx in range(len(data)):
                byte_val = data[byte_idx]
                for bit_idx in range(8):
                    if (byte_val >> bit_idx) & 1:
                        key = (byte_idx, bit_idx)
                        if key not in states:
                            code = f"B{byte_idx}.{bit_idx}"
                            msg = f"未定义报警 (Byte={byte_idx}, Bit={bit_idx})"
                            states[key] = True
                            self.mapping[key] = (code, msg)
        return states

    def _run_once(self, db: Session):
        current_states = self._read_alarm_bits()
        now = datetime.utcnow()
        is_first_run = len(self.previous_states) == 0
        for key, active in current_states.items():
            prev = self.previous_states.get(key, False)
            if active and not prev:
                code, msg = self.mapping[key]
                if not is_first_run or not db.query(AlarmRecord).filter(
                    AlarmRecord.plc_id == self.plc_id,
                    AlarmRecord.alarm_code == code,
                    AlarmRecord.is_active == True
                ).first():
                    record = AlarmRecordCreate(
                        plc_id=self.plc_id,
                        alarm_code=code,
                        alarm_message=msg,
                        start_time=now,
                        end_time=None,
                        duration_seconds=None,
                        is_active=True
                    )
                    db_record = AlarmRecord(**record.model_dump())
                    db.add(db_record)
                    db.commit()
            elif not active and prev:
                code, msg = self.mapping[key]
                record = db.query(AlarmRecord).filter(
                    AlarmRecord.plc_id == self.plc_id,
                    AlarmRecord.alarm_code == code,
                    AlarmRecord.end_time.is_(None)
                ).order_by(AlarmRecord.start_time.desc()).first()
                if record:
                    record.end_time = now
                    record.duration_seconds = int((now - record.start_time).total_seconds())
                    record.is_active = False
                    db.commit()
            self.previous_states[key] = active

        if is_first_run:
            mapped_codes = {code for (code, _) in self.mapping.values()}
            stale = db.query(AlarmRecord).filter(
                AlarmRecord.plc_id == self.plc_id,
                AlarmRecord.is_active == True,
                AlarmRecord.alarm_code.notin_(mapped_codes)
            ).all()
            for r in stale:
                r.end_time = now
                r.duration_seconds = 0
                r.is_active = False
            if stale:
                db.commit()

    def _get_current_status(self):
        if self.is_simulated:
            return "online"
        if hasattr(self.client, 'status'):
            return self.client.status
        return "offline"

    def _update_status(self, status: str):
        db = SessionLocal()
        try:
            from ..models.plc_config import PLCConfig
            from datetime import datetime as dt
            plc = db.query(PLCConfig).filter(PLCConfig.id == self.plc_id).first()
            if plc:
                plc.connection_status = status
                plc.last_seen = dt.utcnow()
                db.commit()
        except Exception as e:
            print(f"[Collector:{self.plc_name}] Status update error: {e}")
        finally:
            db.close()

    def _loop(self):
        while self.running:
            db = SessionLocal()
            try:
                self._run_once(db)
                status = self._get_current_status()
                if not self.is_simulated and hasattr(self.client, 'last_error') and self.client.last_error:
                    status = status + ": " + str(self.client.last_error)[:80]
                self._update_status(status)
            except Exception as e:
                print(f"[Collector:{self.plc_name}] Error: {e}")
            finally:
                db.close()
            time.sleep(self.interval)

    def start(self):
        self.running = True
        self.thread = threading.Thread(target=self._loop, daemon=True)
        self.thread.start()
        self._update_status(self._get_current_status())
        print(f"[Collector:{self.plc_name}] Started (PLC ID={self.plc_id}, {'simulated' if self.is_simulated else self.plc_ip}:DB{self.db_number})")

    def stop(self):
        self.running = False
        now = datetime.utcnow()
        db = SessionLocal()
        try:
            active_records = db.query(AlarmRecord).filter(
                AlarmRecord.plc_id == self.plc_id,
                AlarmRecord.is_active == True
            ).all()
            for r in active_records:
                r.end_time = now
                r.duration_seconds = int((now - r.start_time).total_seconds()) if r.start_time else 0
                r.is_active = False
            if active_records:
                db.commit()
        except Exception as e:
            print(f"[Collector:{self.plc_name}] Error closing alarms: {e}")
        finally:
            db.close()
        self._update_status("offline")
        if hasattr(self.client, 'stop'):
            self.client.stop()
        print(f"[Collector:{self.plc_name}] Stopped")


class CollectorManager:
    def __init__(self):
        self.collectors: Dict[int, AlarmCollector] = {}
        self._config_snapshot: Dict[int, tuple] = {}
        self.lock = threading.Lock()
        self._reload_thread = None
        self._reload_running = False

    @staticmethod
    def _config_key(plc) -> tuple:
        return (plc.name, plc.ip, plc.rack, plc.slot, plc.db_number,
                plc.start_byte, plc.size, plc.dict_path or '',
                plc.is_simulated if plc.is_simulated else False)

    def reload_from_db(self):
        from ..crud import plc_config as plc_crud
        db = SessionLocal()
        try:
            active_plcs = plc_crud.get_active_plc_configs(db)
            active_ids = {p.id for p in active_plcs}

            with self.lock:
                current_ids = set(self.collectors.keys())
                for pid in current_ids - active_ids:
                    self.collectors[pid].stop()
                    del self.collectors[pid]
                    self._config_snapshot.pop(pid, None)

                for plc in active_plcs:
                    key = self._config_key(plc)
                    if plc.id in self.collectors:
                        if self._config_snapshot.get(plc.id) != key:
                            self.collectors[plc.id].stop()
                            del self.collectors[plc.id]
                        else:
                            continue
                    collector = AlarmCollector(
                        plc_id=plc.id,
                        plc_name=plc.name,
                        plc_ip=plc.ip,
                        rack=plc.rack,
                        slot=plc.slot,
                        db_number=plc.db_number,
                        start_byte=plc.start_byte,
                        size=plc.size,
                        interval=1.0,
                        is_simulated=plc.is_simulated if plc.is_simulated else False
                    )
                    collector.start()
                    self.collectors[plc.id] = collector
                    self._config_snapshot[plc.id] = key
        finally:
            db.close()

    def _reload_loop(self):
        while self._reload_running:
            time.sleep(15)
            if self._reload_running:
                self.reload_from_db()

    def start_all(self):
        self.reload_from_db()
        if not self._reload_running:
            self._reload_running = True
            self._reload_thread = threading.Thread(target=self._reload_loop, daemon=True)
            self._reload_thread.start()

    def stop_all(self):
        self._reload_running = False
        with self.lock:
            for c in self.collectors.values():
                c.stop()
            self.collectors.clear()

    @property
    def is_running(self):
        with self.lock:
            return any(c.running for c in self.collectors.values())

    def update_mapping(self, plc_id: int):
        from .alarm_mapping import get_mapping
        new_mapping = get_mapping(plc_id)
        with self.lock:
            collector = self.collectors.get(plc_id)
            if collector:
                collector.mapping = new_mapping

    def get_status(self):
        with self.lock:
            collectors = [{
                "plc_id": c.plc_id,
                "plc_name": c.plc_name,
                "running": c.running,
                "is_simulated": c.is_simulated,
                "status": c._get_current_status(),
                "last_error": str(c.client.last_error)[:100] if hasattr(c.client, 'last_error') and c.client.last_error else ""
            } for c in self.collectors.values()]
        return {
            "running": any(c["running"] for c in collectors),
            "collector_count": len(collectors),
            "collectors": collectors
        }
