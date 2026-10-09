#!/usr/bin/env python3
"""Run a command, keep its combined output in a log, and fail on a bad exit code or on required/forbidden output.

  run_checked.py --log artifacts/logs/x.log [--timeout 300] [--forbid TEXT]... [--require TEXT]... -- COMMAND [ARGS...]

Exit codes: 0 all expectations met, 1 an expectation failed, 2 usage error.
"""
import argparse
import subprocess
import sys
from pathlib import Path

def evaluate(returncode,output,forbid=(),require=()):
    """Violated expectations for one finished run; empty means it passed."""
    problems=[f'exited with code {returncode}, expected 0'] if returncode else []
    problems.extend(f'output contains forbidden text {text!r}' for text in forbid if text in output)
    problems.extend(f'output lacks required text {text!r}' for text in require if text not in output)
    return problems

def run(command,timeout):
    """(exit code, combined output); a timeout is reported as exit code 124 with the output so far."""
    try:
        done=subprocess.run(command,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,timeout=timeout)
        return done.returncode,done.stdout.decode('utf-8','replace')
    except subprocess.TimeoutExpired as exc:
        return 124,(exc.stdout or b'').decode('utf-8','replace')+f'\n[run_checked] timed out after {timeout}s\n'
    except OSError as exc:
        return 127,f'[run_checked] cannot start {command[0]}: {exc}\n'

def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__,formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--log',required=True,type=Path,help='file that receives the combined output')
    parser.add_argument('--timeout',type=float,default=300,help='seconds before the command is killed (default 300)')
    parser.add_argument('--forbid',action='append',default=[],help='text that must not appear in the output')
    parser.add_argument('--require',action='append',default=[],help='text that must appear in the output')
    parser.add_argument('command',nargs=argparse.REMAINDER)
    args=parser.parse_args(argv)
    command=args.command[1:] if args.command[:1]==['--'] else args.command
    if not command: parser.error('give the command after --')
    code,output=run(command,args.timeout)
    args.log.parent.mkdir(parents=True,exist_ok=True)
    args.log.write_text(output,encoding='utf-8')
    sys.stdout.write(output)
    problems=evaluate(code,output,args.forbid,args.require)
    for problem in problems: print(f'[run_checked] {" ".join(command[:1])}: {problem} (log: {args.log})',file=sys.stderr)
    return 1 if problems else 0

if __name__=='__main__': sys.exit(main())
