#!/usr/bin/env python3
"""Compatibility entry point; desktop generation now reads shared/."""
from pathlib import Path
import subprocess
import sys

root=Path(__file__).resolve().parents[2]
raise SystemExit(subprocess.call([sys.executable,str(root/'tools/sync-desktop.py'),*sys.argv[1:]],cwd=root))
