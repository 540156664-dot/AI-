from app.core.database import SessionLocal
from app.models.user import User
from app.core.security import get_password_hash

db = SessionLocal()

existing = db.query(User).filter(User.username == 'admin').first()
if not existing:
    admin = User(
        username='admin',
        email='admin@example.com',
        hashed_password=get_password_hash('admin123'),
        is_admin=True,
        is_active=True
    )
    db.add(admin)
    db.commit()
    print("✅ 管理员用户创建成功: admin / admin123")
else:
    print("⚠️ 用户已存在，无需创建")

db.close()