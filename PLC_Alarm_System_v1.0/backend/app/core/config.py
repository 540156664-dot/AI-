from pydantic_settings import BaseSettings
from typing import Optional

class Settings(BaseSettings):
    DATABASE_URL: str = "sqlite:///./alarm_system.db"
    SECRET_KEY: str = "plc-alarm-system-secret-key-2024"
    ALGORITHM: str = "HS256"
    ACCESS_TOKEN_EXPIRE_MINUTES: int = 1440

    PLC_IP: str = "127.0.0.1"
    PLC_RACK: int = 0
    PLC_SLOT: int = 1
    PLC_DB_NUMBER: int = 3000
    PLC_START_BYTE: int = 0
    PLC_SIZE: int = 1024
    COLLECTOR_INTERVAL: float = 1.0

    class Config:
        env_file = ".env"
        case_sensitive = True

settings = Settings()
