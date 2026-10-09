#!/usr/bin/env python3
"""After reviewed edits, refresh indexes without silently assigning specs to new task IDs."""
import hashlib
import json
import re
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent

def load(p): return json.loads((ROOT/p).read_text(encoding='utf-8'))
def save(p,value): (ROOT/p).write_text(json.dumps(value,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')

def main():
    spec=load('meta/spec-index.json'); phases=load('meta/phases.json'); tasks=load('meta/tasks.json')
    for no,item in spec.items():
        path=ROOT/item['path']
        if not path.exists(): raise RuntimeError(f'Missing normative chapter {path}')
        content=path.read_bytes()
        item['sha256']=hashlib.sha256(content).hexdigest();item['bytes']=len(content)
    for no,item in phases.items():
        source=(ROOT/item['path']).read_text(encoding='utf-8')
        found=re.findall(r'(?m)^- \[[ xX]\] \*\*(SOV-P\d{2}-T\d{2,3})\*\* — (.+)$',source)
        item['tasks']=[tid for tid,_ in found]
        item['count']=len(found)
        for tid,title in found:
            if tid not in tasks:
                raise RuntimeError(f'{tid}: new task needs an explicitly authored context route in meta/tasks.json before refresh')
            if tasks[tid]['phase']!=int(no):
                raise RuntimeError(f'{tid}: illegal move across phases; preserve task ID and phase')
            tasks[tid]['title']=title
            tasks[tid]['phase_file']=item['path']
    if set(tasks)!={t for p in phases.values() for t in p['tasks']}:
        raise RuntimeError('A stable task ID was removed. Do not delete IDs; supersede with traceability.')
    save('meta/spec-index.json',spec);save('meta/phases.json',phases);save('meta/tasks.json',tasks)
    print('Updated normative source hashes, task descriptions and phase indexes. Review diff, then run repository tests.')

if __name__=='__main__': main()
