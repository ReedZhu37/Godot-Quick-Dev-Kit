"""Protect language selection and integrated compilation from accidental regressions."""
import configparser
from pathlib import Path
import re
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
DUAL = ("quick_input", "quick_audio_manager", "quick_database")


class LanguageLabels(unittest.TestCase):
    def test_dual_plugins_have_explicit_language_names(self):
        for module in DUAL:
            for folder, label in (("csharp", "C#"), ("gds", "GDScript")):
                with self.subTest(module=module, language=label):
                    config = configparser.ConfigParser()
                    config.read(ROOT / "addons" / module / folder / "plugin.cfg")
                    self.assertIn(f"({label})", config["plugin"]["name"])
                    self.assertTrue((ROOT / "addons" / module / folder / "README.md").is_file())

    def test_single_language_modules_are_marked(self):
        for relative, label in (("quick_vfx_manager/csharp", "C#"), ("quick_level_kit", "GDScript")):
            config = configparser.ConfigParser()
            config.read(ROOT / "addons" / relative / "plugin.cfg")
            self.assertIn(f"({label})", config["plugin"]["name"])

    def test_every_csharp_source_is_compiled_once(self):
        project = ET.parse(ROOT / "QuickVfxDemo.csproj")
        self.assertEqual(project.findtext(".//EnableDefaultCompileItems"), "false")
        globs = [node.attrib["Include"] for node in project.findall(".//Compile")]
        covered = [path for pattern in globs for path in ROOT.glob(pattern)]
        expected = set((ROOT / "addons").rglob("*.cs"))
        self.assertEqual(set(covered), expected)
        self.assertEqual(len(covered), len(expected))

    def test_language_entry_document_links_exist(self):
        for name in ("README.md", "CSHARP.md"):
            text = (ROOT / name).read_text()
            self.assertIn("C#", text)
            self.assertIn("GDScript", text)
            for target in re.findall(r"\[[^\]]*\]\(([^)]+)\)", text):
                if "://" not in target and not target.startswith("#"):
                    self.assertTrue((ROOT / target.split("#")[0]).exists(), (name, target))


if __name__ == "__main__":
    unittest.main()
