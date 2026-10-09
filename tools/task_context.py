#!/usr/bin/env python3
"""Discover Sovereigns tasks and retrieve only their relevant context."""
import argparse
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
STARTUP = ['AGENTS.md', 'docs/agents/WORKFLOW.md', 'docs/agents/QUALITY_BAR.md']


def load(path):
    return json.loads((ROOT / path).read_text(encoding='utf-8'))


def select_task(task_id):
    result = load('meta/tasks.json').get(task_id)
    if not result:
        raise ValueError(f'Unknown task: {task_id}. Try --search "topic".')
    return result


def search_tasks(query='', phase=None):
    """Match all case-insensitive terms; phase status is intentionally not inferred."""
    terms = query.casefold().split()
    matches = []
    for task in load('meta/tasks.json').values():
        if phase is not None and task['phase'] != phase:
            continue
        searchable = f'{task["id"]} {task["title"]} {task["phase_file"]}'.casefold()
        if all(term in searchable for term in terms):
            matches.append(task)
    return sorted(matches, key=lambda task: task['id'])


def scoped_specs(task):
    index = load('meta/spec-index.json')
    return [index[str(i)] for i in task['spec_chapters']]


def scoped_instructions(paths):
    """Collect ancestor AGENTS files without scanning unrelated directories."""
    instructions = {'AGENTS.md'}
    for path in paths:
        target = (ROOT / path).resolve()
        if not target.is_relative_to(ROOT):
            raise ValueError(f'Context path must stay inside the repository: {path}')
        # Include the target itself so planned directories work before they exist.
        for directory in reversed([target, *target.parents]):
            if not directory.is_relative_to(ROOT):
                continue
            candidate = directory / 'AGENTS.md'
            if candidate.is_file():
                instructions.add(candidate.relative_to(ROOT).as_posix())
    return sorted(instructions, key=lambda path: (len(Path(path).parts), path))


def task_excerpt(task):
    source = (ROOT / task['phase_file']).read_text(encoding='utf-8')
    heading = source.split('\n- [', 1)[0].strip()
    item = re.search(r'(?m)^- \[[ xX]\] \*\*' + re.escape(task['id']) + r'\*\* — .+$', source)
    gate = re.search(r'(?m)^\*\*Exit gate \d{2}.*$', source)
    if not item or not gate:
        raise ValueError('Task or phase gate not found')
    return '\n\n'.join([heading, item.group(0), gate.group(0)])


def slice_section(section):
    match = re.fullmatch(r'(\d{1,2})\.(\d{1,2})', section)
    if not match:
        raise ValueError('Section must look like 3.5')
    index = load('meta/spec-index.json')
    chapter = index.get(str(int(match.group(1))))
    if not chapter:
        raise ValueError('Unknown chapter')
    source = (ROOT / chapter['path']).read_text(encoding='utf-8')
    start = re.search(r'(?m)^## ' + re.escape(section) + r'(?:\s|\.)', source)
    if not start:
        raise ValueError('Unknown section')
    end = re.search(r'(?m)^## \d{1,2}\.\d{1,2}(?:\s|\.)', source[start.end():])
    return source[start.start():start.end() + end.start() if end else len(source)].strip()


