"""Contract generation checks, without building apps or reading private diaries."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('sync_desktop', ROOT/'tools/sync-desktop.py')
sync=importlib.util.module_from_spec(spec)
spec.loader.exec_module(sync)

class DesktopGenerationTests(unittest.TestCase):
    def fixture(self, directory):
        root=Path(directory)
        for relative in ['desktop.json','collection.json','app-shell.json','catalog/templates.json','fixtures/layout.json']:
            target=root/'shared'/relative
            target.parent.mkdir(parents=True,exist_ok=True)
            target.write_bytes((ROOT/'shared'/relative).read_bytes())
        return root

    def test_committed_outputs_match_current_sources(self):
        sync.sync(check=True)

    def test_one_prompt_edit_reaches_both_native_apps(self):
        with tempfile.TemporaryDirectory(prefix='myday-desktop-contract-') as directory:
            root=self.fixture(directory)
            path=root/'shared/catalog/templates.json'
            templates=json.loads(path.read_text(encoding='utf-8'))
            prompt='함께 고친 질문 🌱'
            templates[0]['sections'][0]['prompt']=prompt
            path.write_text(json.dumps(templates,ensure_ascii=False),encoding='utf-8')
            sync.sync(root)
            self.assertIn(prompt,(root/'windows/Core/Shared/Templates.g.cs').read_text(encoding='utf-8'))
            self.assertEqual(prompt,json.loads((root/'macos/Sources/MyDay/Resources/templates.json').read_text(encoding='utf-8'))[0]['sections'][0]['prompt'])
            self.assertEqual([],sync.sync(root))

    def test_direct_edit_to_generated_native_file_fails_check(self):
        with tempfile.TemporaryDirectory(prefix='myday-desktop-contract-') as directory:
            root=self.fixture(directory)
            sync.sync(root)
            path=root/'macos/Sources/MyDay/SharedDesktop.g.swift'
            path.write_text(path.read_text(encoding='utf-8').replace('chatMaxText=2000','chatMaxText=3000'),encoding='utf-8')
            with self.assertRaises(ValueError): sync.sync(root,check=True)

    def test_duplicate_ids_and_invalid_layouts_are_rejected_before_writing(self):
        for mutation in ['duplicate','layout']:
            with tempfile.TemporaryDirectory(prefix='myday-desktop-contract-') as directory:
                root=self.fixture(directory)
                path=root/'shared/catalog/templates.json'
                templates=json.loads(path.read_text(encoding='utf-8'))
                if mutation=='duplicate': templates[1]['id']=templates[0]['id']
                else: templates[0]['layout']='unsupported-layout'
                path.write_text(json.dumps(templates,ensure_ascii=False),encoding='utf-8')
                with self.assertRaises(ValueError): sync.sync(root)
                self.assertFalse((root/'windows').exists())

    def test_utf16_title_limit_includes_surrogate_pairs(self):
        with tempfile.TemporaryDirectory(prefix='myday-desktop-contract-') as directory:
            root=self.fixture(directory)
            path=root/'shared/catalog/templates.json'
            templates=json.loads(path.read_text(encoding='utf-8'))
            templates[0]['sections'][0]['title']='🌱'*41
            path.write_text(json.dumps(templates,ensure_ascii=False),encoding='utf-8')
            with self.assertRaises(ValueError): sync.sync(root)

    def test_growth_bonus_change_reaches_windows_android_and_mac_constants(self):
        with tempfile.TemporaryDirectory(prefix='myday-growth-contract-') as directory:
            root=self.fixture(directory)
            path=root/'shared/desktop.json'
            contract=json.loads(path.read_text(encoding='utf-8'))
            contract['growth']['GrowthAttendanceWeekXP']=20
            path.write_text(json.dumps(contract,ensure_ascii=False),encoding='utf-8')
            sync.sync(root)
            for name in ['windows/Core/Shared/DesktopContract.g.cs','app/src/main/java/com/myday/diary/diary/SharedGrowth.g.kt','macos/Sources/MyDay/SharedDesktop.g.swift']:
                self.assertIn('growthattendanceweekxp=20',(root/name).read_text(encoding='utf-8').lower())

    def test_collection_policy_reaches_both_platforms_and_rejects_duplicate_forms(self):
        with tempfile.TemporaryDirectory(prefix='myday-collection-policy-') as directory:
            root=self.fixture(directory);path=root/'shared/collection.json'
            p=json.loads(path.read_text(encoding='utf-8'));p['synthesisPercent'][0]=61
            path.write_text(json.dumps(p,ensure_ascii=False),encoding='utf-8');sync.sync(root)
            self.assertIn('61,40,25',(root/'windows/Core/Shared/CollectionPolicy.g.cs').read_text(encoding='utf-8'))
            self.assertIn('61,40,25',(root/'app/src/main/java/com/myday/diary/diary/SharedCollection.g.kt').read_text(encoding='utf-8'))
            self.assertIn('61,40,25',(root/'macos/Sources/MyDay/CollectionPolicy.g.swift').read_text(encoding='utf-8'))
            p['forms'][0][0]=p['forms'][0][1];path.write_text(json.dumps(p,ensure_ascii=False),encoding='utf-8')
            with self.assertRaises(ValueError):sync.sync(root)

    def test_navigation_and_palette_reach_all_three_apps(self):
        with tempfile.TemporaryDirectory(prefix='myday-shell-') as directory:
            root=self.fixture(directory);path=root/'shared/app-shell.json'
            shell=json.loads(path.read_text(encoding='utf-8'));shell['greeting']='같은 하루, 같은 질문'
            path.write_text(json.dumps(shell,ensure_ascii=False),encoding='utf-8');sync.sync(root)
            for name in ['windows/Core/Shared/AppShell.g.cs','macos/Sources/MyDay/AppShell.g.swift','app/src/main/java/com/myday/diary/ui/design/AppShell.g.kt']:
                self.assertIn(shell['greeting'],(root/name).read_text(encoding='utf-8'))
            self.assertIn('Color(0xff60694E)',(root/'app/src/main/java/com/myday/diary/ui/design/AppShell.g.kt').read_text(encoding='utf-8'))

if __name__=='__main__': unittest.main()
