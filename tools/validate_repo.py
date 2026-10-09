#!/usr/bin/env python3
"""Validate modular spec/task routing, stable IDs, authority and internal navigation."""
import hashlib
import json
import re
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent

def load(path): return json.loads((ROOT/path).read_text(encoding='utf-8'))

def check():
    errors=[]
    def check_ok(ok,msg):
        if not ok: errors.append(msg)
    index=load('meta/spec-index.json'); phases=load('meta/phases.json'); tasks=load('meta/tasks.json')
    check_ok(list(map(int,index))==list(range(39)),'Expected 39 ordered design chapters')
    check_ok(list(phases)==[f'{i:02d}' for i in range(55)],'Expected 55 ordered phases')
    for no,entry in index.items():
        path=ROOT/entry['path']
        check_ok(path.exists(),f'No spec file {path}')
        if path.exists():
            content=path.read_bytes()
            check_ok(content.startswith(f'# {int(no)}.'.encode()),f'Wrong spec heading {no}')
            check_ok(hashlib.sha256(content).hexdigest()==entry['sha256'],f'Spec checksum metadata stale for {no}')
    discovered=set()
    for no,entry in phases.items():
        path=ROOT/entry['path']
        check_ok(path.exists(),f'No phase file {path}')
        if not path.exists(): continue
        content=path.read_text(encoding='utf-8')
        check_ok(content.startswith(f'## Phase {no} '),f'Wrong phase heading {no}')
        listed=re.findall(r'(?m)^- \[[ xX]\] \*\*(SOV-P\d{2}-T\d{2,3})\*\* — (.+)$',content)
        check_ok([id for id,_ in listed]==entry['tasks'],f'Phase {no} task manifest out of sync')
        check_ok(len(listed)==entry['count'],f'Phase {no} count out of sync')
        check_ok(bool(re.search(r'(?m)^\*\*Exit gate '+re.escape(no)+r'(?:\s+—[^*]+)?[:*]',content)),f'Phase {no} missing exit gate')
        for tid,title in listed:
            check_ok(tid not in discovered,f'Duplicate ID {tid}')
            discovered.add(tid)
            task=tasks.get(tid)
            check_ok(task is not None,f'Missing task metadata {tid}')
            if task:
                check_ok(task['title']==title,f'Task title differs in {tid}')
                check_ok(task['phase']==int(no),f'Task phase differs in {tid}')
                check_ok(task['phase_file']==entry['path'],f'Task path differs in {tid}')
                check_ok(bool(task['spec_chapters']),f'Missing task context in {tid}')
                for spec in task['spec_chapters']:
                    check_ok(str(spec) in index,f'Bad chapter in {tid}: {spec}')
    check_ok(discovered==set(tasks),'Task set and file inventory differ')
    check_ok(len(discovered)==1072,f'Unexpected number of tasks: {len(discovered)}')
    for item in ['AGENTS.md','README.md','docs/agents/QUALITY_BAR.md','docs/agents/WORKFLOW.md',
                 'docs/architecture/ADR-0001-runtime.md','.github/workflows/repo-docs.yml']:
        check_ok((ROOT/item).exists(),f'Missing foundation artifact {item}')
    for path in ROOT.rglob('README.md'):
        for uri in re.findall(r'(?<!!)\[[^\]]+\]\(([^)#]+)(?:#[^)]*)?\)',path.read_text(encoding='utf-8')):
            if uri.startswith(('http://','https://','mailto:')): continue
            check_ok((path.parent/uri).exists(),f'Broken relative link {path.relative_to(ROOT)} -> {uri}')
    if errors:
        print('FAILED:')
        for error in errors: print(' - '+error)
        return False
    print(f'PASS: {len(index)} authoritative design chapters, {len(phases)} phases, {len(discovered)} unique tasks; exit gates, hashes, routing and README links valid.')
    print('Scope: repository documentation validation; not Godot/.NET game execution or remote CI enforcement.')
    return True

if __name__=='__main__': sys.exit(0 if check() else 1)
