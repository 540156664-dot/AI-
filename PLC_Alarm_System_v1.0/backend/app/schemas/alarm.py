from pydantic import BaseModel
from datetime import datetime
from typing import Optional

class AlarmRecordBase(BaseModel):
    plc_id: Optional[int] = None
    alarm_code: str
    alarm_message: str
    start_time: datetime
    end_time: Optional[datetime] = None
    duration_seconds: Optional[int] = None
    is_active: bool

class AlarmRecordCreate(AlarmRecordBase):
    pass

class AlarmRecordOut(AlarmRecordBase):
    id: int

    class Config:
        from_attributes = True
