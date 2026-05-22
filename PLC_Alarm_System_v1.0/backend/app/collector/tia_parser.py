import re
import os
from typing import Dict, List, Tuple, Optional


class TiaDbParser:
    def __init__(self, filepath: str = None):
        self.db_name = ""
        self.optimized = False
        self.version = ""
        self.bit_defs: List[dict] = []
        self.byte_size = 0
        if filepath:
            self.parse(filepath)

    def parse(self, filepath: str) -> Dict:
        with open(filepath, 'r', encoding='utf-8-sig') as f:
            content = f.read()

        self.db_name = self._extract_db_name(content)
        self.optimized = self._extract_optimized(content)
        self.bit_defs = self._extract_bool_arrays(content)
        self.byte_size = (len(self.bit_defs) + 7) // 8 if self.bit_defs else 0
        if self.byte_size == 0:
            self.byte_size = 2
        return self.to_dict()

    def _extract_db_name(self, content: str) -> str:
        m = re.search(r'DATA_BLOCK\s+"([^"]+)"', content)
        return m.group(1) if m else ""

    def _extract_optimized(self, content: str) -> bool:
        m = re.search(r"S7_Optimized_Access\s*:=\s*'(\w+)'", content)
        if m:
            return m.group(1).upper() == 'TRUE'
        return False

    def _extract_bool_arrays(self, content: str) -> List[dict]:
        struct_match = re.search(r'STRUCT(.*?)END_STRUCT', content, re.DOTALL)
        if not struct_match:
            return []

        struct_body = struct_match.group(1)
        bit_defs = []
        global_bit_offset = 0

        for line in struct_body.split('\n'):
            line = line.strip()
            if not line or line.startswith('//'):
                continue

            array_match = re.match(
                r'(\w+)\s*:\s*Array\s*\[(\d+)\.\.(\d+)\]\s*of\s+Bool\s*;',
                line, re.IGNORECASE
            )
            if array_match:
                name = array_match.group(1)
                start = int(array_match.group(2))
                end = int(array_match.group(3))
                count = end - start + 1
                if not self.optimized:
                    for i in range(count):
                        byte_idx = global_bit_offset // 8
                        bit_idx = global_bit_offset % 8
                        bit_defs.append({
                            "index": start + i,
                            "name": f"{name}[{start + i}]",
                            "byte": byte_idx,
                            "bit": bit_idx,
                            "code": f"A{start + i:03d}",
                            "msg": f"{name}[{start + i}]"
                        })
                        global_bit_offset += 1
                else:
                    for i in range(count):
                        byte_idx = i
                        bit_idx = 0
                        bit_defs.append({
                            "index": start + i,
                            "name": f"{name}[{start + i}]",
                            "byte": byte_idx,
                            "bit": bit_idx,
                            "code": f"A{start + i:03d}",
                            "msg": f"{name}[{start + i}]"
                        })

            single_match = re.match(r'(\w+)\s*:\s*Bool\s*;', line, re.IGNORECASE)
            if single_match:
                name = single_match.group(1)
                byte_idx = global_bit_offset // 8
                bit_idx = global_bit_offset % 8
                bit_defs.append({
                    "index": global_bit_offset,
                    "name": name,
                    "byte": byte_idx,
                    "bit": bit_idx,
                    "code": f"B{byte_idx}.{bit_idx}",
                    "msg": name
                })
                global_bit_offset += 1

        return bit_defs

    def to_dict(self) -> Dict:
        return {
            "db_name": self.db_name,
            "optimized": self.optimized,
            "byte_size": self.byte_size,
            "bit_count": len(self.bit_defs),
            "bits": self.bit_defs
        }

    def to_fault_mapping(self) -> Dict[Tuple[int, int], Tuple[str, str]]:
        return {
            (b["byte"], b["bit"]): (b["code"], b["msg"])
            for b in self.bit_defs
        }


def scan_db_files(base_dir: str) -> List[str]:
    files = []
    for root, dirs, filenames in os.walk(base_dir):
        for f in filenames:
            if f.endswith('.db') and f != 'alarm_system.db':
                files.append(os.path.join(root, f))
    return sorted(files)
