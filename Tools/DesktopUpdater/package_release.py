"""Prepare a desktop build, then create a whole-build ZIP and public GitHub Release manifest.

No uploads. Python 3.10+ standard library only. Run prepare BEFORE macOS signing,
then pack AFTER signing/notarization. Never modify a signed bundle after pack.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import stat
from urllib.parse import quote
import zipfile

PLATFORMS = {
    "win-x64": ("ArcaneCasters.exe", "ArcaneCasters_Data/StreamingAssets"),
    "linux-x64": ("ArcaneCasters", "ArcaneCasters_Data/StreamingAssets"),
    "osx-x64": ("Contents/MacOS/ArcaneCasters", "Contents/Resources/Data/StreamingAssets"),
    "osx-arm64": ("Contents/MacOS/ArcaneCasters", "Contents/Resources/Data/StreamingAssets"),
}


def marker(root):
    marker_path = "Contents/Resources/arcane-install.json" if root.name.endswith(".app") else "arcane-install.json"
    data = json.loads((root / marker_path).read_text(encoding="utf-8-sig"))
    if data.get("schemaVersion") != 1 or data.get("appId") != "arcane-casters":
        raise ValueError("Not a dedicated ArcaneCasters installation")
    if data["platform"] not in PLATFORMS:
        raise ValueError("Unsupported platform")
    folder = "ArcaneCasters.app" if data["platform"].startswith("osx-") else "ArcaneCasters"
    if root.name.lower() != folder.lower():
        raise ValueError("Use a dedicated installation directory named " + folder)
    if not re.fullmatch(r"(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)", data["version"]):
        raise ValueError("Use a stable X.Y.Z bundleVersion")
    if data["executable"] != PLATFORMS[data["platform"]][0] or not (root / data["executable"]).is_file():
        raise ValueError("Missing game executable")
    return data


def prepare(root, helper, repository):
    if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", repository):
        raise ValueError("Repository must be public-owner/public-repository, not a URL or token")
    data = marker(root)
    streaming = root / PLATFORMS[data["platform"]][1]
    helper_name = "ArcaneUpdater.exe" if data["platform"] == "win-x64" else "ArcaneUpdater"
    if not (helper / helper_name).is_file():
        raise ValueError("Publish the self-contained updater for this platform first")
    if (helper / "updater-platform.txt").read_text(encoding="utf-8-sig").strip() != data["platform"]:
        raise ValueError("Updater runtime must match the game platform")
    destination = streaming / "DesktopUpdater"
    if destination.exists():
        raise ValueError("Updater already prepared; use a fresh build")
    shutil.copytree(helper, destination)
    url = f"https://github.com/{repository}/releases/latest/download/desktop-update-{data['platform']}.json"
    (streaming / "desktop-update.json").write_text(
        json.dumps({"manifestUrl": url, "platform": data["platform"]}, indent=2) + "\n", encoding="utf-8")
    if data["platform"] != "win-x64":
        # Cross-compiling .NET on Windows may not preserve executable bits.
        (destination / helper_name).chmod(0o755)
        (root / data["executable"]).chmod(0o755)


def pack(root, output, repository, tag):
    data = marker(root)
    if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", repository):
        raise ValueError("Invalid public repository")
    streaming = root / PLATFORMS[data["platform"]][1]
    config = json.loads((streaming / "desktop-update.json").read_text(encoding="utf-8"))
    expected_url = f"https://github.com/{repository}/releases/latest/download/desktop-update-{data['platform']}.json"
    if config != {"manifestUrl": expected_url, "platform": data["platform"]}:
        raise ValueError("Prepare the build for the same repository/platform")
    helper_name = "ArcaneUpdater.exe" if data["platform"] == "win-x64" else "ArcaneUpdater"
    if not (streaming / "DesktopUpdater" / helper_name).is_file():
        raise ValueError("Updater missing")
    output = output.resolve()
    if output == root or root in output.parents:
        raise ValueError("Output must be outside the installation")
    output.mkdir(parents=True, exist_ok=True)
    stem = f"ArcaneCasters-{data['version']}-{data['platform']}"
    filename = stem + "-update.zip"
    installer = output / (stem + ".zip")
    archive = output / filename
    manifest_path = output / f"desktop-update-{data['platform']}.json"
    if archive.exists() or installer.exists() or manifest_path.exists():
        raise ValueError("Refusing to overwrite release artifacts; use a fresh output directory")
    seen = set()
    expanded = 0
    entries = []
    for parent, directories, files in os.walk(root, followlinks=False):
        for name in sorted(directories + files):
            path = Path(parent) / name
            relative = path.relative_to(root).as_posix()
            if relative.casefold() in seen:
                raise ValueError("Case-colliding archive paths")
            seen.add(relative.casefold())
            if len(seen) > 200000:
                raise ValueError("Too many files")
            for part in relative.split("/"):
                if (part.endswith((" ", ".")) or "\\" in part or ":" in part
                    or any(ord(c) < 32 for c in part)
                    or re.match(r"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)", part, re.I)):
                    raise ValueError("Unsupported archive path")
            if path.is_symlink():
                resolved = path.resolve(strict=True)
                if root != resolved and root not in resolved.parents:
                    raise ValueError("Symbolic link escapes installation")
                target = os.readlink(path)
                if os.path.isabs(target) or data["platform"] == "win-x64":
                    raise ValueError("Only relative Unix symbolic links are supported")
            expanded += path.lstat().st_size
            entries.append(path)
    if expanded > 32 * 1024**3:
        raise ValueError("Build exceeds updater size limit")
    for destination, prefix in ((archive, ""), (installer, root.name + "/")):
        with zipfile.ZipFile(destination, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as zip_file:
            for path in entries:
                relative = path.relative_to(root).as_posix()
                archived_path = prefix + relative
                if path.is_symlink():
                    info = zipfile.ZipInfo(archived_path)
                    info.create_system = 3
                    info.external_attr = (stat.S_IFLNK | 0o777) << 16
                    zip_file.writestr(info, os.readlink(path).encode("utf-8"))
                else:
                    info = zipfile.ZipInfo.from_file(path, archived_path)
                    # On Windows ZipInfo has DOS attributes; emit portable modes for Linux builds.
                    mode = stat.S_IFDIR | 0o755 if path.is_dir() else stat.S_IFREG | 0o644
                    if data["platform"] != "win-x64":
                        mode = path.stat().st_mode if os.name != "nt" else mode
                        if relative == data["executable"] or relative.endswith("/DesktopUpdater/ArcaneUpdater"):
                            mode |= 0o111
                    info.create_system = 3
                    info.external_attr = mode << 16
                    info.compress_type = zipfile.ZIP_DEFLATED
                    if path.is_dir():
                        zip_file.writestr(info, b"")
                    else:
                        with path.open("rb") as source, zip_file.open(info, "w", force_zip64=True) as dest:
                            shutil.copyfileobj(source, dest)
    digest = hashlib.sha256()
    with archive.open("rb") as file:
        for block in iter(lambda: file.read(1024 * 1024), b""):
            digest.update(block)
    manifest = {"schemaVersion": 1, "version": data["version"], "platform": data["platform"],
                "url": f"https://github.com/{repository}/releases/download/{quote(tag, safe='')}/{filename}",
                "sha256": digest.hexdigest(), "size": archive.stat().st_size}
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(installer)
    print(archive)
    print(manifest_path)
    return archive, manifest_path


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["prepare", "pack"])
    parser.add_argument("--install-root", type=Path, required=True)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--helper", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--tag")
    args = parser.parse_args()
    root = args.install_root.resolve(strict=True)
    if args.action == "prepare":
        if args.helper is None:
            parser.error("prepare requires --helper")
        prepare(root, args.helper.resolve(strict=True), args.repository)
    else:
        if args.output is None or not args.tag:
            parser.error("pack requires --output and --tag")
        pack(root, args.output, args.repository, args.tag)
