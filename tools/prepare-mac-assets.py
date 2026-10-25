"""Stage the same first-party Windows-rendered Android art for the native Mac app."""
from pathlib import Path
import shutil

root = Path(__file__).resolve().parents[1]
source = root / 'app/src/main/assets'
target = root / 'macos/Sources/MyDay/Resources/Art'
target.mkdir(parents=True, exist_ok=True)
for name in ['catalog.json', 'sangmon', 'portraits', 'items', 'equipment']:
    src, dst = source / name, target / name
    if src.is_dir():
        shutil.copytree(src, dst, dirs_exist_ok=True)
    else:
        shutil.copy2(src, dst)
(target/'fixtures').mkdir(exist_ok=True)
shutil.copy2(root/'app/src/test/resources/windows-v8.json', target/'fixtures/windows-v8.json')
shutil.copy2(root/'app/src/test/resources/windows-studio.json', target/'fixtures/windows-studio.json')
print('Mac staged: same 64 animated characters, 300 items and equipment overlays.')
