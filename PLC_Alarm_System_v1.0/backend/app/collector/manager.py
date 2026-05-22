from .snap7_collector import CollectorManager

_collector_manager = None


def get_collector_manager():
    global _collector_manager
    if _collector_manager is None:
        _collector_manager = CollectorManager()
    return _collector_manager
