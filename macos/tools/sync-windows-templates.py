#!/usr/bin/env python3
"""Extract the original catalog from the locally available Windows release commit."""
import json
import re
import subprocess
from pathlib import Path

REVISION = '9abf375745369fc9a45e0d5e918e0e8a8fb9c26b'
FILES = ['DiaryTemplates.cs'] + ['Templates/' + name + 'Formats.cs' for name in ['Daily', 'Reflection', 'Gratitude', 'Emotion', 'Learning', 'Planning', 'Wellbeing', 'Relationship', 'Creative', 'Memory']]
TOKEN = re.compile(r'\s*(?:((?:"(?:\\.|[^"\\])*"))|([0-9]+)|([A-Za-z_][A-Za-z_0-9]*)|([(),]))')

def args(source, start):
    values = []
    position = start
    while True:
        match = TOKEN.match(source, position)
        if not match:
            raise ValueError(source[position:position+80])
        position = match.end()
        string, number, identifier, punctuation = match.groups()
        if string:
            values.append(json.loads(string))
        elif number:
            values.append(int(number))
        elif identifier:
            if identifier == 'new':
                continue
            opening = TOKEN.match(source, position)
            assert opening and opening.group(4) == '('
            nested, position = args(source, opening.end())
            values.append((identifier, nested))
        elif punctuation == ')':
            return values, position
        elif punctuation != ',':
            raise ValueError(punctuation)

def main():
    catalog = []
    for filename in FILES:
        source = subprocess.check_output(['git', 'show', f'{REVISION}:windows/Core/{filename}'], text=True)
        pattern = r'new JournalTemplate\(' if filename == 'DiaryTemplates.cs' else r'\bT\('
        for match in re.finditer(pattern, source):
            values, _ = args(source, match.end())
            if filename == 'DiaryTemplates.cs':
                identifier, name, description, style, category, minutes, layout, reference, *sections = values
            else:
                category, identifier, name, description, style, minutes, layout, reference, *sections = values
            converted = []
            for function, fields in sections:
                if function == 'JournalSection':
                    kind, title, prompt = fields
                else:
                    kind = {'S': 'text', 'E': 'emotion', 'D': 'todo', 'H': 'habit'}[function]
                    title, prompt = fields
                converted.append(dict(kind=kind, title=title, prompt=prompt))
            catalog.append(dict(id=identifier, name=name, description=description, style=style, category=category, minutes=minutes, layout=layout, reference=reference, sections=converted))
    assert len(catalog) == 100 and len({t['id'] for t in catalog}) == 100
    destination = Path(__file__).resolve().parents[1] / 'Sources/MyDay/Resources/templates.json'
    destination.write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + '\n')
    print(f'Copied {len(catalog)} original templates from {REVISION}')

if __name__ == '__main__':
    main()
