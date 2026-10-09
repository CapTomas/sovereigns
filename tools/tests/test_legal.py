"""Registry, toolchain-pin and workflow-matrix rules of validate_repo, exercised on temporary trees."""
import copy
import json
import sys
import tempfile
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools'))
import validate_repo

SHA='a'*128
PROPS='<Project><ItemGroup><PackageVersion Include="Foo.Bar" Version="1.2.3" /></ItemGroup></Project>'
LOCK={'version':2,'dependencies':{'net10.0':{
    'Foo.Bar':{'type':'Direct','requested':'[1.2.3, )','resolved':'1.2.3'},
    'Transit':{'type':'Transitive','resolved':'0.5.0'},
    'other.project':{'type':'Project'}}}}
REGISTRY={
    'allowed_licenses':['MIT','CC-BY-4.0','LicenseRef-Project-Original'],
    'dependencies':[
        {'name':'Foo.Bar','version':'1.2.3','kind':'nuget','scope':'runtime','license':'MIT','source':'u','copyright':'(c) Foo'},
        {'name':'Transit','version':'0.5.0','kind':'nuget','scope':'runtime','license':'MIT','source':'u','copyright':'(c) T'},
        {'name':'Godot.NET.Sdk','version':'4.7.2','kind':'nuget','scope':'build','license':'MIT','source':'u'},
        {'name':'GodotSharp','version':'4.7.2','kind':'nuget','via':'Godot.NET.Sdk','scope':'runtime','license':'MIT','source':'u','copyright':'(c) G'},
        {'name':'Engine','version':'4.7.2','kind':'component','toolchain':'godot.version','scope':'runtime','license':'MIT','source':'u','copyright':'(c) G'}],
    'datasets':[],
    'assets':[{'path':'game/icon.svg','license':'LicenseRef-Project-Original','source':'original'}]}
TOOLCHAIN={'dotnet':{'sdk':'10.0.401'},'godot':{'version':'4.7.2','channel':'stable','flavor':'mono',
    'editor':{os:{'url':f'https://github.com/godotengine/godot/releases/download/4.7.2-stable/{os}.zip','sha512':SHA} for os in ('linux','macos','windows')},
    'templates':{'url':'https://github.com/godotengine/godot/releases/download/4.7.2-stable/t.tpz','sha512':SHA}}}

def write(root,rel,text):
    path=root/rel
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(text if isinstance(text,str) else json.dumps(text),encoding='utf-8')

def make_tree(root,registry=REGISTRY,toolchain=TOOLCHAIN):
    write(root,'Directory.Packages.props',PROPS)
    write(root,'src/App/App.csproj','<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Foo.Bar" /></ItemGroup></Project>')
    write(root,'src/App/packages.lock.json',LOCK)
    write(root,'game/Client.csproj','<Project Sdk="Godot.NET.Sdk/4.7.2" />')
    write(root,'game/icon.svg','<svg/>')
    write(root,'game/icon.svg.import','generated')
    write(root,'game/.godot/imported/icon.png','cache')
    write(root,'global.json',{'sdk':{'version':'10.0.401'}})
    write(root,'tools/toolchain.json',toolchain)
    write(root,'docs/legal/third-party.json',registry)

