from datetime import datetime, timedelta
import random
from app.core.database import SessionLocal
from app.models.alarm_record import AlarmRecord
from app.models.plc_config import PLCConfig
from app.collector.alarm_mapping import get_mapping

db = SessionLocal()

# Ensure a default PLC config exists
existing_plc = db.query(PLCConfig).first()
if not existing_plc:
    plc = PLCConfig(
        name="DefaultPLC",
        ip="127.0.0.1",
        rack=0,
        slot=1,
        db_number=3000,
        start_byte=0,
        size=1024,
        dict_path="plc_data/PLC-1/fault_dict.xlsx",
        is_simulated=True,
        is_active=True
    )
    db.add(plc)
    db.commit()
    db.refresh(plc)
    plc_id = plc.id
    print(f"Created default PLC config (ID={plc_id})")
else:
    plc_id = existing_plc.id

alarm_list = list(get_mapping(plc_id).values())

# Create 2 more PLC configs for testing
for i in range(2, 4):
    name = f"PLC-{i}"
    if not db.query(PLCConfig).filter(PLCConfig.name == name).first():
        p = PLCConfig(
            name=name,
            ip=f"192.168.1.{i + 50}",
            rack=0, slot=1,
            db_number=3000 + i,
            start_byte=0, size=1024,
            is_simulated=True,
            is_active=True
        )
        db.add(p)
        db.commit()
        print(f"Created PLC config: {name}")

# Clear old data
db.query(AlarmRecord).delete()
db.commit()

end_date = datetime.now().replace(hour=0, minute=0, second=0, microsecond=0)
start_date = end_date - timedelta(days=30)

# Designate resolved/improved for demo
resolved_codes = [alarm_list[i] for i in [0, 3, 7]]
improved_codes = [alarm_list[i] for i in [2, 5, 9]]
weekly_cutoff = end_date - timedelta(days=7)

# Collect all active PLC IDs for random distribution
all_plc_ids = [p.id for p in db.query(PLCConfig).filter(PLCConfig.is_active == True).all()]
if not all_plc_ids:
    all_plc_ids = [plc_id]

records = []
current = start_date
while current < end_date:
    num = random.randint(30, 80)
    for _ in range(num):
        is_this_week = current >= weekly_cutoff
        assigned_plc = random.choice(all_plc_ids)

        if is_this_week:
            candidates = alarm_list.copy()
            for rc in resolved_codes:
                if rc in candidates:
                    candidates.remove(rc)
            weighted = []
            for c in candidates:
                weight = 1 if c not in improved_codes else 0.25
                weighted.extend([c] * int(weight * 100))
            code, msg = random.choice(weighted)
        else:
            code, msg = random.choice(alarm_list)

        start_time = current + timedelta(
            hours=random.randint(0, 23),
            minutes=random.randint(0, 59),
            seconds=random.randint(0, 59)
        )
        duration = random.randint(10, 1800)
        end_time = start_time + timedelta(seconds=duration)
        records.append(AlarmRecord(
            plc_id=assigned_plc,
            alarm_code=code,
            alarm_message=msg,
            start_time=start_time,
            end_time=end_time,
            duration_seconds=duration,
            is_active=False
        ))
    current += timedelta(days=1)

db.bulk_save_objects(records)
db.commit()
print(f"Inserted {len(records)} alarm records ({start_date.date()} to {end_date.date()})")
print(f"Green (resolved this week): {[c[0] for c in resolved_codes]}")
print(f"Yellow (improved >50%): {[c[0] for c in improved_codes]}")
db.close()
