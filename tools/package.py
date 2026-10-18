#!/usr/bin/env python3
"""Build deterministic, runtime-only archives; never publish a release."""
import argparse
import pathlib
import re
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
LANGUAGES = ('gdscript', 'csharp')


def modules(language):
    return sorted((ROOT / 'implementations' / language / 'addons').iterdir())


def runtime_files(language, module=None):
    project = ROOT / 'implementations' / language
    selected = [p for p in modules(language) if module is None or p.name == module]
    if not selected:
        raise ValueError(f'No {module} implementation in {language}')
    return {p.relative_to(project).as_posix(): p for addon in selected
            for p in sorted(addon.rglob('*')) if p.is_file()
            and not any(part in ('.godot', 'bin', 'obj', '__pycache__') for part in p.parts)}


def validate(files):
    """Check literal Godot resource dependencies within an installable selection."""
    for name, path in files.items():
        if path.suffix not in ('.gd', '.cs', '.tscn', '.tres', '.cfg', '.gdshader'):
            continue
        content = path.read_text()
        for target in re.findall(r'res://([^"\s\)]+)', content):
            # Ignore directory prefixes and formatted runtime paths.
            if target.endswith('/') or '%' in target or '{' in target:
                continue
            if target not in files:
                raise ValueError(f'{name}: missing package resource {target}')
        if path.name == 'plugin.cfg':
            match = re.search(r'^script="([^"]+)"', content, re.M)
            if match and (path.parent / match[1]).relative_to(
                    ROOT / 'implementations' / path.parts[path.parts.index('implementations') + 1]
            ).as_posix() not in files:
                raise ValueError(f'{name}: missing plugin script')


def build(language, output, module=None):
    files = runtime_files(language, module)
    validate(files)
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as archive:
        for name, path in sorted(files.items()):
            info = zipfile.ZipInfo(name, (2020, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            archive.writestr(info, path.read_bytes())
    return files


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--language', choices=LANGUAGES, required=True)
    parser.add_argument('--module', help='Optional quick_* folder name; only that module is packaged')
    parser.add_argument('--version', default='dev', help='Same version/tag for both language bundles')
    parser.add_argument('--output', type=pathlib.Path, default=ROOT / 'dist')
    args = parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9._-]+', args.version):
        parser.error('version must be a safe filename component')
    stem = args.module or 'godot-quick-dev-kit'
    output = args.output / f'{stem}-{args.language}-{args.version}.zip'
    files = build(args.language, output, args.module)
    print(f'{output}: {len(files)} files')


if __name__ == '__main__':
    main()
