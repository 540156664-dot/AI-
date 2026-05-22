import os
import glob
import shutil
from pathlib import Path
from fastapi import APIRouter, Depends, HTTPException, status, Query, Request, UploadFile, File
from sqlalchemy.orm import Session
from typing import List

from ....core.database import get_db
from ....core.audit import write_log
from ....crud import plc_config as plc_crud
from ....schemas.plc_config import PLCConfigCreate, PLCConfigUpdate, PLCConfigOut
from ....collector.alarm_mapping import BASE_DATA_DIR

router = APIRouter(prefix="/plc-configs", tags=["plc-configs"])

UPLOAD_DIR = os.path.join(BASE_DATA_DIR, "uploads")


@router.get("/dict-files")
def list_dict_files():
    files = []
    base_parent = os.path.join(BASE_DATA_DIR, "..")
    for ext in ["fault_dict.xlsx", "*.db"]:
        pattern = os.path.join(BASE_DATA_DIR, "**", ext)
        for f in glob.glob(pattern, recursive=True):
            if os.path.basename(f) == 'alarm_system.db':
                continue
            rel = os.path.relpath(f, base_parent)
            files.append(rel.replace("\\", "/"))
    if not files:
        default = os.path.join(BASE_DATA_DIR, "default", "fault_dict.xlsx")
        if os.path.exists(default):
            files.append("plc_data/default/fault_dict.xlsx")
    return {"files": sorted(files)}


@router.post("/upload-dict")
def upload_dict_file(file: UploadFile = File(...), request: Request = None):
    if not file.filename:
        raise HTTPException(status_code=400, detail="No file selected")
    ext = os.path.splitext(file.filename)[1].lower()
    if ext not in ('.xlsx', '.db'):
        raise HTTPException(status_code=400, detail="Only .xlsx or .db files allowed")
    os.makedirs(UPLOAD_DIR, exist_ok=True)
    safe_name = Path(file.filename).name
    dest = os.path.join(UPLOAD_DIR, safe_name)
    with open(dest, "wb") as f:
        shutil.copyfileobj(file.file, f)
    rel = os.path.relpath(dest, os.path.join(BASE_DATA_DIR, "..")).replace("\\", "/")
    username = request.state.username if request else "system"
    write_log(username, "UPLOAD_DICT", f"File:{safe_name}", rel, request.state.client_ip if request else None)
    return {"message": f"Uploaded {safe_name}", "path": rel}


@router.get("/", response_model=List[PLCConfigOut])
def list_plc_configs(
    skip: int = Query(default=0, ge=0),
    limit: int = Query(default=100, ge=1, le=200),
    db: Session = Depends(get_db)
):
    return plc_crud.get_plc_configs(db, skip=skip, limit=limit)


@router.get("/{plc_id}", response_model=PLCConfigOut)
def get_plc_config(plc_id: int, db: Session = Depends(get_db)):
    plc = plc_crud.get_plc_config_by_id(db, plc_id)
    if not plc:
        raise HTTPException(status_code=404, detail="PLC config not found")
    return plc


@router.post("/", response_model=PLCConfigOut, status_code=status.HTTP_201_CREATED)
def create_plc_config(config: PLCConfigCreate, request: Request, db: Session = Depends(get_db)):
    existing = plc_crud.get_plc_config_by_name(db, config.name)
    if existing:
        raise HTTPException(status_code=400, detail="PLC name already exists")
    result = plc_crud.create_plc_config(db, config)
    write_log(request.state.username, "CREATE_PLC", f"PLC:{config.name}",
              f"IP={config.ip}, DB={config.db_number}", request.state.client_ip)
    return result


@router.put("/{plc_id}", response_model=PLCConfigOut)
def update_plc_config(plc_id: int, config: PLCConfigUpdate, request: Request, db: Session = Depends(get_db)):
    old = plc_crud.get_plc_config_by_id(db, plc_id)
    if not old:
        raise HTTPException(status_code=404, detail="PLC config not found")
    if config.name:
        existing_name = plc_crud.get_plc_config_by_name(db, config.name)
        if existing_name and existing_name.id != plc_id:
            raise HTTPException(status_code=400, detail="PLC name already exists")
    updated = plc_crud.update_plc_config(db, plc_id, config)
    changes = config.model_dump(exclude_unset=True)
    write_log(request.state.username, "UPDATE_PLC", f"PLC:{old.name}",
              str(changes), request.state.client_ip)
    return updated


@router.delete("/{plc_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_plc_config(plc_id: int, request: Request, db: Session = Depends(get_db)):
    plc = plc_crud.get_plc_config_by_id(db, plc_id)
    if not plc:
        raise HTTPException(status_code=404, detail="PLC config not found")
    plc_crud.delete_plc_config(db, plc_id)
    write_log(request.state.username, "DELETE_PLC", f"PLC:{plc.name}",
              f"IP={plc.ip}", request.state.client_ip)
