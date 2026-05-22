from fastapi import APIRouter, Depends, Query, Request
from sqlalchemy.orm import Session
from typing import List, Optional
from pydantic import BaseModel
from datetime import datetime
from ....core.database import get_db
from ....core.audit import write_log
from ....crud.alarm_record import get_active_alarms, delete_old_alarm_records
from ....models.alarm_record import AlarmRecord
from ....collector.alarm_mapping import get_mapping, reload_mapping

router = APIRouter(prefix="/alarms", tags=["alarms"])


class AlarmStatus(BaseModel):
    alarm_code: str
    alarm_message: str
    is_active: bool
    plc_name: str = ""
    start_time: Optional[datetime] = None
    duration_seconds: Optional[int] = None

    class Config:
        from_attributes = True


@router.get("/status", response_model=List[AlarmStatus])
def all_alarm_status(plc_id: Optional[int] = Query(None), db: Session = Depends(get_db)):
    active_list = get_active_alarms(db, plc_id=plc_id)
    active_by_plc_code = {}  # {plc_id: {code: record}}
    for a in active_list:
        active_by_plc_code.setdefault(a.plc_id, {})[a.alarm_code] = a

    from ....models.plc_config import PLCConfig
    if plc_id is not None:
        target_plc_ids = [plc_id]
    elif active_by_plc_code:
        target_plc_ids = list(active_by_plc_code.keys())
    else:
        target_plc_ids = [p.id for p in db.query(PLCConfig).filter(PLCConfig.is_active == True).all()]
        if not target_plc_ids:
            target_plc_ids = [p.id for p in db.query(PLCConfig).all()]

    plcs = db.query(PLCConfig).filter(PLCConfig.id.in_(target_plc_ids)).all()
    plc_names = {p.id: p.name for p in plcs}
    result = []
    mapped_codes_per_plc = {}  # {plc_id: {code, ...}}

    for pid in target_plc_ids:
        mapping = get_mapping(pid)
        mapped_codes_per_plc[pid] = set()
        for (byte, bit), (code, msg) in mapping.items():
            mapped_codes_per_plc[pid].add(code)
            alarm_map = active_by_plc_code.get(pid, {})
            record = alarm_map.get(code)
            result.append(AlarmStatus(
                alarm_code=code,
                alarm_message=msg,
                is_active=record is not None,
                plc_name=plc_names.get(pid, ""),
                start_time=record.start_time if record else None,
                duration_seconds=record.duration_seconds if record else None
            ))

    for pid, alarm_map in active_by_plc_code.items():
        mapped_codes = mapped_codes_per_plc.get(pid, set())
        for code, record in alarm_map.items():
            if code not in mapped_codes:
                result.append(AlarmStatus(
                    alarm_code=code,
                    alarm_message=record.alarm_message,
                    is_active=True,
                    plc_name=plc_names.get(pid, ""),
                    start_time=record.start_time,
                    duration_seconds=record.duration_seconds
                ))

    return result


@router.post("/reload-dict")
def reload_alarm_dict(plc_id: int = Query(...), request: Request = None,
                       db: Session = Depends(get_db)):
    mapping = reload_mapping(plc_id)
    mapped_codes = {code for (code, _) in mapping.values()}
    stale = db.query(AlarmRecord).filter(
        AlarmRecord.plc_id == plc_id,
        AlarmRecord.is_active == True,
        AlarmRecord.alarm_code.notin_(mapped_codes)
    ).all()
    for r in stale:
        r.end_time = datetime.utcnow()
        r.duration_seconds = 0
        r.is_active = False
    if stale:
        db.commit()
    try:
        from ....collector.manager import get_collector_manager
        get_collector_manager().update_mapping(plc_id)
    except Exception:
        pass
    username = request.state.username if request else "system"
    write_log(username, "RELOAD_DICT", f"PLC_ID:{plc_id}",
              f"Loaded {len(mapping)} alarm codes, closed {len(stale)} stale records",
              request.state.client_ip if request else None)
    return {"message": f"PLC-{plc_id} 故障字典已重新加载，共 {len(mapping)} 条报警",
            "stale_closed": len(stale)}


@router.post("/cleanup")
def cleanup_old_records(months: int = Query(default=6, ge=1, le=24),
                        request: Request = None, db: Session = Depends(get_db)):
    deleted = delete_old_alarm_records(db, months=months)
    username = request.state.username if request else "system"
    write_log(username, "CLEANUP_DATA", f"months={months}",
              f"Deleted {deleted} records", request.state.client_ip if request else None)
    return {"message": f"Deleted {deleted} old alarm records", "deleted_count": deleted}
