from fastapi import APIRouter, Depends, Query
from fastapi.responses import StreamingResponse
from sqlalchemy.orm import Session
from typing import List, Optional
from ....core.database import get_db
from ....crud import statistics as stats_crud
from ....schemas.statistics import (
    StatisticsOverview, AlarmCountItem, DailyTrendItem,
    WeeklyReport, DurationItem
)

router = APIRouter(prefix="/statistics", tags=["statistics"])


@router.get("/overview", response_model=StatisticsOverview)
def get_overview(plc_id: Optional[int] = Query(None), db: Session = Depends(get_db)):
    return stats_crud.get_overview(db, plc_id=plc_id)


@router.get("/distribution", response_model=List[AlarmCountItem])
def get_distribution(plc_id: Optional[int] = Query(None), db: Session = Depends(get_db)):
    return stats_crud.get_alarm_distribution(db, plc_id=plc_id)


@router.get("/trend", response_model=List[DailyTrendItem])
def get_trend(days: int = Query(default=7, ge=1, le=90),
              plc_id: Optional[int] = Query(None),
              db: Session = Depends(get_db)):
    return stats_crud.get_daily_trend(db, days=days, plc_id=plc_id)


@router.get("/weekly", response_model=WeeklyReport)
def get_weekly(week_start: str = Query(...),
               plc_id: Optional[int] = Query(None),
               top_n: int = Query(default=20, ge=1, le=100),
               db: Session = Depends(get_db)):
    report = stats_crud.get_weekly_report(db, week_start, plc_id=plc_id)
    report["top_alarms"] = report["top_alarms"][:top_n]
    return report


@router.get("/duration", response_model=List[DurationItem])
def get_duration(week_start: str = Query(...),
                 plc_id: Optional[int] = Query(None),
                 db: Session = Depends(get_db)):
    return stats_crud.get_duration_stats(db, week_start, plc_id=plc_id)


@router.get("/export")
def export(week_start: str = Query(...),
           plc_id: Optional[int] = Query(None),
           sort_by: str = Query(default="count"),
           db: Session = Depends(get_db)):
    xlsx = stats_crud.export_weekly_excel(db, week_start, plc_id=plc_id, sort_by=sort_by)
    return StreamingResponse(
        xlsx,
        media_type="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        headers={"Content-Disposition": f"attachment; filename=alarm_report_{week_start}.xlsx"}
    )
