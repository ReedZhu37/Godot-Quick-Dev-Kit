"""Packaging, isolation, resource-path and extracted empty-project checks."""
import importlib.util
import pathlib
import re
import tempfile
import unittest
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('package', ROOT / 'tools/package.py')
package = importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)


class Packages(unittest.TestCase):
    def test_all_bundles_and_individual_modules(self):
        with tempfile.TemporaryDirectory() as directory:
            for language in package.LANGUAGES:
                for module in [None] + [p.name for p in package.modules(language)]:
                    with self.subTest(language=language, module=module):
                        output = pathlib.Path(directory) / f'{language}-{module}.zip'
                        files = package.build(language, output, module)
                        original = output.read_bytes()
                        package.build(language, output, module)
                        self.assertEqual(original, output.read_bytes())
                        with zipfile.ZipFile(output) as archive:
                            names = archive.namelist()
                            self.assertTrue(all(n.startswith('addons/quick_') for n in names))
                            self.assertFalse(any('/tests/' in n or '/demos/' in n for n in names))
                            if language == 'gdscript':
                                self.assertFalse(any(n.endswith('.cs') for n in names))
                            if module:
                                self.assertTrue(all(n.startswith(f'addons/{module}/') for n in names))
                            project = pathlib.Path(directory) / f'empty-{language}-{module}'
                            project.mkdir()
                            (project / 'project.godot').write_text('config_version=5\n')
                            archive.extractall(project)
                            for name in files:
                                self.assertEqual((project / name).read_bytes(), files[name].read_bytes())

    def test_project_resource_paths(self):
        for language in package.LANGUAGES:
            root = ROOT / 'implementations' / language
            for path in root.rglob('*'):
                if not path.is_file() or '.godot' in path.parts:
                    continue
                if path.suffix not in ('.tscn', '.tres', '.godot'):
                    continue
                for target in re.findall(r'res://([^"\s]+)', path.read_text()):
                    self.assertTrue((root / target).is_file(), f'{path}: {target}')

    def test_root_is_not_a_godot_project(self):
        self.assertFalse((ROOT / 'project.godot').exists())
        self.assertFalse(list(ROOT.glob('*.csproj')))


if __name__ == '__main__':
    unittest.main()
