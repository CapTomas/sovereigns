#!/usr/bin/env python3
"""Validate modular spec/task routing, stable IDs, authority and internal navigation."""
import hashlib
import itertools
import json
import os
import re
import sys
import tomllib
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
REQUIRED_FIXTURES={'F-VALLEY','F-RAIN-SNOW','F-TRADE','F-BORDER','F-BATTLE','F-MANY-SEEDS','F-LONG-RUN'}
TASK_LINE=r'(?m)^- \[([ xX])\] \*\*(SOV-P\d{2}-T\d{2,3})\*\* — (.+)$'
MIN_TASKS=1072  # Migrated baseline; IDs are append-only, so the inventory can only grow.
MARKDOWN_DIRS=['docs','tasks','tests','tools','src','game','meta','.github','.claude','.codex']
REGISTRY='docs/legal/third-party.json'
ASSET_EXTENSIONS={'.svg','.png','.jpg','.jpeg','.webp','.bmp','.tga','.exr','.hdr','.dds','.ktx','.ogg','.wav','.mp3','.flac',
                  '.ttf','.otf','.woff','.woff2','.glb','.gltf','.fbx','.mp4','.webm'}
ASSET_DIRS=('game','content')
SKIP_DIRS={'.git','.godot','.mono','bin','obj','node_modules','worktrees','artifacts','__pycache__'}
NO_ATTRIBUTION={'CC0-1.0','Unlicense','LicenseRef-Project-Original'}  # Every other asset or dataset license needs attribution text.

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

def matrix_names(name,matrix):
    """Expand ${{ matrix.key }} over literal matrix lists; a name using anything else stays as written."""
    keys=list(dict.fromkeys(re.findall(r'\$\{\{\s*matrix\.([A-Za-z0-9_-]+)\s*\}\}',name)))
    if not keys or not all(matrix.get(key) for key in keys): return {name}
    expanded=set()
    for values in itertools.product(*(matrix[key] for key in keys)):
        text=name
        for key,value in zip(keys,values): text=re.sub(r'\$\{\{\s*matrix\.'+re.escape(key)+r'\s*\}\}',lambda _:value,text)
        expanded.add(text)
    return expanded

def workflow_checks(text):
    """Job ids and job names under a workflow's top-level jobs: map (the check names GitHub reports).
    A name using ${{ matrix.key }} expands over the literal lists under strategy.matrix."""
    names,in_jobs,in_matrix,key,job_name,matrix=set(),False,False,None,None,{}
    for line in text.splitlines()+['end:']:  # The sentinel closes the last job.
        top=re.match(r'^[^\s#]',line)
        job=in_jobs and re.match(r'^  ([A-Za-z0-9_-]+):\s*$',line)
        if top or job:
            if job_name: names|=matrix_names(job_name,matrix)
            job_name,matrix,in_matrix,key=None,{},False,None
            if top: in_jobs=line.rstrip()=='jobs:'
            else: names.add(job.group(1))
        elif not in_jobs: continue
        elif match:=re.match(r'^    name:\s*(.+?)\s*$',line): job_name=match.group(1).strip('\'"')
        elif re.match(r'^      matrix:\s*$',line): in_matrix=True
        elif in_matrix and (match:=re.match(r'^        ([A-Za-z0-9_-]+):\s*(.*?)\s*$',line)):
            key=match.group(1)
            matrix[key]=[item.strip().strip('\'"') for item in match.group(2)[1:-1].split(',')] if match.group(2).startswith('[') else []
        elif in_matrix and key and (match:=re.match(r'^          - (.+?)\s*$',line)): matrix[key].append(match.group(1).strip('\'"'))
        elif in_matrix and re.match(r'^ {0,6}\S',line): in_matrix=False
    return names

def ruleset_errors(name,data,checks):
    """Required status checks must name a real workflow job, or merges wait forever."""
    if not isinstance(data,dict) or not isinstance(data.get('rules'),list): return [f'Ruleset {name} needs a rules list']
    contexts=[check.get('context') for rule in data['rules'] if isinstance(rule,dict) and rule.get('type')=='required_status_checks'
              for check in rule.get('parameters',{}).get('required_status_checks',[])]
    return [f'Ruleset {name} requires check {context!r}, which no workflow job provides' for context in contexts if context not in checks]

