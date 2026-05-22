from ..core.database import SessionLocal
from ..models.audit_log import AuditLog


def write_log(username: str, action: str, target: str, detail: str = None, ip_address: str = None):
    db = SessionLocal()
    try:
        entry = AuditLog(
            username=username,
            action=action,
            target=target,
            detail=detail,
            ip_address=ip_address
        )
        db.add(entry)
        db.commit()
    except Exception as e:
        print(f"[Audit] Failed to write log: {e}")
    finally:
        db.close()


def get_current_username(request=None) -> str:
    """Extract username from request state or return 'system'."""
    try:
        if request and hasattr(request, 'state'):
            return getattr(request.state, 'username', 'system')
    except Exception:
        pass
    return 'system'
