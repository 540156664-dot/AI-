from sqlalchemy import Column, Integer, String, DateTime, Boolean
from sqlalchemy.sql import func
from ..core.database import Base

class PLCConfig(Base):
    __tablename__ = "plc_configs"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(50), unique=True, nullable=False)
    ip = Column(String(15), nullable=False)
    rack = Column(Integer, default=0)
    slot = Column(Integer, default=1)
    db_number = Column(Integer, nullable=False)
    start_byte = Column(Integer, default=0)
    size = Column(Integer, default=1024)
    dict_path = Column(String(500), nullable=True)
    is_simulated = Column(Boolean, default=False)
    is_active = Column(Boolean, default=True)
    connection_status = Column(String(200), default="offline")
    last_seen = Column(DateTime(timezone=True), nullable=True)
    created_at = Column(DateTime(timezone=True), server_default=func.now())
