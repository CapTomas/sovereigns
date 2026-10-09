"""Validation tests for the Phase 00 agentic context tooling."""
import sys
import json
import subprocess
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools'))
import task_context
import validate_repo

class TestContext(unittest.TestCase):
    def test_repository(self):
        self.assertTrue(validate_repo.check())

    def test_task_id_order(self):
        self.assertEqual(validate_repo.task_id_errors('03',['SOV-P03-T01','SOV-P03-T02']),[])
        for ids in (['SOV-P03-T01','SOV-P03-T03'],['SOV-P03-T02','SOV-P03-T01'],['SOV-P04-T01']):
            with self.subTest(ids=ids):
                self.assertTrue(validate_repo.task_id_errors('03',ids))

    def test_checked_task_needs_evidence(self):
        self.assertEqual(validate_repo.evidence_errors('00',[],None),[])
        self.assertTrue(validate_repo.evidence_errors('00',['SOV-P00-T01'],None))
        self.assertTrue(validate_repo.evidence_errors('00',['SOV-P00-T01'],'Body mentions SOV-P00-T01 only.'))
        self.assertTrue(validate_repo.evidence_errors('00',['SOV-P00-T10'],'## SOV-P00-T100'))
        self.assertEqual(validate_repo.evidence_errors('00',['SOV-P00-T01'],'## SOV-P00-T01, SOV-P00-T02 — ADRs'),[])
        self.assertTrue(validate_repo.evidence_errors('00',['SOV-P00-T01'],'```\n# SOV-P00-T01\n```'))

    def test_ruleset_checks_match_workflow_jobs(self):
        workflow='name: CI\non: push\njobs:\n  docs:\n    runs-on: x\n    steps:\n      - name: step\n  build:\n    name: Build game\n'
        checks=validate_repo.workflow_checks(workflow)
        self.assertEqual(checks,{'docs','build','Build game'})
        ruleset={'rules':[{'type':'required_status_checks',
                           'parameters':{'required_status_checks':[{'context':'docs'},{'context':'missing'}]}}]}
        self.assertEqual(len(validate_repo.ruleset_errors('r.json',ruleset,checks)),1)
        self.assertTrue(validate_repo.ruleset_errors('r.json',{},checks))

    def test_fixture_manifest_rules(self):
        blank={'id':'F-X','version':1,'status':'blank','purpose':'p','seed':None,'scenario':None}
        self.assertEqual(validate_repo.fixture_errors('F-X',blank),[])
        for change in ({'id':'F-Y'},{'version':0},{'version':True},{'purpose':' '},{'seed':7},
                       {'status':'done'},{'status':'defined'},{'status':'defined','seed':7,'scenario':'no/such/file'},
                       {'status':'defined','seed':7,'scenario':'../outside.json'}):
            with self.subTest(change=change):
                self.assertTrue(validate_repo.fixture_errors('F-X',{**blank,**change}))

    def test_spec_version_picks_newest(self):
        record='**1.1.0 · 2026-10-08:** a\n**1.10.0 · 2027-01-02:** b\n**1.9.3 · 2026-12-01:** c'
        self.assertEqual(validate_repo.spec_version(record),('1.10.0','2027-01-02'))
        self.assertIsNone(validate_repo.spec_version('no entries'))

    def test_task_context(self):
        task=task_context.select_task('SOV-P00-T01')
        self.assertIn(0,task['spec_chapters'])
        self.assertIn('SOV-P00-T01',task_context.task_excerpt(task))

    def test_physics_route(self):
        task=task_context.select_task('SOV-P07-T01')
        self.assertTrue({3,5}.issubset(set(task['spec_chapters'])))

    def test_section(self):
        text=task_context.slice_section('3.5')
        self.assertIn('3.5',text)
        self.assertNotIn('3.6',text)

    def test_all_references(self):
        for task in task_context.load('meta/tasks.json').values():
            self.assertTrue((ROOT/task['phase_file']).exists())
            for chapter in task_context.scoped_specs(task):
                self.assertTrue((ROOT/chapter['path']).exists())

    def test_unknown_task_error(self):
        with self.assertRaises(ValueError): task_context.select_task('SOV-P00-T999')

    def test_task_discovery(self):
        matches = task_context.search_tasks('SOV-p00 TEST', phase=0)
        self.assertTrue(matches)
        self.assertTrue(all(task['phase'] == 0 and 'test' in task['title'].lower() for task in matches))
        self.assertIn('SOV-P00-T11', [task['id'] for task in matches])
        self.assertEqual(task_context.search_tasks('no-such-topic-987654'), [])

    def run_cli(self, *args):
        return subprocess.run([sys.executable, str(ROOT/'tools/task_context.py'), *args],
                              cwd=ROOT, text=True, capture_output=True)

    def test_cli_overview_and_bounded_discovery(self):
        overview = self.run_cli()
        self.assertEqual(overview.returncode, 0, overview.stderr)
        self.assertIn('--search', overview.stdout)
        result = self.run_cli('--phase', '0', '--limit', '2')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(sum(line.startswith('SOV-P') for line in result.stdout.splitlines()), 2)
        self.assertIn('30 matches; showing 2', result.stdout)

    def test_scoped_instruction_context(self):
        result = self.run_cli('SOV-P00-T01', '--path', 'tools/planned/script.py', '--json')
        self.assertEqual(result.returncode, 0, result.stderr)
        context = json.loads(result.stdout)
        self.assertEqual(context['instructions'],
                         ['AGENTS.md', 'tasks/AGENTS.md', 'tools/AGENTS.md', 'docs/spec/AGENTS.md'])
        self.assertTrue(context['spec_paths'])

    def test_cli_invalid_requests(self):
        for args, error in [
            (('--search', ' '), 'at least one word'),
            (('--phase', '55'), 'unknown phase'),
            (('--limit', '0'), 'must be positive'),
            (('--path', '../outside'), 'inside the repository'),
            (('SOV-P00-T01', '--search', 'save'), 'cannot be combined'),
        ]:
            with self.subTest(args=args):
                result = self.run_cli(*args)
                self.assertEqual(result.returncode, 2)
                self.assertIn(error, result.stderr)

if __name__=='__main__': unittest.main()
