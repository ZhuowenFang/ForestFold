"""Package the Unity macOS build from Windows with Unix executable permissions."""
from pathlib import Path
import plistlib
import stat
import struct
import time
import zipfile
import argparse

project = Path(__file__).resolve().parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--source', type=Path, default=project / 'Builds/macOS/ForestFold.app')
parser.add_argument('--output', type=Path, default=project / 'Builds/macOS')
args = parser.parse_args()
source = args.source.resolve()
destination = args.output.resolve()
destination.mkdir(parents=True, exist_ok=True)
plist = plistlib.loads((source / 'Contents/Info.plist').read_bytes())
executable = source / 'Contents/MacOS' / plist['CFBundleExecutable']
with executable.open('rb') as stream:
    header = stream.read(4096)
assert header[:4] == bytes.fromhex('cafebabe'), 'Expected Universal Mach-O binary'
count = struct.unpack_from('>I', header, 4)[0]
architectures = [struct.unpack_from('>I', header, 8+i*20)[0] for i in range(count)]
assert 0x01000007 in architectures and 0x0100000c in architectures, architectures
notes = '''折叠森林 Forest Fold — macOS Demo

适用架构：Intel x86_64 和 Apple Silicon arm64（Universal）。
内容：固定形状整块扩容、初始两件1星芽芽及当前所有游戏机制。

在 Mac 上解压此 ZIP，将 ForestFold.app 拖到应用程序或其他本地目录，再打开。
请直接传输 ZIP，在 Mac 上解压，避免经过 Windows 解压再打包导致执行权限丢失。

此为本地测试包，未使用 Apple Developer ID 签名或进行 Apple 公证。
构建和包内双架构检查已通过；尚未在真实 Mac 上运行验证。
Unity 官方签名与公证说明：https://docs.unity3d.com/2022.3/Documentation/Manual/macos-building-notarization.html
'''
archive = destination / 'ForestFold-macOS-Universal.zip'
native_magics = {bytes.fromhex(x) for x in ('cafebabe', 'bebafeca', 'feedfacf', 'cffaedfe', 'feedface', 'cefaedfe')}
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as output:
    for path in [source, *sorted(source.rglob('*'))]:
        name = path.relative_to(source.parent).as_posix()
        directory = path.is_dir()
        entry = zipfile.ZipInfo(name + ('/' if directory else ''), time.localtime(path.stat().st_mtime)[:6])
        entry.create_system = 3
        if directory:
            entry.external_attr = (stat.S_IFDIR | 0o755) << 16 | 0x10
            output.writestr(entry, b'')
        else:
            with path.open('rb') as stream:
                native = stream.read(4) in native_magics
            entry.external_attr = (stat.S_IFREG | (0o755 if native or path == executable else 0o644)) << 16
            entry.compress_type = zipfile.ZIP_DEFLATED
            with path.open('rb') as incoming, output.open(entry, 'w') as outgoing:
                import shutil
                shutil.copyfileobj(incoming, outgoing)
    output.writestr('README-macOS.txt', notes)
with zipfile.ZipFile(archive) as check:
    assert check.testzip() is None, 'Archive CRC failed'
    executable_entry = check.getinfo(executable.relative_to(source.parent).as_posix())
    assert (executable_entry.external_attr >> 16) & 0o111 == 0o111
(destination / 'README-macOS.txt').write_text(notes, encoding='utf-8')
print(f'MAC_PACKAGE_OK: Intel + Apple Silicon, executable permissions, CRC; {archive.stat().st_size:,} bytes')
print(archive)
