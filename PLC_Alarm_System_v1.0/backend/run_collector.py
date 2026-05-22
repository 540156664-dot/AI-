import os
import sys
import time
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from app.collector.snap7_collector import CollectorManager

if __name__ == "__main__":
    manager = CollectorManager()
    manager.start_all()
    print("Multi-PLC collector started. Press Ctrl+C to stop.")
    try:
        while True:
            time.sleep(30)
            manager.reload_from_db()
    except KeyboardInterrupt:
        manager.stop_all()
        print("Collector stopped.")
