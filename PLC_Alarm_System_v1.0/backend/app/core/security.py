from datetime import datetime, timedelta
from typing import Optional
from jose import JWTError, jwt
import hashlib
import secrets
from .config import settings

# 简单密码哈希（仅开发用，生产请用 bcrypt）
def verify_password(plain_password, hashed_password):
    return hash_password(plain_password) == hashed_password

def hash_password(password):
    # 加盐（固定盐仅用于演示，生产应使用随机盐）
    salt = "plc_alarm_salt_2026"
    return hashlib.sha256((password + salt).encode()).hexdigest()

def get_password_hash(password):
    return hash_password(password)

def create_access_token(data: dict, expires_delta: Optional[timedelta] = None):
    to_encode = data.copy()
    if expires_delta:
        expire = datetime.utcnow() + expires_delta
    else:
        expire = datetime.utcnow() + timedelta(minutes=settings.ACCESS_TOKEN_EXPIRE_MINUTES)
    to_encode.update({"exp": expire})
    encoded_jwt = jwt.encode(to_encode, settings.SECRET_KEY, algorithm=settings.ALGORITHM)
    return encoded_jwt