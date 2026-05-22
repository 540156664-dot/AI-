from sqlalchemy.orm import Session
from typing import Optional, List
from ..models.alarm_record import AlarmRecord
from ..schemas.alarm import AlarmRecordCreate
from datetime import datetime, timedelta


def create_alarm_record(db: Session, record: AlarmRecordCreate):
    db_record = AlarmRecord(**record.model_dump())
    db.add(db_record)
    db.commit()
    db.refresh(db_record)
    return db_record


def get_active_alarms(db: Session, plc_id: Optional[int] = None,
                      skip: int = 0, limit: int = 500) -> List[AlarmRecord]:
    query = db.query(AlarmRecord).filter(AlarmRecord.is_active == True)
    if plc_id is not None:
        query = query.filter(AlarmRecord.plc_id == plc_id)
    return query.order_by(AlarmRecord.start_time.desc()).offset(skip).limit(limit).all()


def get_alarm_history(db: Session, plc_id: Optional[int] = None,
                      skip: int = 0, limit: int = 100) -> List[AlarmRecord]:
    query = db.query(AlarmRecord)
    if plc_id is not None:
        query = query.filter(AlarmRecord.plc_id == plc_id)
    return query.order_by(AlarmRecord.start_time.desc()).offset(skip).limit(limit).all()


def update_alarm_end_time(db: Session, alarm_code: str, plc_id: Optional[int],
                          start_time: datetime, end_time: datetime):
    query = db.query(AlarmRecord).filter(
        AlarmRecord.alarm_code == alarm_code,
        AlarmRecord.start_time == start_time,
        AlarmRecord.end_time.is_(None)
    )
    if plc_id is not None:
        query = query.filter(AlarmRecord.plc_id == plc_id)
    record = query.first()
    if record:
        record.end_time = end_time
        record.duration_seconds = int((end_time - record.start_time).total_seconds())
        record.is_active = False
        db.commit()
        return record
    return None


def delete_old_alarm_records(db: Session, months: int = 6) -> int:
    cutoff = datetime.utcnow() - timedelta(days=months * 30)
    count = db.query(AlarmRecord).filter(AlarmRecord.start_time < cutoff).delete()
    db.commit()
    return count
