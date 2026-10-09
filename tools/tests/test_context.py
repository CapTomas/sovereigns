"""Validation tests for the Phase 00 agentic context tooling."""
import sys
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools'))
import task_context
import validate_repo

class TestContext(unittest.TestCase):
    def test_repository(self):
        self.assertTrue(validate_repo.check())

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

if __name__=='__main__': unittest.main()
