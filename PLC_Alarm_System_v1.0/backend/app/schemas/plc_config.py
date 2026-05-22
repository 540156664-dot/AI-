from pydantic import BaseModel, Field
from datetime import datetime
from typing import Optional


class PLCConfigBase(BaseModel):
    name: str = Field(..., max_length=50)
    ip: str = Field(..., max_length=15, pattern=r'^(\d{1,3}\.){3}\d{1,3}$')
    rack: int = Field(default=0, ge=0, le=15)
    slot: int = Field(default=1, ge=0, le=31)
    db_number: int = Field(..., ge=1, le=65535)
    start_byte: int = Field(default=0, ge=0)
    size: int = Field(default=1024, ge=1, le=65535)
    dict_path: Optional[str] = Field(None, max_length=500)
    is_simulated: bool = False
    is_active: bool = True


class PLCConfigCreate(PLCConfigBase):
    pass


class PLCConfigUpdate(BaseModel):
    name: Optional[str] = Field(None, max_length=50)
    ip: Optional[str] = Field(None, max_length=15, pattern=r'^(\d{1,3}\.){3}\d{1,3}$')
    rack: Optional[int] = Field(None, ge=0, le=15)
    slot: Optional[int] = Field(None, ge=0, le=31)
    db_number: Optional[int] = Field(None, ge=1, le=65535)
    start_byte: Optional[int] = Field(None, ge=0)
    size: Optional[int] = Field(None, ge=1, le=65535)
    dict_path: Optional[str] = Field(None, max_length=500)
    is_simulated: Optional[bool] = None
    is_active: Optional[bool] = None


class PLCConfigOut(PLCConfigBase):
    id: int
    connection_status: str = "unknown"
    last_seen: Optional[datetime] = None
    created_at: datetime

    class Config:
        from_attributes = True