class RegistryRules(unittest.TestCase):
    def errors(self,mutate_registry=None,mutate_tree=None):
        registry=copy.deepcopy(REGISTRY)
        if mutate_registry: mutate_registry(registry)
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder)
            make_tree(root,registry)
            if mutate_tree: mutate_tree(root)
            return validate_repo.legal_errors(root)

    def remove(self,name):
        return lambda registry: registry['dependencies'].__setitem__(slice(None),[d for d in registry['dependencies'] if d['name']!=name])

    def assertFails(self,expected,*args):
        errors=self.errors(*args)
        self.assertTrue(any(expected in error for error in errors),f'expected {expected!r} in {errors}')

    def test_consistent_tree_passes(self):
        self.assertEqual(self.errors(),[])

    def test_unregistered_references_fail(self):
        self.assertFails('Foo.Bar 1.2.3 has no entry',self.remove('Foo.Bar'))
        self.assertFails('Godot.NET.Sdk 4.7.2 has no entry',self.remove('Godot.NET.Sdk'))
        self.assertFails('Transit 0.5.0 has no entry',self.remove('Transit'))
        self.assertFails('Foo.Bar 1.2.4 has no entry',None,lambda root: write(root,'Directory.Packages.props',PROPS.replace('1.2.3','1.2.4')))
        self.assertFails('Baz 2.0.0 has no entry',None,lambda root: write(
            root,'src/App/Other.csproj','<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Baz" Version="2.0.0" /></ItemGroup></Project>'))

    def test_disallowed_license_fails(self):
        self.assertFails('license GPL-3.0-only is not allowed',lambda registry: registry['dependencies'][0].update(license='GPL-3.0-only'))
        self.assertFails('license GPL-3.0-only is not allowed',lambda registry: registry['assets'][0].update(license='GPL-3.0-only'))

    def test_stale_entries_fail(self):
        gone={'name':'Gone','version':'1.0.0','kind':'nuget','scope':'test','license':'MIT','source':'u'}
        self.assertFails('Gone 1.0.0 is no longer referenced',lambda registry: registry['dependencies'].append(gone))
        self.assertFails('GodotSharp 4.7.2 is no longer referenced',None,lambda root: write(root,'game/Client.csproj','<Project Sdk="Microsoft.NET.Sdk" />'))
        self.assertFails('asset game/gone.svg does not exist',lambda registry: registry['assets'].append(
            {'path':'game/gone.svg','license':'MIT','source':'u','attribution':'x'}))

    def test_assets_need_entries(self):
        self.assertFails('game/new.png: audiovisual asset has no entry',None,lambda root: write(root,'game/new.png','x'))
        self.assertFails('content/core/sfx/hit.ogg: audiovisual asset',None,lambda root: write(root,'content/core/sfx/hit.ogg','x'))
        self.assertEqual(self.errors(lambda registry: registry['assets'].append(
            {'path':'content/core/sfx/','license':'CC-BY-4.0','source':'u','attribution':'Someone'}),
            lambda root: write(root,'content/core/sfx/hit.ogg','x')),[])

    def test_notice_fields_are_required(self):
        self.assertFails('need a copyright notice',lambda registry: registry['dependencies'][0].pop('copyright'))
        self.assertFails('requires attribution text',lambda registry: registry['assets'].append(
            {'path':'game/icon.svg','license':'CC-BY-4.0','source':'u'}))
        self.assertFails('missing source',lambda registry: registry['assets'][0].pop('source'))

    def test_pinned_component_must_match_toolchain(self):
        self.assertFails('differs from tools/toolchain.json godot.version',lambda registry: registry['dependencies'][4].update(version='4.8.0'))

    def test_missing_registry_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            self.assertTrue(validate_repo.legal_errors(Path(folder)))

class ToolchainRules(unittest.TestCase):
    def errors(self,mutate=None):
        toolchain=copy.deepcopy(TOOLCHAIN)
        if mutate: mutate(toolchain)
        with tempfile.TemporaryDirectory() as folder:
            make_tree(Path(folder),toolchain=toolchain)
            return validate_repo.toolchain_errors(Path(folder))

    def test_consistent_pins_pass(self):
        self.assertEqual(self.errors(),[])

    def test_drifting_pins_fail(self):
        for name,mutate,expected in [
                ('sdk',lambda t: t['dotnet'].update(sdk='10.0.402'),'must equal global.json'),
                ('checksum',lambda t: t['godot']['templates'].update(sha512='abc'),'godot.templates.sha512'),
                ('url',lambda t: t['godot']['editor']['linux'].update(url='https://example.com/g.zip'),'godot.editor.linux.url'),
                ('version',lambda t: t['godot'].update(version='4.8.0'),'differs from tools/toolchain.json godot.version')]:
            with self.subTest(name):
                self.assertTrue(any(expected in error for error in self.errors(mutate)),self.errors(mutate))

class WorkflowMatrix(unittest.TestCase):
    JOB='jobs:\n  sim:\n    name: sim (${{ matrix.os }})\n    strategy:\n      fail-fast: false\n      matrix:\n        os: [a-1, "b-2"]\n    steps:\n      - name: s\n  plain:\n    runs-on: x\n'

    def test_literal_matrix_names_expand(self):
        self.assertEqual(validate_repo.workflow_checks(self.JOB),{'sim','sim (a-1)','sim (b-2)','plain'})

    def test_block_lists_and_several_keys_expand(self):
        text='jobs:\n  t:\n    name: t ${{ matrix.os }} ${{ matrix.v }}\n    strategy:\n      matrix:\n        os:\n          - a\n          - b\n        v: [1]\n    runs-on: x\n'
        self.assertEqual(validate_repo.workflow_checks(text),{'t','t a 1','t b 1'})

    def test_unknown_matrix_values_stay_literal(self):
        text='jobs:\n  t:\n    name: t ${{ matrix.os }}\n    strategy:\n      matrix:\n        os: ${{ fromJson(inputs.os) }}\n'
        self.assertEqual(validate_repo.workflow_checks(text),{'t','t ${{ matrix.os }}'})

    def test_ruleset_accepts_expanded_names(self):
        ruleset={'rules':[{'type':'required_status_checks','parameters':{'required_status_checks':[
            {'context':'sim (a-1)'},{'context':'sim (c-3)'}]}}]}
        errors=validate_repo.ruleset_errors('r.json',ruleset,validate_repo.workflow_checks(self.JOB))
        self.assertEqual(len(errors),1)
        self.assertIn('sim (c-3)',errors[0])

if __name__=='__main__': unittest.main()
