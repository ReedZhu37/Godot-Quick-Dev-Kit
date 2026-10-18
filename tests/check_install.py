#!/usr/bin/env python3
"""Import each runtime-only module in a fresh Godot project (no demos/tests)."""
import argparse
import importlib.util
import pathlib
import subprocess
import tempfile
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('package', ROOT / 'tools/package.py')
package = importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--language', choices=package.LANGUAGES, required=True)
parser.add_argument('--godot', default='godot')
parser.add_argument('--dotnet', default='dotnet')
args = parser.parse_args()
for module in [None] + [p.name for p in package.modules(args.language)]:
    with tempfile.TemporaryDirectory(prefix='quick-install-') as work:
        root = pathlib.Path(work)
        archive = root / 'addon.zip'
        package.build(args.language, archive, module)
        with zipfile.ZipFile(archive) as source:
            source.extractall(root)
        plugins = sorted(p.relative_to(root).as_posix() for p in root.glob('addons/**/plugin.cfg'))
        config = 'config_version=5\n[application]\nconfig/name="Package installation check"\n'
        if args.language == 'csharp':
            config += 'config/features=PackedStringArray("4.7", "C#")\n[dotnet]\nproject/assembly_name="InstallCheck"\n'
            (root / 'InstallCheck.csproj').write_text('<Project Sdk="Godot.NET.Sdk/4.7.2"><PropertyGroup><TargetFramework>net9.0</TargetFramework><EnableDynamicLoading>true</EnableDynamicLoading><Nullable>enable</Nullable></PropertyGroup></Project>')
            subprocess.run([args.dotnet, 'build', str(root / 'InstallCheck.csproj')], check=True, timeout=180)
        config += '[editor_plugins]\nenabled=PackedStringArray(' + ', '.join('"res://' + p + '"' for p in plugins) + ')\n'
        (root / 'project.godot').write_text(config)
        result = subprocess.run([args.godot, '--headless', '--path', work, '--editor', '--quit'], text=True, capture_output=True, timeout=90)
        if result.returncode or 'SCRIPT ERROR:' in result.stdout + result.stderr or '\nERROR:' in result.stdout + result.stderr:
            raise RuntimeError(f'{module}: {result.stdout}\n{result.stderr}')
        print(f'PASS isolated installed import: {args.language}/{module or "all"}')
