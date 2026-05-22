from pydantic import BaseModel
from typing import List, Optional


class AlarmCountItem(BaseModel):
    alarm_code: str
    alarm_message: str
    count: int


class StatisticsOverview(BaseModel):
    total_alarms: int
    active_alarms: int
    today_alarms: int
    today_resolved: int
    avg_duration_seconds: Optional[float] = None


class DailyTrendItem(BaseModel):
    date: str
    count: int


class WeeklyAlarmItem(BaseModel):
    alarm_code: str
    alarm_message: str
    this_week_count: int
    last_week_count: int
    change_percent: float


class WeeklyReport(BaseModel):
    top_alarms: List[WeeklyAlarmItem]


class DurationItem(BaseModel):
    alarm_code: str
    alarm_message: str = ""
    total_duration_sec: int
