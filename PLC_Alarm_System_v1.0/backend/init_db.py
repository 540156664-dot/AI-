"""One-time database initialization. Only seeds data if DB is empty."""
from app.core.database import SessionLocal, engine, Base
from app.models.user import User
from app.models.plc_config import PLCConfig
from app.core.security import get_password_hash

Base.metadata.create_all(bind=engine)
db = SessionLocal()

# Create admin user if not exists
if not db.query(User).filter(User.username == 'admin').first():
    admin = User(
        username='admin', email='admin@example.com',
        hashed_password=get_password_hash('admin123'),
        is_admin=True, is_active=True
    )
    db.add(admin)
    db.commit()
    print("[Init] Admin user created: admin / admin123")
else:
    print("[Init] Admin user already exists")

# Create default PLC configs if table is empty
if db.query(PLCConfig).count() == 0:
    plcs = [
        PLCConfig(name="DefaultPLC", ip="127.0.0.1", rack=0, slot=1,
                  db_number=3000, start_byte=0, size=1024,
                  dict_path="plc_data/PLC-1/fault_dict.xlsx",
                  is_simulated=True, is_active=True),
        PLCConfig(name="PLC-2", ip="192.168.1.52", rack=0, slot=1,
                  db_number=3002, start_byte=0, size=1024,
                  dict_path="plc_data/PLC-2/fault_dict.xlsx",
                  is_simulated=True, is_active=True),
        PLCConfig(name="PLC-3", ip="192.168.1.53", rack=0, slot=1,
                  db_number=3003, start_byte=0, size=1024,
                  dict_path="plc_data/PLC-3/fault_dict.xlsx",
                  is_simulated=True, is_active=True),
    ]
    for p in plcs:
        db.add(p)
    db.commit()
    print(f"[Init] {len(plcs)} default PLC configs created")
else:
    print(f"[Init] PLC configs already exist ({db.query(PLCConfig).count()} total)")

db.close()
print("[Init] Database initialization complete")
