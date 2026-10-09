#!/usr/bin/env python3
"""Validate modular spec/task routing, stable IDs, authority and internal navigation."""
import hashlib
import json
import re
import sys
import tomllib
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
REQUIRED_FIXTURES={'F-VALLEY','F-RAIN-SNOW','F-TRADE','F-BORDER','F-BATTLE','F-MANY-SEEDS','F-LONG-RUN'}
TASK_LINE=r'(?m)^- \[([ xX])\] \*\*(SOV-P\d{2}-T\d{2,3})\*\* — (.+)$'
MIN_TASKS=1072  # Migrated baseline; IDs are append-only, so the inventory can only grow.
MARKDOWN_DIRS=['docs','tasks','tests','tools','src','game','meta','.github','.claude','.codex']

def load(path): return json.loads((ROOT/path).read_text(encoding='utf-8'))

def task_id_errors(no,ids):
    """IDs are append-only: a phase lists T01..Tnn in order, so gaps or renumbering show up."""
    expected=[f'SOV-P{no}-T{i:02d}' for i in range(1,len(ids)+1)]
    return [] if ids==expected else [f'Phase {no} task IDs must run {expected[0]}..{expected[-1]} in order without gaps']

def markdown_headings(text):
    headings,fenced=[],False
    for line in text.splitlines():
        if line.lstrip().startswith(('```','~~~')): fenced=not fenced
        elif not fenced and line.startswith('#'): headings.append(line)
    return headings

def evidence_errors(no,checked,evidence):
    """A checked task needs a heading naming its full ID in tasks/evidence/PNN.md."""
    if not checked: return []
    if evidence is None: return [f'Phase {no} has checked tasks but no tasks/evidence/P{no}.md']
    headings='\n'.join(markdown_headings(evidence))
    return [f'{tid} is checked without an evidence heading in tasks/evidence/P{no}.md'
            for tid in checked if not re.search(rf'\b{re.escape(tid)}\b',headings)]

def fixture_errors(name,data):
    errors=[]
    fid=data.get('id') if isinstance(data,dict) else None
    if fid!=name: return [f'Fixture {name}.json must be an object whose id is {name}']
    if not re.fullmatch(r'F-[A-Z0-9]+(?:-[A-Z0-9]+)*',fid): errors.append(f'Fixture {fid}: id must look like F-NAME')
    version=data.get('version')
    if type(version) is not int or version<1: errors.append(f'Fixture {fid}: version must be a positive integer')
    if not isinstance(data.get('purpose'),str) or not data['purpose'].strip(): errors.append(f'Fixture {fid}: purpose is required')
    status=data.get('status')
    if status=='blank':
        if data.get('seed') is not None or data.get('scenario') is not None:
            errors.append(f'Fixture {fid}: blank fixtures must have null seed and scenario')
    elif status=='defined':
        if data.get('seed') is None or not isinstance(data.get('scenario'),str):
            errors.append(f'Fixture {fid}: defined fixtures need a seed and a scenario path')
        elif not (ROOT/data['scenario']).resolve().is_relative_to(ROOT):
            errors.append(f'Fixture {fid}: scenario path must stay inside the repository')
        elif not (ROOT/data['scenario']).exists():
            errors.append(f'Fixture {fid}: scenario path {data["scenario"]} does not exist')
    else: errors.append(f'Fixture {fid}: status must be blank or defined')
    return errors

def workflow_checks(text):
    """Job ids and job names under a workflow's top-level jobs: map (the check names GitHub reports)."""
    names,in_jobs=set(),False
    for line in text.splitlines():
        if re.match(r'^\S',line): in_jobs=line.rstrip()=='jobs:'
        elif in_jobs and (match:=re.match(r'^  ([A-Za-z0-9_-]+):\s*$',line)): names.add(match.group(1))
        elif in_jobs and (match:=re.match(r'^    name:\s*(.+?)\s*$',line)): names.add(match.group(1).strip('\'"'))
    return names

def ruleset_errors(name,data,checks):
    """Required status checks must name a real workflow job, or merges wait forever."""
    if not isinstance(data,dict) or not isinstance(data.get('rules'),list): return [f'Ruleset {name} needs a rules list']
    contexts=[check.get('context') for rule in data['rules'] if isinstance(rule,dict) and rule.get('type')=='required_status_checks'
              for check in rule.get('parameters',{}).get('required_status_checks',[])]
    return [f'Ruleset {name} requires check {context!r}, which no workflow job provides' for context in contexts if context not in checks]