def walk(root,*start):
    """Files under the start directories (default: all of root), skipping VCS, build output and Godot caches."""
    for top in start or ['.']:
        for folder,dirs,files in os.walk(root/top):
            dirs[:]=sorted(d for d in dirs if d not in SKIP_DIRS)
            for name in sorted(files): yield Path(folder)/name

def nuget_references(root,errors):
    """{(lowercase id, version): (id, file)} for central package versions, inline PackageReference versions and versioned MSBuild SDKs."""
    refs={}
    def add(name,version): refs[(name.lower(),version)]=(name,where)
    for path in walk(root):
        if path.name!='Directory.Packages.props' and path.suffix!='.csproj': continue
        where=path.relative_to(root).as_posix()
        try: tree=ET.parse(path).getroot()
        except ET.ParseError as exc:
            errors.append(f'{where}: invalid XML ({exc})')
            continue
        for item in tree.iter('PackageVersion'): add(item.get('Include',''),item.get('Version'))
        for item in tree.iter('PackageReference'):
            if item.get('Version'): add(item.get('Include',''),item.get('Version'))
        for sdk in (tree.get('Sdk') or '').split(';'):
            if '/' in sdk: add(*sdk.split('/',1))
        for item in tree.iter('Sdk'):
            if item.get('Version'): add(item.get('Name',''),item.get('Version'))
    return refs

def locked_packages(root,errors):
    """{(lowercase id, resolved version): (id, lock file)} for every package restored by a project under src/ (project references excluded)."""
    packages={}
    for path in walk(root,'src'):
        if path.name!='packages.lock.json': continue
        try: data=json.loads(path.read_text(encoding='utf-8'))
        except json.JSONDecodeError as exc:
            errors.append(f'{path.relative_to(root).as_posix()}: invalid JSON ({exc})')
            continue
        for framework in data.get('dependencies',{}).values():
            for name,entry in framework.items():
                if entry.get('type')!='Project': packages[(name.lower(),entry.get('resolved'))]=(name,path.relative_to(root).as_posix())
    return packages

def registry_entry_errors(section,entry,allowed):
    """Field, license and notice rules for one registry entry."""
    if not isinstance(entry,dict): return [f'{REGISTRY} {section}: every entry must be an object']
    label=f'{REGISTRY} {section}[{entry.get("name") or entry.get("path")}]'
    required={'dependencies':('name','version','kind','scope','license','source'),'datasets':('name','license','source'),
              'assets':('path','license','source')}[section]
    errors=[f'{label}: missing {field}' for field in required if not isinstance(entry.get(field),str) or not entry[field].strip()]
    license_id=entry.get('license')
    if isinstance(license_id,str) and license_id not in allowed:
        errors.append(f'{label}: license {license_id} is not allowed; see docs/legal/README.md (allowed_licenses in the registry)')
    if section=='dependencies':
        if entry.get('kind') not in ('nuget','component'): errors.append(f'{label}: kind must be nuget or component')
        if entry.get('scope') not in ('runtime','build','test'): errors.append(f'{label}: scope must be runtime, build or test')
        if entry.get('scope')=='runtime' and not entry.get('copyright'): errors.append(f'{label}: runtime items ship in builds and need a copyright notice')
    elif license_id not in NO_ATTRIBUTION and not entry.get('attribution'):
        errors.append(f'{label}: license {license_id} requires attribution text')
    return errors

