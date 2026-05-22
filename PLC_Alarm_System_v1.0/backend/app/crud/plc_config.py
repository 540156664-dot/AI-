from sqlalchemy.orm import Session
from typing import List, Optional
from ..models.plc_config import PLCConfig
from ..schemas.plc_config import PLCConfigCreate, PLCConfigUpdate


def get_plc_configs(db: Session, skip: int = 0, limit: int = 100) -> List[PLCConfig]:
    return db.query(PLCConfig).offset(skip).limit(limit).all()


def get_plc_config_by_id(db: Session, plc_id: int) -> Optional[PLCConfig]:
    return db.query(PLCConfig).filter(PLCConfig.id == plc_id).first()


def get_plc_config_by_name(db: Session, name: str) -> Optional[PLCConfig]:
    return db.query(PLCConfig).filter(PLCConfig.name == name).first()


def create_plc_config(db: Session, config: PLCConfigCreate) -> PLCConfig:
    db_config = PLCConfig(**config.model_dump())
    db.add(db_config)
    db.commit()
    db.refresh(db_config)
    return db_config


def update_plc_config(db: Session, plc_id: int, config: PLCConfigUpdate) -> Optional[PLCConfig]:
    db_config = get_plc_config_by_id(db, plc_id)
    if not db_config:
        return None
    update_data = config.model_dump(exclude_unset=True)
    for field, value in update_data.items():
        setattr(db_config, field, value)
    db.commit()
    db.refresh(db_config)
    return db_config


def delete_plc_config(db: Session, plc_id: int) -> bool:
    db_config = get_plc_config_by_id(db, plc_id)
    if not db_config:
        return False
    from ..models.alarm_record import AlarmRecord
    db.query(AlarmRecord).filter(AlarmRecord.plc_id == plc_id).delete()
    db.delete(db_config)
    db.commit()
    return True


def get_active_plc_configs(db: Session) -> List[PLCConfig]:
    return db.query(PLCConfig).filter(PLCConfig.is_active == True).all()