def spec_version(chapter38):
    """Newest x.y.z entry in the §38.4 living-version record."""
    found=re.findall(r'\*\*(\d+)\.(\d+)\.(\d+) · (\d{4}-\d{2}-\d{2}):\*\*',chapter38)
    if not found: return None
    *ver,date=max(found,key=lambda entry:tuple(map(int,entry[:3])))
    return '.'.join(ver),date

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
    routed={str(spec) for task in tasks.values() for spec in task['spec_chapters']}
    check_ok(set(index)<=routed,f'Spec chapters not routed by any task: {sorted(set(index)-routed,key=int)}')
    discovered=set()
    for no,entry in phases.items():
        path=ROOT/entry['path']
        check_ok(path.exists(),f'No phase file {path}')
        if not path.exists(): continue
        content=path.read_text(encoding='utf-8')
        check_ok(content.startswith(f'## Phase {no} '),f'Wrong phase heading {no}')
        marked=re.findall(TASK_LINE,content)
        listed=[(tid,title) for _,tid,title in marked]
        check_ok([id for id,_ in listed]==entry['tasks'],f'Phase {no} task manifest out of sync')
        errors.extend(task_id_errors(no,[id for id,_ in listed]))
        evidence_path=ROOT/f'tasks/evidence/P{no}.md'
        evidence=evidence_path.read_text(encoding='utf-8') if evidence_path.exists() else None
        errors.extend(evidence_errors(no,[tid for mark,tid,_ in marked if mark in 'xX'],evidence))
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
    check_ok(len(discovered)>=MIN_TASKS,f'Only {len(discovered)} tasks; IDs may be appended but never removed (baseline {MIN_TASKS})')
    fixture_dir=ROOT/'tests/fixtures'
    fixtures={path.stem:path for path in fixture_dir.glob('*.json')}
    check_ok(REQUIRED_FIXTURES<=set(fixtures),f'Missing fixture manifests: {sorted(REQUIRED_FIXTURES-set(fixtures))}')
    for name,path in sorted(fixtures.items()):
        try: errors.extend(fixture_errors(name,json.loads(path.read_text(encoding='utf-8'))))
        except json.JSONDecodeError as exc: check_ok(False,f'Invalid JSON {path.relative_to(ROOT)}: {exc}')
    if '38' in index and (ROOT/index['38']['path']).exists():
        version=spec_version((ROOT/index['38']['path']).read_text(encoding='utf-8'))
        check_ok(version is not None,'No x.y.z version entry in spec §38.4')
        if version:
            ver,date=version
            check_ok(f'**Game Design Bible version:** {ver} ({date})' in (ROOT/'docs/spec/README.md').read_text(encoding='utf-8'),
                     f'docs/spec/README.md must state Game Design Bible version {ver} ({date}) from §38.4')
            check_ok(f'Game Design Bible {ver}' in (ROOT/'README.md').read_text(encoding='utf-8'),
                     f'README.md must name Game Design Bible {ver}')
    checks=set()
    for path in (ROOT/'.github/workflows').glob('*.y*ml'): checks|=workflow_checks(path.read_text(encoding='utf-8'))
    for path in sorted((ROOT/'.github/rulesets').glob('*.json')):
        try: errors.extend(ruleset_errors(path.name,json.loads(path.read_text(encoding='utf-8')),checks))
        except json.JSONDecodeError as exc: check_ok(False,f'Invalid JSON {path.relative_to(ROOT)}: {exc}')
    for item in ['AGENTS.md','CLAUDE.md','README.md','docs/agents/QUALITY_BAR.md','docs/agents/WORKFLOW.md',
                 'docs/agents/MODEL_ROUTING.md','docs/agents/DEFINITION_OF_DONE.md','docs/architecture/ADR-0001-runtime.md',
                 'docs/architecture/ADR-0003-platforms-input-distribution.md','docs/architecture/ADR-0004-units-coordinates-time-identifiers.md',
                 '.github/workflows/repo-docs.yml','.codex/config.toml',
                 '.claude/agents/repo-scout.md','.claude/agents/repo-implementer.md',
                 '.claude/agents/repo-reviewer.md']:
        check_ok((ROOT/item).exists(),f'Missing foundation artifact {item}')
    claude=ROOT/'CLAUDE.md'
    if claude.exists():
        check_ok('@AGENTS.md' in claude.read_text(encoding='utf-8').splitlines(),
                 'CLAUDE.md must import the shared AGENTS.md policy')
    config_paths=[ROOT/'.codex/config.toml']
    config_paths.extend(ROOT/'.codex/agents'/f'repo-{role}.toml'
                        for role in ['scout','implementer','reviewer'])
    agent_names=set()
    for path in config_paths:
        check_ok(path.is_file(),f'Missing Codex adapter {path.relative_to(ROOT)}')
        if not path.is_file(): continue
        try:
            config=tomllib.loads(path.read_text(encoding='utf-8'))
        except tomllib.TOMLDecodeError as exc:
            check_ok(False,f'Invalid TOML {path.relative_to(ROOT)}: {exc}')
            continue
        if path.parent.name=='agents':
            for key in ['name','description','developer_instructions']:
                check_ok(isinstance(config.get(key),str) and bool(config[key].strip()),
                         f'{path.relative_to(ROOT)} needs a nonempty {key}')
            name=config.get('name')
            if isinstance(name,str):
                check_ok(name not in agent_names,f'Duplicate Codex agent name {name}')
                agent_names.add(name)
    navigation=set(ROOT.glob('*.md'))
    for directory in MARKDOWN_DIRS:
        navigation.update(path for path in (ROOT/directory).rglob('*.md') if 'worktrees' not in path.parts)
    for path in sorted(navigation):
        for uri in re.findall(r'(?<!!)\[[^\]]+\]\(([^)#]+)(?:#[^)]*)?\)',path.read_text(encoding='utf-8')):
            if uri.startswith(('http://','https://','mailto:')): continue
            check_ok((path.parent/uri).exists(),f'Broken relative link {path.relative_to(ROOT)} -> {uri}')
    if errors:
        print('FAILED:')
        for error in errors: print(' - '+error)
        return False
    print(f'PASS: {len(index)} authoritative design chapters, {len(phases)} phases, {len(discovered)} unique tasks, {len(fixtures)} fixture manifests; exit gates, hashes, routing, ID order, evidence, rulesets, agent adapters and relative Markdown links valid.')
    print('Scope: repository documentation validation; not Godot/.NET game execution or remote CI enforcement.')
    return True

if __name__=='__main__': sys.exit(0 if check() else 1)
