#!/usr/bin/env python3
"""Locate an individual Sovereigns task and its relevant, authoritative context."""
import argparse
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

def load(path):
    return json.loads((ROOT / path).read_text(encoding='utf-8'))

def select_task(task_id):
    result = load('meta/tasks.json').get(task_id)
    if not result:
        raise ValueError(f'Unknown task: {task_id}')
    return result

def scoped_specs(task):
    index=load('meta/spec-index.json')
    return [index[str(i)] for i in task['spec_chapters']]

def task_excerpt(task):
    source=(ROOT/task['phase_file']).read_text(encoding='utf-8')
    heading=source.split('\n- [',1)[0].strip()
    item=re.search(r'(?m)^- \[[ xX]\] \*\*'+re.escape(task['id'])+r'\*\* — .+$',source)
    gate=re.search(r'(?m)^\*\*Exit gate \d{2}.*$',source)
    if not item or not gate: raise ValueError('Task or phase gate not found')
    return '\n\n'.join([heading,item.group(0),gate.group(0)])

def slice_section(section):
    match=re.fullmatch(r'(\d{1,2})\.(\d{1,2})',section)
    if not match: raise ValueError('Section must look like 3.5')
    index=load('meta/spec-index.json')
    chapter=index.get(str(int(match.group(1))))
    if not chapter: raise ValueError('Unknown chapter')
    source=(ROOT/chapter['path']).read_text(encoding='utf-8')
    start=re.search(r'(?m)^## '+re.escape(section)+r'(?:\s|\.)',source)
    if not start: raise ValueError('Unknown section')
    end=re.search(r'(?m)^## \d{1,2}\.\d{1,2}(?:\s|\.)',source[start.end():])
    return source[start.start():start.end()+end.start() if end else len(source)].strip()

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('task_id',nargs='?')
    options=parser.add_mutually_exclusive_group()
    options.add_argument('--list',action='store_true')
    options.add_argument('--task',action='store_true')
    options.add_argument('--bundle',action='store_true')
    options.add_argument('--section',metavar='N.N')
    options.add_argument('--json',action='store_true')
    args=parser.parse_args()
    try:
        if args.section:
            print(slice_section(args.section)); return
        if not args.task_id: parser.error('provide a task ID or --section')
        task=select_task(args.task_id)
        specs=scoped_specs(task)
        startup=['AGENTS.md','docs/agents/QUALITY_BAR.md','docs/agents/WORKFLOW.md']
        if args.task:
            print(task_excerpt(task)); return
        if args.json:
            print(json.dumps({'task':task,'startup':startup,'spec_paths':[s['path'] for s in specs]},indent=2)); return
        if args.bundle:
            for p in startup:
                print(f'\n---\nFILE: {p}\n'); print((ROOT/p).read_text())
            print('\n---\nACTIVE TASK AND PHASE EXIT\n'); print(task_excerpt(task))
            for spec in specs:
                print(f'\n---\nFILE: {spec["path"]}\n'); print((ROOT/spec['path']).read_text())
            print('\nReview any additional producer/consumer specs the change touches.')
            return
        print(f'TASK: {task["id"]}: {task["title"]}')
        print(f'PHASE: {task["phase_file"]}')
        if task.get('recommended_spec_sections'):
            print('EXACT DESIGN REFERENCES FROM PHASE: '+', '.join(task['recommended_spec_sections']))
        print('BEGIN WITH: '+', '.join(startup))
        print('SCOPED AUTHORITATIVE SPECIFICATIONS:')
        for spec in specs: print(f' - {spec["path"]} ({spec["bytes"]} bytes)')
        print(f'One task and phase gate: python3 tools/task_context.py {task["id"]} --task')
        print('One exact section: python3 tools/task_context.py --section 3.5')
        print('Inspect upstream/downstream specs if work scope crosses these domains.')
    except ValueError as exc: parser.error(str(exc))

if __name__=='__main__': main()
