#!/usr/bin/env python3
"""Run the headless fixture command twice and fail if the deterministic summary fields differ.

  check_determinism.py [--write <file.json>] -- dotnet run --project tools/Sovereigns.Headless -c Release --no-build -- run --fixture F-EMPTY
  check_determinism.py --compare <directory>

The command must print one JSON object on stdout. --write saves the compared fields so that runs on different
operating systems can be compared afterwards with --compare, which fails unless every *.json file in the
directory (searched recursively) has identical fields (ADR-0004 §3 cross-platform measurement). Fields such as wall_ms and memory legitimately differ between runs
and are ignored. When GITHUB_STEP_SUMMARY is set, the checksum is appended there so runners can be compared by eye.
Exit codes: 0 identical, 1 differing or failing run, 2 usage error.
"""
import json
import os
import subprocess
import sys

COMPARED=('checksum','sim_time_ms','steps')

def summary_of(stdout):
    """The JSON object on the last stdout line that starts with '{'."""
    lines=[line for line in stdout.splitlines() if line.lstrip().startswith('{')]
    if not lines: raise ValueError('no JSON summary on stdout')
    value=json.loads(lines[-1])
    if not isinstance(value,dict): raise ValueError('summary is not a JSON object')
    return value

def differences(first,second):
    """Compared fields that are missing or differ between two summaries."""
    problems=[]
    for key in COMPARED:
        if key not in first or key not in second: problems.append(f'{key} missing from the summary')
        elif first[key]!=second[key]: problems.append(f'{key} differs: {first[key]!r} then {second[key]!r}')
    return problems

def run_once(command):
    done=subprocess.run(command,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf-8',errors='replace')
    if done.returncode: raise RuntimeError(f'exit code {done.returncode}\n{done.stderr}')
    return summary_of(done.stdout)

def compare(directory):
    """Fail unless every saved summary under directory has identical compared fields."""
    paths=sorted(path for path in __import__('pathlib').Path(directory).rglob('*.json'))
    if len(paths)<2:
        print(f'check_determinism: need at least two summaries under {directory}, found {len(paths)}',file=sys.stderr)
        return 1
    summaries={path.stem:json.loads(path.read_text(encoding='utf-8')) for path in paths}
    reference_name,reference=next(iter(summaries.items()))
    problems=[f'{name}: {problem}' for name,summary in summaries.items() for problem in differences(reference,summary)]
    if problems:
        print(f'check_determinism: platforms disagree with {reference_name}:\n - '+'\n - '.join(problems),file=sys.stderr)
        return 1
    print(f'identical on {", ".join(summaries)}: '+', '.join(f'{key}={reference[key]}' for key in COMPARED))
    return 0

def main(argv=None):
    argv=sys.argv[1:] if argv is None else argv
    if argv[:1]==['--compare']:
        return compare(argv[1]) if len(argv)==2 else 2
    write=None
    if argv[:1]==['--write']:
        if len(argv)<2: return 2
        write,argv=argv[1],argv[2:]
    command=argv[1:] if argv[:1]==['--'] else argv
    if not command:
        print(__doc__,file=sys.stderr)
        return 2
    try: first,second=run_once(command),run_once(command)
    except (RuntimeError,ValueError,OSError) as exc:
        print(f'check_determinism: run failed: {exc}',file=sys.stderr)
        return 1
    problems=differences(first,second)
    if problems:
        print('check_determinism: two identical runs disagree:\n - '+'\n - '.join(problems),file=sys.stderr)
        return 1
    line=', '.join(f'{key}={first[key]}' for key in COMPARED)
    print(f'deterministic: {line}')
    if write:
        os.makedirs(os.path.dirname(os.path.abspath(write)),exist_ok=True)
        with open(write,'w',encoding='utf-8') as out: json.dump({key:first[key] for key in COMPARED},out)
    if os.environ.get('GITHUB_STEP_SUMMARY'):
        with open(os.environ['GITHUB_STEP_SUMMARY'],'a',encoding='utf-8') as summary:
            summary.write(f'Determinism on {os.environ.get("RUNNER_OS","local")}: `{first["checksum"]}` after {first["steps"]} steps\n\n')
    return 0

if __name__=='__main__': sys.exit(main())
