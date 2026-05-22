from contextlib import asynccontextmanager
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from starlette.middleware.base import BaseHTTPMiddleware
from slowapi import Limiter
from slowapi.util import get_remote_address
from slowapi.middleware import SlowAPIMiddleware
from jose import jwt, JWTError
from .core.config import settings
from .core.database import engine, Base
from .api.v1.endpoints import auth, statistics, alarms, plc_configs, audit_logs, collector

Base.metadata.create_all(bind=engine)


class AuditMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request: Request, call_next):
        username = "anonymous"
        auth_header = request.headers.get("Authorization", "")
        if auth_header.startswith("Bearer "):
            try:
                token = auth_header[7:]
                payload = jwt.decode(token, settings.SECRET_KEY, algorithms=[settings.ALGORITHM])
                username = payload.get("sub", "anonymous")
            except JWTError:
                pass
        request.state.username = username
        request.state.client_ip = request.client.host if request.client else ""
        response = await call_next(request)
        return response


def _init_data():
    from .core.database import SessionLocal
    from .models.user import User
    from .models.plc_config import PLCConfig
    from .core.security import get_password_hash
    db = SessionLocal()
    try:
        if not db.query(User).filter(User.username == 'admin').first():
            db.add(User(username='admin', email='admin@example.com',
                       hashed_password=get_password_hash('admin123'),
                       is_admin=True, is_active=True))
            db.commit()
        if db.query(PLCConfig).count() == 0:
            db.add_all([
                PLCConfig(name="DefaultPLC", ip="127.0.0.1", db_number=3000,
                          dict_path="plc_data/PLC-1/fault_dict.xlsx",
                          is_simulated=True, is_active=True),
                PLCConfig(name="PLC-2", ip="192.168.1.52", db_number=3002,
                          dict_path="plc_data/PLC-2/fault_dict.xlsx",
                          is_simulated=True, is_active=True),
                PLCConfig(name="PLC-3", ip="192.168.1.53", db_number=3003,
                          dict_path="plc_data/PLC-3/fault_dict.xlsx",
                          is_simulated=True, is_active=True),
            ])
            db.commit()
    finally:
        db.close()


@asynccontextmanager
async def lifespan(app: FastAPI):
    _init_data()
    from .core.scheduler import start_scheduler_sync
    start_scheduler_sync()
    from .collector.manager import get_collector_manager
    get_collector_manager().start_all()
    yield
    get_collector_manager().stop_all()


app = FastAPI(title="PLC Alarm System", version="1.0", lifespan=lifespan)

limiter = Limiter(key_func=get_remote_address, default_limits=["120/minute"])
app.state.limiter = limiter
app.add_middleware(AuditMiddleware)
app.add_middleware(SlowAPIMiddleware)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173", "http://localhost:8080"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(auth.router, prefix="/api/v1")
app.include_router(statistics.router, prefix="/api/v1")
app.include_router(alarms.router, prefix="/api/v1")
app.include_router(plc_configs.router, prefix="/api/v1")
app.include_router(audit_logs.router, prefix="/api/v1")
app.include_router(collector.router, prefix="/api/v1")


@app.get("/")
def root():
    return {"message": "PLC Alarm System API"}

