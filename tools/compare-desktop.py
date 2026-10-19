#!/usr/bin/env python3
"""Compare outputs from the real Windows and Mac apps against shared source expectations."""
import argparse
import importlib.util
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('sync_desktop', ROOT/'tools/sync-desktop.py')
shared=importlib.util.module_from_spec(spec)
spec.loader.exec_module(shared)

def expected():
    c, templates, cases, digest=shared.load_sources()
    limits=c['limits']; chat=c['chat']
    return {
        'Digest':digest,
        'Templates':[[t['id'],t['name'],t['description'],t['style'],t['category'],str(t['minutes']),t['layout'],t['reference']]
            + [s[k] for s in t['sections'] for k in ['kind','title','prompt']] for t in templates],
        'Layouts':[case['expected'] for case in cases],
        'Limits':[limits[k] for k in ['MaxDays','MaxBlocks','MaxTextUtf16','MaxTitleUtf16','MaxPromptUtf16','MinWidth','MaxWidth','MinHeight','MaxHeight','MaxPosition']]
            + [chat[k] for k in ['ChatProtocol','ChatMaxText','ChatMaxPage','ChatMaxUsers','ChatVisibleHistory']],
        'Heights':[limits['PhotoHeight'] if k in ['photo','photo-slot'] else limits['EmotionHeight'] if k=='emotion' else limits['TextHeight'] for k in c['blockKinds']],
        'Palette':[c['design'][k] for k in ['Ink','Muted','Accent','Paper']] + c['design']['Backgrounds'],
    }

def compare(files):
    wanted=expected()
    for path in files:
        actual=json.loads(Path(path).read_text(encoding='utf-8-sig'))
        if actual != wanted:
            fields=[key for key in wanted if actual.get(key)!=wanted[key]]
            raise ValueError(str(path)+': native shared contract mismatch: '+', '.join(fields))
    return len(wanted['Templates']),len(wanted['Layouts'])

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('files',type=Path,nargs='+')
    args=parser.parse_args()
    templates,cases=compare(args.files)
    print(f'Native desktop outputs match: {templates} templates, {cases} geometry cases, data limits and palette.')
