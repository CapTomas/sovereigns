#!/usr/bin/env python3
"""Check that this machine matches the toolchain pins (tools/toolchain.json, global.json).

  python3 tools/doctor.py

Prints one line per check with a fix for anything missing; exits 1 when a required tool is missing or too old.
Export templates are optional locally (only packaging needs them), so a missing set is a warning.
"""
import json
import os
import platform
import re
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OK, WARN, FAIL = 'ok', 'warn', 'FAIL'


def parse_sdk_version(text):
    """(major, minor, feature band, patch, is_prerelease) from a .NET SDK version such as 10.0.401, or None."""
    match = re.fullmatch(r'(\d+)\.(\d+)\.(\d)(\d{2})(-[0-9A-Za-z.-]+)?', text.strip())
    if not match:
        return None
    return int(match[1]), int(match[2]), int(match[3]), int(match[4]), bool(match[5])


def sdk_satisfies(installed, pinned, roll_forward='latestPatch', allow_prerelease=True):
    """Whether an installed SDK version meets a global.json pin: the same feature band at or above the pinned patch."""
    have, want = parse_sdk_version(installed), parse_sdk_version(pinned)
    if have is None or want is None or (have[4] and not allow_prerelease):
        return False
    if roll_forward == 'disable':
        return have[:4] == want[:4]
    return have[:3] == want[:3] and have[3] >= want[3]


def godot_matches(output, version, channel, flavor):
    """Whether `godot --version` output names the pinned version, channel and flavor, e.g. 4.7.2.stable.mono.official.abc."""
    lines = output.strip().splitlines()
    return bool(lines) and lines[0].startswith(f'{version}.{channel}.{flavor}')


def templates_dir(system, home, env, folder):
    """Where the Godot editor looks for export templates of one version folder (e.g. 4.7.2.stable.mono)."""
    if system == 'Darwin':
        base = home / 'Library' / 'Application Support' / 'Godot'
    elif system == 'Windows':
        base = Path(env.get('APPDATA') or home / 'AppData' / 'Roaming') / 'Godot'
    else:
        base = Path(env.get('XDG_DATA_HOME') or home / '.local' / 'share') / 'godot'
    return base / 'export_templates' / folder


def run(command, **kwargs):
    """(exit code, stdout, stderr), or (None, '', reason) when the program cannot run."""
    try:
        done = subprocess.run(command, capture_output=True, text=True, timeout=60, **kwargs)
        return done.returncode, done.stdout, done.stderr
    except (OSError, subprocess.TimeoutExpired) as exc:
        return None, '', str(exc)


def check_python(toolchain):
    minimum = tuple(int(part) for part in toolchain['python']['minimum'].split('.'))
    found = '.'.join(map(str, sys.version_info[:3]))
    if sys.version_info[:2] >= minimum:
        return OK, f'Python {found}', ''
    return FAIL, f'Python {found}, need {toolchain["python"]["minimum"]} or newer', 'Install Python from https://www.python.org/downloads/ and rerun with it.'


def check_dotnet(root):
    sdk = json.loads((root / 'global.json').read_text(encoding='utf-8'))['sdk']
    pinned = sdk['version']
    url = 'https://dotnet.microsoft.com/download/dotnet/10.0'
    code, out, err = run(['dotnet', '--version'], cwd=root)
    if code is None:
        return FAIL, 'dotnet not found', f'Install .NET SDK {pinned} (or a later patch of the same feature band) from {url}.'
    installed = out.strip()
    if code == 0 and sdk_satisfies(installed, pinned, sdk.get('rollForward', 'latestPatch'), sdk.get('allowPrerelease', True)):
        return OK, f'.NET SDK {installed} (global.json pins {pinned})', ''
    shown = installed if code == 0 else (err.strip().splitlines() or ['dotnet --version failed'])[0]
    return FAIL, f'.NET SDK does not satisfy global.json {pinned} ({shown})', f'Install .NET SDK {pinned} (same feature band, patch {pinned} or newer) from {url}; `dotnet --list-sdks` shows what is installed.'


def godot_fix(toolchain, system):
    key = {'Darwin': 'macos', 'Windows': 'windows'}.get(system, 'linux')
    editor = toolchain['godot']['editor'][key]
    flavor = toolchain['godot']['flavor']
    fix = (f'Install the Godot {toolchain["godot"]["version"]} .NET ({flavor}) editor from {editor["url"]} (SHA-512 {editor["sha512"]}); '
           'put it on PATH as `godot` or set SOVEREIGNS_GODOT to the executable (macOS: Godot_mono.app/Contents/MacOS/Godot).')
    if key == 'linux':
        fix += ' CI-style install: tools/ci/install_godot.sh ~/.cache/sovereigns/godot'
    return fix


def check_godot(toolchain, env, system):
    pin = toolchain['godot']
    wanted = f'{pin["version"]}.{pin["channel"]}.{pin["flavor"]}'
    path = env.get('SOVEREIGNS_GODOT') or shutil.which('godot')
    if not path:
        return FAIL, 'Godot not found', godot_fix(toolchain, system)
    code, out, err = run([path, '--version'])
    first = (out.strip().splitlines() or [err.strip() or 'no output'])[0]
    if code == 0 and godot_matches(out, pin['version'], pin['channel'], pin['flavor']):
        return OK, f'Godot {first} ({path})', ''
    return FAIL, f'Godot at {path} reports "{first}", need {wanted}', godot_fix(toolchain, system)


def check_templates(toolchain, home, env, system):
    pin = toolchain['godot']
    folder = f'{pin["version"]}.{pin["channel"]}.{pin["flavor"]}'
    where = templates_dir(system, home, env, folder)
    if (where / 'version.txt').is_file():
        return OK, f'Export templates {folder} ({where})', ''
    return WARN, f'Export templates {folder} not installed (only needed to package builds)', (
        f'In the Godot editor use Editor > Manage Export Templates, or download {pin["templates"]["url"]} '
        f'(SHA-512 {pin["templates"]["sha512"]}) and extract the files from its templates/ folder into {where}.')


def check_git():
    if shutil.which('git'):
        return OK, 'git found', ''
    return FAIL, 'git not found', 'Install git from https://git-scm.com/downloads.'


def main():
    toolchain = json.loads((ROOT / 'tools' / 'toolchain.json').read_text(encoding='utf-8'))
    system, home, env = platform.system(), Path.home(), dict(os.environ)
    results = [check_python(toolchain), check_dotnet(ROOT), check_godot(toolchain, env, system),
               check_templates(toolchain, home, env, system), check_git()]
    for status, message, fix in results:
        print(f'[{status:<4}] {message}')
        if fix:
            print(f'       fix: {fix}')
    failed = [message for status, message, _ in results if status == FAIL]
    print(f'\n{len(failed)} required check(s) failed.' if failed else '\nAll required checks passed.')
    return 1 if failed else 0


if __name__ == '__main__':
    sys.exit(main())
