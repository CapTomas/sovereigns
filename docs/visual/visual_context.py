#!/usr/bin/env python3
"""Print minimal visual design context file paths for a requested work topic.

Usage: python3 docs/visual/visual_context.py terrain
       python3 docs/visual/visual_context.py --topics
       python3 docs/visual/visual_context.py terrain --show

Read the root repository AGENTS.md and affected gameplay specifications too.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def run() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("topic", nargs="?", help="Visual work topic, see --topics")
    parser.add_argument("--topics", action="store_true", help="List all known topics")
    parser.add_argument("--show", action="store_true", help="Print context document contents")
    args = parser.parse_args()
    with (ROOT / "VISUAL_CONTEXT.json").open(encoding="utf-8") as stream:
        context = json.load(stream)
    if args.topics:
        print("\n".join(sorted(context["topics"])))
        return 0
    if not args.topic or args.topic not in context["topics"]:
        parser.error("Choose a known topic: " + ", ".join(sorted(context["topics"])))
    file_names = list(dict.fromkeys(context["always"] + context["topics"][args.topic]))
    for name in file_names:
        path = ROOT / name
        if not path.is_file():
            raise FileNotFoundError(path)
        print("docs/visual/" + name)
        if args.show:
            print("\n--- " + name + " ---\n")
            print(path.read_text(encoding="utf-8"))
    return 0


if __name__ == "__main__":
    raise SystemExit(run())
