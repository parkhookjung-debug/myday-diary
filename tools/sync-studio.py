"""Generate the same offline studio catalog for all three native apps."""
import json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];data=json.loads((root/'shared/studio.json').read_text(encoding='utf-8'))
keys=['questions','furnitureKinds','furnitureNames','furnitureSymbols','moods','stickers']
assert len(data['furnitureKinds'])==len(data['furnitureNames'])==len(data['furnitureSymbols'])==6
assert len(data['moods'])==5 and len(data['questions'])>=7
assert all(isinstance(s,str) and 0<len(s)<=200 for key in keys for s in data[key])
arr=lambda k:','.join(json.dumps(x,ensure_ascii=False) for x in data[k])
cs='namespace MyDay.Windows.Core { public static class StudioCatalog {\n'+''.join('public static readonly string[] '+k[0].upper()+k[1:]+'={'+arr(k)+'};\n' for k in keys)+'}}\n'
kt='package com.myday.diary.data\nobject StudioCatalog {\n'+''.join('val '+k+'=listOf('+arr(k)+')\n' for k in keys)+'}\n'
sw='enum StudioCatalog {\n'+''.join('static let '+k+'=['+arr(k)+']\n' for k in keys)+'}\n'
outputs={'windows/Core/Shared/StudioCatalog.g.cs':cs,'app/src/main/java/com/myday/diary/data/StudioCatalog.g.kt':kt,'macos/Sources/MyDay/StudioCatalog.g.swift':sw}
for path,value in outputs.items():
 p=root/path
 if '--check' in sys.argv:
  if not p.exists() or p.read_text(encoding='utf-8')!=value:raise SystemExit('Out of date: '+path)
 else:p.parent.mkdir(parents=True,exist_ok=True);p.write_text(value,encoding='utf-8')