def print_overview(paths):
    print('SOVEREIGNS: task-scoped repository navigation')
    print('INSTRUCTIONS: ' + ', '.join(scoped_instructions(paths)))
    print('WORKFLOW: docs/agents/WORKFLOW.md')
    print('GAME BEHAVIOR: docs/spec/README.md')
    print('IMPLEMENTATION DECISIONS: docs/architecture/README.md')
    print('DELIVERY AND VERIFIED STATUS: tasks/phases/ (task index status is not authoritative)')
    print('CURRENT FOUNDATION: README.md')
    print('Find a task: python3 tools/task_context.py --search "topic"')
    print('Browse a phase: python3 tools/task_context.py --phase 0 --limit 30')
    print('Route a task: python3 tools/task_context.py SOV-P00-T01 --list')
    print('For an unnumbered user request, use --path tools/ or another intended edit path.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('task_id', nargs='?')
    options = parser.add_mutually_exclusive_group()
    options.add_argument('--list', action='store_true', help='list task context paths (default for a task ID)')
    options.add_argument('--task', action='store_true', help='show the task and phase exit gate')
    options.add_argument('--bundle', action='store_true', help='print full task context; use only for isolated handoff')
    options.add_argument('--section', metavar='N.N', help='read one exact spec section')
    options.add_argument('--json', action='store_true', help='emit machine-readable task context')
    options.add_argument('--search', metavar='WORDS', help='find tasks containing all words')
    parser.add_argument('--phase', type=int, metavar='N', help='filter task discovery to phase 0–54')
    parser.add_argument('--limit', type=int, default=10, help='maximum discovery results (default: 10)')
    parser.add_argument('--path', action='append', default=[], metavar='PATH',
                        help='include instructions for an intended edit path; repeat as needed')
    args = parser.parse_args()
    try:
        if args.limit < 1:
            parser.error('--limit must be positive')
        if args.phase is not None and str(args.phase).zfill(2) not in load('meta/phases.json'):
            parser.error('unknown phase; use 0–54')
        discovering = args.search is not None or args.phase is not None
        if discovering:
            if args.task_id or args.list or args.task or args.bundle or args.section or args.json or args.path:
                parser.error('task discovery cannot be combined with task context or --path')
            if args.search is not None and not args.search.strip():
                parser.error('--search requires at least one word')
            matches = search_tasks(args.search or '', args.phase)
            for task in matches[:args.limit]:
                print(f'{task["id"]}: {task["title"]}')
            print(f'{len(matches)} matches; showing {min(len(matches), args.limit)}. '
                  'Phase files own verified status.')
            if len(matches) > args.limit:
                print('Narrow --search/--phase or raise --limit.')
            return
        if args.section:
            if args.task_id or args.path:
                parser.error('--section cannot be combined with a task ID or --path')
            print(slice_section(args.section))
            return
        if not args.task_id:
            if args.task or args.bundle or args.json:
                parser.error('provide a task ID for --task, --bundle or --json')
            print_overview(args.path)
            return
        task = select_task(args.task_id)
        specs = scoped_specs(task)
        instructions = scoped_instructions([*STARTUP, task['phase_file'], *[s['path'] for s in specs], *args.path])
        if args.task:
            print(task_excerpt(task))
            return
        if args.json:
            print(json.dumps({'task': task, 'startup': STARTUP, 'instructions': instructions,
                              'spec_paths': [s['path'] for s in specs]}, indent=2))
            return
        if args.bundle:
            for path in dict.fromkeys([*instructions, *STARTUP]):
                print(f'\n---\nFILE: {path}\n')
                print((ROOT / path).read_text(encoding='utf-8'))
            print('\n---\nACTIVE TASK AND PHASE EXIT\n')
            print(task_excerpt(task))
            for spec in specs:
                print(f'\n---\nFILE: {spec["path"]}\n')
                print((ROOT / spec['path']).read_text(encoding='utf-8'))
            print('\nReview any additional producer/consumer specs the change touches.')
            return
        print(f'TASK: {task["id"]}: {task["title"]}')
        print(f'PHASE: {task["phase_file"]}')
        if task.get('recommended_spec_sections'):
            print('EXACT DESIGN REFERENCES FROM PHASE: ' + ', '.join(task['recommended_spec_sections']))
        print('BEGIN WITH: ' + ', '.join(STARTUP))
        print('APPLICABLE INSTRUCTIONS: ' + ', '.join(instructions))
        print('SCOPED AUTHORITATIVE SPECIFICATIONS:')
        for spec in specs:
            print(f' - {spec["path"]} ({spec["bytes"]} bytes)')
        print(f'One task and phase gate: python3 tools/task_context.py {task["id"]} --task')
        print('One exact section: python3 tools/task_context.py --section 3.5')
        print('Add --path for intended edits; inspect additional producer/consumer specs as scope changes.')
    except ValueError as exc:
        parser.error(str(exc))


if __name__ == '__main__':
    main()
