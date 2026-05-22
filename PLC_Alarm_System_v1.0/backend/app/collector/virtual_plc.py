import threading
import time
import random


class RealS7Plc:
    def __init__(self, ip, rack, slot, db_number, size=1024):
        self.ip = ip
        self.rack = rack
        self.slot = slot
        self.db_number = db_number
        self.db_size = size
        self.lock = threading.Lock()
        self._client = None
        self._connected = False
        self.status = "offline"
        self.last_error = None

    def connect(self):
        try:
            import snap7
            if self._client:
                try:
                    self._client.disconnect()
                except Exception:
                    pass
                self._client = None
            self._client = snap7.client.Client()
            self._client.connect(self.ip, self.rack, self.slot)
            self._connected = True
            self.status = "online"
            self.last_error = None
            return True
        except Exception as e:
            self._connected = False
            self.status = "conn_failed"
            self.last_error = str(e)
            return False

    def read_db(self, start, size):
        if not self._connected:
            if not self.connect():
                return bytes(min(size, self.db_size))
        try:
            with self.lock:
                data = self._client.db_read(self.db_number, start, size)
                self._connected = True
                self.status = "online"
                self.last_error = None
                result = bytes(data)
                if len(result) < size:
                    result += bytes(size - len(result))
                return result
        except Exception as e:
            self._connected = False
            try:
                self.connect()
                data = self._client.db_read(self.db_number, 0, 1)
                self._connected = True
                self.status = "degraded"
                self.last_error = f"DB{self.db_number} limited, requested {size} bytes failed"
                result = bytes(data)
                if len(result) < size:
                    result += bytes(size - len(result))
                return result
            except Exception as e2:
                self._connected = False
                self.status = "read_error"
                self.last_error = str(e2)
                return bytes(min(size, self.db_size))

    def stop(self):
        try:
            if self._client:
                self._client.disconnect()
        except Exception:
            pass
        self._connected = False
        self.status = "offline"
        self.last_error = None


class VirtualS7Plc:
    def __init__(self, db_number=3000, size=1024, alarm_bits=None):
        self.db_number = db_number
        self.db_size = size
        self.db_data = bytearray(size)
        self.lock = threading.Lock()
        self.running = True
        self.alarm_bits = alarm_bits or []
        self._init_random_bits()
        self.sim_thread = threading.Thread(target=self._simulate, daemon=True)
        self.sim_thread.start()

    def _init_random_bits(self):
        for i in range(0, min(32, self.db_size)):
            self.db_data[i] = random.randint(0, 255)

    def _simulate(self):
        while self.running:
            time.sleep(random.uniform(0.3, 1.0))
            if not self.alarm_bits:
                continue
            byte_idx, bit_idx = random.choice(self.alarm_bits)
            if byte_idx >= self.db_size:
                continue
            with self.lock:
                current = (self.db_data[byte_idx] >> bit_idx) & 1
                if current:
                    self.db_data[byte_idx] &= ~(1 << bit_idx)
                else:
                    self.db_data[byte_idx] |= (1 << bit_idx)

    def read_db(self, start, size):
        with self.lock:
            end = min(start + size, self.db_size)
            return bytes(self.db_data[start:end])

    def stop(self):
        self.running = False
