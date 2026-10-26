import importlib.util,json,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('experience',ROOT/'tools/sync-experience.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
class ExperienceTests(unittest.TestCase):
    def test_native_outputs_match(self):m.sync(check=True)
    def test_secondary_text_contrast(self):
        p=json.loads((ROOT/'shared/desktop.json').read_text(encoding='utf-8'))['design']
        def lum(hex):
            rgb=[int(hex[i:i+2],16)/255 for i in (1,3,5)];c=[v/12.92 if v<=0.04045 else ((v+0.055)/1.055)**2.4 for v in rgb]
            return sum(a*b for a,b in zip(c,[.2126,.7152,.0722]))
        text=lum(p['Muted'])
        for color in [p['Paper'],p['Sidebar'],p['FriendCard']]+p['Backgrounds']:
            self.assertGreaterEqual((lum(color)+.05)/(text+.05),4.5,color)
    def test_help_uses_existing_navigation(self):
        p=json.loads((ROOT/'shared/experience.json').read_text(encoding='utf-8'));shell=json.loads((ROOT/'shared/app-shell.json').read_text(encoding='utf-8'))
        for nav in shell['navigation']:self.assertIn(nav['title'],p['sections'][1]['body'])
