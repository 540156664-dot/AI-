from sqlalchemy import Column, Integer, String, DateTime, Boolean, Index, ForeignKey
from sqlalchemy.sql import func
from ..core.database import Base

class AlarmRecord(Base):
    __tablename__ = "alarm_records"

    id = Column(Integer, primary_key=True, index=True)
    plc_id = Column(Integer, ForeignKey("plc_configs.id", ondelete="SET NULL"), nullable=True, index=True)
    alarm_code = Column(String(20), index=True, nullable=False)
    alarm_message = Column(String(200), nullable=False)
    start_time = Column(DateTime(timezone=True), index=True, nullable=False)
    end_time = Column(DateTime(timezone=True), nullable=True)
    duration_seconds = Column(Integer, nullable=True)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime(timezone=True), server_default=func.now())

    __table_args__ = (
        Index('idx_alarm_start', 'alarm_code', 'start_time'),
        Index('idx_alarm_plc_active', 'plc_id', 'is_active'),
    )
