# Repository tools

These scripts require Python 3.11+, use only its standard library and run offline. Run commands from the repository root. No game executable is implemented yet; documentation checks cannot establish gameplay correctness.

| Need | Command |
| --- | --- |
| Orient without a task ID | `python3 tools/task_context.py` |
| Find a task by topic | `python3 tools/task_context.py --search "save load"` |
| Narrow discovery to a phase | `python3 tools/task_context.py --search "test" --phase 0` |
| Browse all tasks in one phase | `python3 tools/task_context.py --phase 0 --limit 30` |
| Locate task context | `python3 tools/task_context.py SOV-P00-T01 --list` |
| Read only task and exit gate | `python3 tools/task_context.py SOV-P00-T01 --task` |
| Read one exact spec section | `python3 tools/task_context.py --section 3.5` |
| Find instructions for intended edits | `python3 tools/task_context.py --path tools/` |
| Include edit instructions in task context | `python3 tools/task_context.py SOV-P00-T01 --path tools/task_context.py --json` |
| Validate routing, hashes, ID order, fixtures, evidence for checked tasks and links | `python3 tools/validate_repo.py` |
| Run tooling regressions and repository validation | `python3 -m unittest discover -s tools/tests -v` |
| Refresh manifests after reviewed content changes | `python3 tools/update_manifests.py` |

Search matches all whitespace-separated words, ignoring case, in task IDs, titles and phase paths. Results are ordered by ID and limited to ten by default. Search does not infer readiness or completion from generated status fields; phase files and accepted evidence govern status. `--path` may be repeated and includes ancestor `AGENTS.md` instructions for existing or planned paths inside this repository.

Read exact sections before whole chapters. `--bundle` prints all startup instructions, task context and routed chapters; reserve it for an isolated handoff that needs that context. It can be large.

Check according to the change: a prose correction can need only link/structure validation; routing or CLI changes need the tooling suite. The suite already invokes `validate_repo.check()`, so running the standalone validator afterward adds no evidence. Regenerate manifests only when their source content changes; regeneration preserves existing routes, so new task routes still need explicit authoring.
