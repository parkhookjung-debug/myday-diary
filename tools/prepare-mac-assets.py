"""Stage the original companion only; keep old IDs in metadata for backup compatibility."""
from pathlib import Path
import json,shutil
root=Path(__file__).resolve().parents[1]
profile=json.loads((root/'shared/product.json').read_text(encoding='utf-8'))
source=root/'app/src/main/assets'
target=root/'macos/Sources/MyDay/Resources/Art'
target.mkdir(parents=True,exist_ok=True)
# These are generated resources under this exact workspace directory, never user records.
for name in ['portraits','items','equipment','sangmon']:
    old=(target/name).resolve()
    if old.parent!=target.resolve():raise ValueError('Unsafe generated resource path')
    if old.exists():shutil.rmtree(old)
(target/'sangmon').mkdir()
shutil.copy2(source/'sangmon'/f"{profile['characterId']}.png",target/'sangmon'/f"{profile['characterId']}.png")
shutil.copy2(source/'catalog.json',target/'catalog.json')
shutil.copytree(source/'studio',target/'studio',dirs_exist_ok=True)
(target/'fixtures').mkdir(exist_ok=True)
for fixture in ['windows-v8.json','windows-studio.json']:
    shutil.copy2(root/'app/src/test/resources'/fixture,target/'fixtures'/fixture)
print('Mac staged: one animated original companion; no outfits, alternate skins or item images.')
