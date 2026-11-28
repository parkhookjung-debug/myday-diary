"""Build the two native webview resources from one offline game source. No downloads."""
import argparse,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
folder=root/'shared/playroom'
html=(folder/'playroom.html').read_text(encoding='utf-8')
rules='window.MyDayMiniRules='+json.dumps(json.loads((root/'shared/mini-games.json').read_text()),ensure_ascii=False)+';\nwindow.MyDaySnackRules='+json.dumps(json.loads((root/'shared/snack-game.json').read_text()),ensure_ascii=False)+';'
rules+='\nwindow.MyDayPetRules='+json.dumps({key:value for key,value in json.loads((root/'shared/desktop.json').read_text(encoding='utf-8'))['limits'].items() if key.startswith('Pet')})+';'
for marker,content in [('RULES',rules),('ENGINE',(folder/'engine.js').read_text(encoding='utf-8')),('VIEW',(folder/'view.js').read_text(encoding='utf-8'))]:
    if '</script' in content.lower():raise ValueError('Embedded script must not terminate its element')
    html=html.replace('/*'+marker+'*/',content)
check=argparse.ArgumentParser();check.add_argument('--check',action='store_true');args=check.parse_args()
for relative in ['app/src/main/assets/playroom.html','macos/Sources/MyDay/Resources/playroom.html']:
    path=root/relative
    if args.check:
        if not path.exists() or path.read_text(encoding='utf-8')!=html:raise SystemExit('Run python tools/sync-playroom.py: '+relative)
    else:path.parent.mkdir(parents=True,exist_ok=True);path.write_text(html,encoding='utf-8',newline='\n')
print('Offline playroom resources match the shared source.' if args.check else 'Generated Android and Mac offline playrooms.')
