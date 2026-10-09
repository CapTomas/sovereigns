"""CI helper scripts in tools/ci: the checks that decide whether the pipeline fails."""
import contextlib
import io
import json
import sys
import tempfile
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools'/'ci'))
import check_determinism
import run_checked
import write_notices

def python(code): return [sys.executable,'-c',code]

class RunChecked(unittest.TestCase):
    def run_main(self,*args):
        with tempfile.TemporaryDirectory() as folder, contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            log=Path(folder)/'logs'/'x.log'
            code=run_checked.main(['--log',str(log),*args])
            return code,log.read_text(encoding='utf-8')

    def test_evaluate_reports_each_violation(self):
        self.assertEqual(run_checked.evaluate(0,'ok world created',['leaked'],['world created']),[])
        self.assertEqual(len(run_checked.evaluate(3,'ObjectDB instances leaked at exit',['leaked at exit'],['world created'])),3)

    def test_clean_run_passes_and_is_logged(self):
        code,log=self.run_main('--require','world created','--',*python('print("world created")'))
        self.assertEqual((code,log.strip()),(0,'world created'))

    def test_failing_exit_code_forbidden_and_missing_text_fail(self):
        self.assertEqual(self.run_main('--',*python('import sys; sys.exit(4)'))[0],1)
        self.assertEqual(self.run_main('--forbid','leaked at exit','--',*python('print("1 leaked at exit")'))[0],1)
        self.assertEqual(self.run_main('--require','world created','--',*python('print("nothing")'))[0],1)

    def test_arguments_after_the_separator_reach_the_command(self):
        code,log=self.run_main('--',*python('import sys; print(sys.argv[1:])'),'--headless','--forbid')
        self.assertEqual((code,log.strip()),(0,"['--headless', '--forbid']"))

    def test_timeout_and_missing_program_fail(self):
        self.assertEqual(self.run_main('--timeout','0.5','--',*python('import time; time.sleep(30)'))[0],1)
        self.assertEqual(self.run_main('--','definitely-not-a-program-987')[0],1)

class Determinism(unittest.TestCase):
    SUMMARY={'checksum':'abc','sim_time_ms':10,'steps':10,'wall_ms':1.5}

    def test_only_compared_fields_matter(self):
        self.assertEqual(check_determinism.differences(self.SUMMARY,{**self.SUMMARY,'wall_ms':9.9}),[])
        self.assertEqual(len(check_determinism.differences(self.SUMMARY,{**self.SUMMARY,'checksum':'def'})),1)
        self.assertEqual(len(check_determinism.differences(self.SUMMARY,{'checksum':'abc'})),2)

    def test_summary_is_the_last_json_line(self):
        self.assertEqual(check_determinism.summary_of('noise\n'+json.dumps(self.SUMMARY)+'\n'),self.SUMMARY)
        with self.assertRaises(ValueError): check_determinism.summary_of('no json')

    def run_main(self,code):
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            return check_determinism.main(['--',*python(code)])

    def test_compare_requires_identical_summaries_from_every_platform(self):
        with tempfile.TemporaryDirectory() as directory:
            for name,checksum in [('ubuntu','abc'),('windows','abc')]:
                (Path(directory)/name).mkdir()
                (Path(directory)/name/f'{name}.json').write_text(json.dumps({**self.SUMMARY,'checksum':checksum}),encoding='utf-8')
            with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
                self.assertEqual(check_determinism.main(['--compare',directory]),0)
                (Path(directory)/'macos.json').write_text(json.dumps({**self.SUMMARY,'checksum':'zzz'}),encoding='utf-8')
                self.assertEqual(check_determinism.main(['--compare',directory]),1)
            (Path(directory)/'ubuntu'/'ubuntu.json').unlink()
            (Path(directory)/'macos.json').unlink()
            with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
                self.assertEqual(check_determinism.main(['--compare',directory]),1)

    def test_write_saves_the_compared_fields(self):
        with tempfile.TemporaryDirectory() as directory:
            out=Path(directory)/'sub'/'os.json'
            code=f'import json; print(json.dumps({self.SUMMARY!r}))'
            with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
                self.assertEqual(check_determinism.main(['--write',str(out),'--',*python(code)]),0)
            self.assertEqual(json.loads(out.read_text(encoding='utf-8')),{'checksum':'abc','sim_time_ms':10,'steps':10})

    def test_stable_command_passes_and_drifting_or_failing_command_fails(self):
        self.assertEqual(self.run_main('import json; print(json.dumps({"checksum":"a","sim_time_ms":1,"steps":1,"wall_ms":2}))'),0)
        self.assertEqual(self.run_main('import json,random; print(json.dumps({"checksum":random.random(),"sim_time_ms":1,"steps":1}))'),1)
        self.assertEqual(self.run_main('import sys; sys.exit(1)'),1)

class Notices(unittest.TestCase):
    def test_registry_renders_shipped_items_only(self):
        registry={'dependencies':[
            {'name':'Engine','version':'1.0','scope':'runtime','license':'MIT','copyright':'(c) E'},
            {'name':'xunit','version':'3','scope':'test','license':'Apache-2.0'}],
            'datasets':[],'assets':[{'path':'game/a.png','license':'CC-BY-4.0','attribution':'By Someone'}]}
        text=write_notices.render(registry)
        for shipped in ('Engine 1.0 - MIT','(c) E','game/a.png - CC-BY-4.0','By Someone'): self.assertIn(shipped,text)
        self.assertNotIn('xunit',text)

if __name__=='__main__': unittest.main()
