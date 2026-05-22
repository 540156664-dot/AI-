import asyncio
import aioschedule
from ..core.database import SessionLocal
from ..crud.alarm_record import delete_old_alarm_records


async def cleanup_job():
    db = SessionLocal()
    try:
        deleted = delete_old_alarm_records(db, months=6)
        if deleted > 0:
            print(f"[Scheduler] Cleaned up {deleted} old alarm records")
    except Exception as e:
        print(f"[Scheduler] Cleanup error: {e}")
    finally:
        db.close()


async def start_scheduler():
    aioschedule.every().day.at("03:00").do(cleanup_job)
    print("[Scheduler] Daily cleanup job scheduled at 03:00")
    while True:
        await aioschedule.run_pending()
        await asyncio.sleep(60)


def start_scheduler_sync():
    asyncio.create_task(start_scheduler())
