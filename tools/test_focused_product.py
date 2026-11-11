"""Product packaging contract: no network or user data. APK checking is optional."""
import json,unittest,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class FocusedProductTests(unittest.TestCase):
 def test_shared_profile_and_asset_allowlist(self):
  p=json.loads((ROOT/'shared/product.json').read_text(encoding='utf-8'))
  self.assertEqual('original',p['characterId']);self.assertIn('local-chat',p['retained']);self.assertIn('kakao-integration-source',p['retained'])
  self.assertFalse(p['externalFriendsEnabled'])
  self.assertEqual(['sangmon/original.png'],[x for x in p['assetIncludes'] if x.startswith('sangmon/')])
  self.assertFalse(any(x.startswith(('items/','equipment/','portraits/')) for x in p['assetIncludes']))
 def test_local_staged_mac_art_when_available(self):
  p=ROOT/'macos/Sources/MyDay/Resources/Art'
  if not p.exists():self.skipTest('Run tools/prepare-mac-assets.py to stage Mac art')
  self.assertEqual(['original.png'],sorted(x.name for x in (p/'sangmon').iterdir()))
  for name in ('portraits','items','equipment'):self.assertFalse((p/name).exists())
 def test_packaged_android_art_when_available(self):
  p=ROOT/'app/build/outputs/apk/debug/app-debug.apk'
  if not p.exists():self.skipTest('Build Android APK first')
  with zipfile.ZipFile(p) as z:
   names=z.namelist();self.assertEqual(['assets/sangmon/original.png'],[n for n in names if n.startswith('assets/sangmon/')])
   self.assertFalse(any(n.startswith(('assets/items/','assets/equipment/','assets/portraits/')) for n in names))
   self.assertIn('assets/catalog.json',names);self.assertIn('assets/device-link.html',names)
if __name__=='__main__':unittest.main()
