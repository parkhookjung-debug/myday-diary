import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
spec=importlib.util.spec_from_file_location('journal',Path(__file__).with_name('sync-journal.py'));journal=importlib.util.module_from_spec(spec);spec.loader.exec_module(journal)
class JournalGenerationTests(unittest.TestCase):
    def test_generated_outputs_match(self):journal.sync(check=True)
    def test_policy_propagation_and_validation(self):
        with tempfile.TemporaryDirectory() as path:
            root=Path(path);(root/'shared').mkdir();policy=json.loads((journal.ROOT/'shared/journal-tools.json').read_text(encoding='utf-8'))
            policy['reactions']['record']='한 줄 $ test';policy['historyLimitFiles']=137
            source=root/'shared/journal-tools.json';source.write_text(json.dumps(policy),encoding='utf-8')
            outputs=journal.render(root);self.assertTrue(all('137' in value for key,value in outputs.items() if 'Policy' in key));self.assertIn('\\$',next(value for key,value in outputs.items() if key.endswith('.kt')))
            policy['historyLimitBytes']=-1;source.write_text(json.dumps(policy),encoding='utf-8')
            with self.assertRaises(ValueError):journal.render(root)
if __name__=='__main__':unittest.main()