def legal_errors(root=ROOT):
    """Every dependency, dataset and audiovisual asset needs a registry entry with an allowed license, and the registry must not go stale."""
    try: registry=json.loads((root/REGISTRY).read_text(encoding='utf-8'))
    except (OSError,json.JSONDecodeError) as exc: return [f'{REGISTRY}: {exc}']
    errors=[]
    allowed=set(registry.get('allowed_licenses') or [])
    for section in ('dependencies','datasets','assets'):
        if not isinstance(registry.get(section),list): errors.append(f'{REGISTRY}: {section} must be a list')
        else: errors.extend(error for entry in registry[section] for error in registry_entry_errors(section,entry,allowed))
    if errors: return errors
    registered={(d['name'].lower(),d['version']):d for d in registry['dependencies'] if d.get('kind')=='nuget'}
    refs=nuget_references(root,errors)
    locked=locked_packages(root,errors)
    for (key,version),(name,where) in sorted({**locked,**refs}.items()):
        if (key,version) not in registered: errors.append(f'{where}: package {name} {version} has no entry in {REGISTRY}')
    direct={name for name,_ in refs}
    for (name,version),entry in sorted(registered.items()):
        if (name,version) not in refs and (name,version) not in locked and str(entry.get('via','')).lower() not in direct:
            errors.append(f'{REGISTRY}: {entry["name"]} {version} is no longer referenced by any project; remove the entry or fix its version')
    toolchain=load_toolchain(root)
    for entry in registry['dependencies']:
        if 'toolchain' in entry and toolchain_value(toolchain,entry['toolchain'])!=entry.get('version'):
            errors.append(f'{REGISTRY}: {entry["name"]} version {entry.get("version")} differs from tools/toolchain.json {entry["toolchain"]}')
    assets=[a['path'] for a in registry['assets'] if isinstance(a.get('path'),str)]
    for path in walk(root,*ASSET_DIRS):
        rel=path.relative_to(root).as_posix()
        if path.suffix.lower() in ASSET_EXTENSIONS and not any(rel==a or a.endswith('/') and rel.startswith(a) for a in assets):
            errors.append(f'{rel}: audiovisual asset has no entry in {REGISTRY} assets (add its path, license, source and author)')
    errors.extend(f'{REGISTRY}: asset {a} does not exist; remove or fix the entry' for a in assets if not (root/a).exists())
    return errors

def load_toolchain(root=ROOT):
    try: return json.loads((root/'tools/toolchain.json').read_text(encoding='utf-8'))
    except (OSError,json.JSONDecodeError): return None

def toolchain_value(toolchain,dotted):
    for key in dotted.split('.'):
        toolchain=toolchain.get(key) if isinstance(toolchain,dict) else None
    return toolchain

def toolchain_errors(root=ROOT):
    """tools/toolchain.json must agree with global.json and the Godot SDK pin, and its Godot downloads must match the pinned version."""
    toolchain=load_toolchain(root)
    if toolchain is None: return ['tools/toolchain.json is missing or not valid JSON']
    errors=[]
    try: sdk=json.loads((root/'global.json').read_text(encoding='utf-8'))['sdk']['version']
    except (OSError,KeyError,json.JSONDecodeError): sdk=None
    if toolchain_value(toolchain,'dotnet.sdk')!=sdk: errors.append(f'tools/toolchain.json dotnet.sdk must equal global.json sdk.version ({sdk})')
    version=toolchain_value(toolchain,'godot.version')
    base=f'https://github.com/godotengine/godot/releases/download/{version}-{toolchain_value(toolchain,"godot.channel")}/'
    for path in ('godot.editor.linux','godot.editor.macos','godot.editor.windows','godot.templates'):
        url,sha=(toolchain_value(toolchain,f'{path}.{key}') for key in ('url','sha512'))
        if not isinstance(url,str) or not url.startswith(base): errors.append(f'tools/toolchain.json {path}.url must start with {base}')
        if not isinstance(sha,str) or not re.fullmatch(r'[0-9a-f]{128}',sha): errors.append(f'tools/toolchain.json {path}.sha512 must be 128 lowercase hex digits')
    for (key,sdk_version),(name,where) in nuget_references(root,[]).items():
        if key=='godot.net.sdk' and sdk_version!=version: errors.append(f'{where}: {name} {sdk_version} differs from tools/toolchain.json godot.version {version}')
    return errors

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
    errors.extend(toolchain_errors())
    errors.extend(legal_errors())
    for item in ['AGENTS.md','CLAUDE.md','README.md','docs/agents/QUALITY_BAR.md','docs/agents/WORKFLOW.md',
                 'docs/agents/MODEL_ROUTING.md','docs/agents/DEFINITION_OF_DONE.md','docs/architecture/ADR-0001-runtime.md',
                 'docs/architecture/ADR-0003-platforms-input-distribution.md','docs/architecture/ADR-0004-units-coordinates-time-identifiers.md',
                 '.github/workflows/repo-docs.yml','.github/workflows/build.yml','.codex/config.toml',
                 'tools/toolchain.json','tools/doctor.py','docs/legal/README.md',
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
    print(f'PASS: {len(index)} authoritative design chapters, {len(phases)} phases, {len(discovered)} unique tasks, {len(fixtures)} fixture manifests; exit gates, hashes, routing, ID order, evidence, rulesets, toolchain pins, license registry, agent adapters and relative Markdown links valid.')
    print('Scope: repository documentation validation; not Godot/.NET game execution or remote CI enforcement.')
    return True

if __name__=='__main__': sys.exit(0 if check() else 1)
