from datetime import datetime, timedelta
import random
from ..core.database import SessionLocal
from ..models.alarm_record import AlarmRecord
from ..models.plc_config import PLCConfig
from .alarm_mapping import get_mapping, _parse_excel, _get_default_dict_path


def _get_alarm_list(plc_id: int, dict_path: str = None):
    try:
        mapping = get_mapping(plc_id)
        if mapping:
            return list(mapping.values())
    except Exception:
        pass
    return []


def generate_week_data(days: int = 7) -> int:
    db = SessionLocal()
    try:
        plc_configs = db.query(PLCConfig).filter(PLCConfig.is_active == True).all()
        if not plc_configs:
            plc = PLCConfig(
                name="DefaultPLC", ip="127.0.0.1", rack=0, slot=1,
                db_number=3000, start_byte=0, size=1024,
                dict_path="plc_data/PLC-1/fault_dict.xlsx",
                is_simulated=True, is_active=True
            )
            db.add(plc)
            db.commit()
            db.refresh(plc)
            plc_configs = [plc]

        valid_plcs = []
        for p in plc_configs:
            alarm_list = _get_alarm_list(p.id, p.dict_path)
            if alarm_list:
                valid_plcs.append((p.id, alarm_list))

        if not valid_plcs:
            default_path = _get_default_dict_path(1)
            try:
                mapping = _parse_excel(default_path)
                if mapping:
                    plc = db.query(PLCConfig).filter(PLCConfig.name == "DefaultPLC").first()
                    if not plc:
                        plc = PLCConfig(
                            name="DefaultPLC", ip="127.0.0.1", rack=0, slot=1,
                            db_number=3000, start_byte=0, size=1024,
                            dict_path="plc_data/PLC-1/fault_dict.xlsx",
                            is_simulated=True, is_active=True
                        )
                        db.add(plc)
                        db.commit()
                        db.refresh(plc)
                    valid_plcs = [(plc.id, list(mapping.values()))]
            except Exception:
                return 0

        end_date = datetime.now().replace(hour=0, minute=0, second=0, microsecond=0)
        start_date = end_date - timedelta(days=days)

        records = []
        current = start_date
        while current < end_date:
            num = random.randint(30, 80)
            plc_id, alarm_list = random.choice(valid_plcs)
            for _ in range(num):
                code, msg = random.choice(alarm_list)
                start_time = current + timedelta(
                    hours=random.randint(0, 23),
                    minutes=random.randint(0, 59),
                    seconds=random.randint(0, 59)
                )
                duration = random.randint(10, 1800)
                end_time = start_time + timedelta(seconds=duration)
                records.append(AlarmRecord(
                    plc_id=plc_id,
                    alarm_code=code,
                    alarm_message=msg,
                    start_time=start_time,
                    end_time=end_time,
                    duration_seconds=duration,
                    is_active=False
                ))
            current += timedelta(days=1)

        db.bulk_save_objects(records)
        db.commit()
        return len(records)
    finally:
        db.close()
