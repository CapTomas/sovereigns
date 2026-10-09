# Drop-in installation

Extract the archive at the **root of the existing Sovereigns repository**, preserving paths:

```bash
unzip SOVEREIGNS_VISUAL_DIRECTION_FOLDER.zip -d .
```

The archive contains `docs/visual/` and replaces the former `docs/visual/README.md` placeholder. It does not modify `docs/spec/`, the gameplay authority, the architecture ADRs, the roadmap, engine files or runtime assets. Version-control the new folder.

## Agent entry

1. Keep the repository root `AGENTS.md` as the entrypoint.
2. A visual task should read `docs/visual/README.md`, then use:

```bash
python3 docs/visual/visual_context.py --topics
python3 docs/visual/visual_context.py soldiers
```

3. Read the affected `docs/spec` chapter(s), with particular attention to §27 presentation boundaries. The router is intentionally minimal and never supersedes task dependencies.
4. Deliver source/exports/review evidence as required by `docs/visual/12-ASSET-PIPELINE-AND-GOVERNANCE.md` and `docs/visual/13-LOOKDEV-AND-VISUAL-QA.md`.

Optional documentation housekeeping: change the root `docs/README.md` entry describing `visual/README.md` as “intentionally separate future visual authority” to “authoritative visual direction, asset standards and look-dev acceptance.” That is a documentation link description only. No gameplay specification should be rewritten to insert a palette or sprite style.

## Validate

```bash
python3 docs/visual/visual_context.py --topics
python3 docs/visual/visual_context.py terrain
python3 tools/validate_repo.py
```

The last command must be executed in the real repository. Its scope depends on the version of the repo tools. Production art is **not** implied by installing these specifications; scene-render and performance tests must be carried out as systems are built.
