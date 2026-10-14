"""Version logic of tools/doctor.py: what counts as a matching .NET SDK and Godot build."""
import sys
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools'))
import doctor

class SdkRules(unittest.TestCase):
    def test_latest_patch_accepts_same_feature_band_at_or_above_pin(self):
        for installed in ('10.0.401','10.0.402','10.0.499'):
            with self.subTest(installed):
                self.assertTrue(doctor.sdk_satisfies(installed,'10.0.401'))

    def test_other_bands_versions_and_garbage_are_rejected(self):
        for installed in ('10.0.400','10.0.500','10.0.301','10.1.401','9.0.401','11.0.401','10.0','not a version',''):
            with self.subTest(installed):
                self.assertFalse(doctor.sdk_satisfies(installed,'10.0.401'))

    def test_prerelease_follows_allow_prerelease(self):
        self.assertFalse(doctor.sdk_satisfies('10.0.402-preview.1','10.0.401',allow_prerelease=False))
        self.assertTrue(doctor.sdk_satisfies('10.0.402-preview.1','10.0.401',allow_prerelease=True))

    def test_disable_requires_the_exact_version(self):
        self.assertTrue(doctor.sdk_satisfies('10.0.401','10.0.401','disable'))
        self.assertFalse(doctor.sdk_satisfies('10.0.402','10.0.401','disable'))

class GodotRules(unittest.TestCase):
    def test_pinned_build_is_recognised(self):
        self.assertTrue(doctor.godot_matches('4.7.2.stable.mono.official.ed1daf0bf\n','4.7.2','stable','mono'))

    def test_wrong_version_channel_or_flavor_is_rejected(self):
        for output in ('4.7.2.stable.official.ed1daf0bf','4.7.3.stable.mono.official.x','4.7.2.beta1.mono.official.x','4.7.20.stable.mono.x','','warning only'):
            with self.subTest(output):
                self.assertFalse(doctor.godot_matches(output,'4.7.2','stable','mono'))

class TemplatePaths(unittest.TestCase):
    def test_template_folder_per_system(self):
        home=Path('/home/u')
        folder='4.7.2.stable.mono'
        self.assertEqual(doctor.templates_dir('Linux',home,{},folder),home/'.local/share/godot/export_templates'/folder)
        self.assertEqual(doctor.templates_dir('Linux',home,{'XDG_DATA_HOME':'/data'},folder),Path('/data/godot/export_templates')/folder)
        self.assertEqual(doctor.templates_dir('Darwin',home,{},folder),home/'Library/Application Support/Godot/export_templates'/folder)
        self.assertEqual(doctor.templates_dir('Windows',home,{'APPDATA':'/roaming'},folder),Path('/roaming/Godot/export_templates')/folder)

if __name__=='__main__': unittest.main()
