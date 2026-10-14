#!/usr/bin/env python3
"""Write the third-party notices that ship next to a packaged build, from docs/legal/third-party.json.

  write_notices.py OUTPUT_FILE

Lists redistributed (runtime) dependencies, datasets and assets with their licenses and notices.
Build-only and test-only dependencies do not ship and are omitted.
"""
import json
import sys
from pathlib import Path

REGISTRY=Path(__file__).resolve().parents[2]/'docs/legal/third-party.json'

def render(registry):
    lines=['THIRD-PARTY NOTICES','',
           'This build includes the components below. Each is used under the license named; full license texts',
           'are available from the listed source. Registry: docs/legal/third-party.json in the Sovereigns repository.','']
    runtime=[dep for dep in registry['dependencies'] if dep['scope']=='runtime']
    for title,items in (('Components',runtime),('Datasets',registry['datasets']),('Assets',registry['assets'])):
        if not items: continue
        lines+=[title,'-'*len(title)]
        for item in items:
            name=item.get('name') or item['path']
            lines.append(f'{name}{" "+item["version"] if item.get("version") else ""} - {item["license"]}')
            for key in ('copyright','author','attribution','source','note'):
                if item.get(key): lines.append(f'    {key}: {item[key]}')
            lines.append('')
    return '\n'.join(lines).rstrip()+'\n'

def main(argv=None):
    argv=sys.argv[1:] if argv is None else argv
    if len(argv)!=1:
        print(__doc__,file=sys.stderr)
        return 2
    output=Path(argv[0])
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(render(json.loads(REGISTRY.read_text(encoding='utf-8'))),encoding='utf-8')
    return 0

if __name__=='__main__': sys.exit(main())
