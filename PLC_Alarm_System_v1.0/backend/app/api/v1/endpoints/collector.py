from fastapi import APIRouter, Request, Query
from pydantic import BaseModel
from typing import List

from ....core.audit import write_log
from ....collector.manager import get_collector_manager
from ....collector.data_generator import generate_week_data

router = APIRouter(prefix="/collector", tags=["collector"])


class CollectorInfo(BaseModel):
    plc_id: int
    plc_name: str
    running: bool
    is_simulated: bool
    status: str = ""
    last_error: str = ""


class CollectorStatus(BaseModel):
    running: bool
    collector_count: int
    collectors: List[CollectorInfo]


class GenerateResult(BaseModel):
    message: str
    record_count: int


@router.get("/status", response_model=CollectorStatus)
def get_status():
    return get_collector_manager().get_status()


@router.post("/start")
def start_collector(request: Request):
    mgr = get_collector_manager()
    mgr.start_all()
    write_log(request.state.username, "COLLECTOR_START", "Collector",
              "Started all collectors", request.state.client_ip)
    return {"message": "Collector started", "status": mgr.get_status()}


@router.post("/stop")
def stop_collector(request: Request):
    mgr = get_collector_manager()
    mgr.stop_all()
    write_log(request.state.username, "COLLECTOR_STOP", "Collector",
              "Stopped all collectors", request.state.client_ip)
    return {"message": "Collector stopped", "status": mgr.get_status()}


@router.post("/generate-data", response_model=GenerateResult)
def generate_data(request: Request, days: int = Query(default=7, ge=1, le=365)):
    count = generate_week_data(days=days)
    write_log(request.state.username, "GENERATE_DATA", "Simulation",
              f"Generated {count} records for {days} days", request.state.client_ip)
    return {"message": f"Generated {count} alarm records for {days} days", "record_count": count}
