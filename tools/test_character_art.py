"""Native painter provenance and the one atlas shipped to both sprite consumers."""
import hashlib
import json
import struct
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


class CharacterArtTests(unittest.TestCase):
    def test_shared_atlas_identity_and_reference(self):
        art = json.loads((ROOT / 'shared/character-art.json').read_text(encoding='utf-8'))
        profile = json.loads((ROOT / 'shared/product.json').read_text(encoding='utf-8'))
        self.assertEqual(profile['characterId'], art['characterId'])
        self.assertTrue(art['backupIdentityUnchanged'])
        self.assertFalse(art['reference']['rawVideoBundled'])
        atlas = (ROOT / art['atlas']['path']).read_bytes()
        self.assertEqual(art['atlas']['sha256'], hashlib.sha256(atlas).hexdigest())
        self.assertEqual(b'\x89PNG\r\n\x1a\n', atlas[:8])
        self.assertEqual((1536, 1536), struct.unpack('>II', atlas[16:24]))
        self.assertEqual(8, len(art['atlas']['rows']))
        staged = ROOT / 'macos/Sources/MyDay/Resources/Art/sangmon/original.png'
        if staged.exists():
            self.assertEqual(atlas, staged.read_bytes(), 'Mac must stage the same new atlas')


if __name__ == '__main__':
    unittest.main()
