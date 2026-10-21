import hashlib
import json
import os
from pathlib import Path
import tempfile
import unittest
import zipfile

import package_release as release


class PackagingTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="arcane-package-test-", dir=os.environ.get("ARCANE_TEST_ROOT"))
        self.base = Path(self.temp.name)
        self.root = self.base / "ArcaneCasters"
        self.root.mkdir()
        (self.root / "ArcaneCasters.exe").write_bytes(b"fixture-game")
        (self.root / "arcane-install.json").write_text(json.dumps({
            "schemaVersion": 1, "appId": "arcane-casters", "version": "1.2.3",
            "platform": "win-x64", "executable": "ArcaneCasters.exe",
        }))
        self.helper = self.base / "helper"
        self.helper.mkdir()
        (self.helper / "ArcaneUpdater.exe").write_bytes(b"fixture-helper")
        (self.helper / "updater-platform.txt").write_text("win-x64")

    def tearDown(self):
        self.temp.cleanup()

    def test_manifest_archive_and_config(self):
        release.prepare(self.root, self.helper, "example/public-game")
        archive, manifest = release.pack(self.root, self.base / "out", "example/public-game", "v1.2.3")
        data = json.loads(manifest.read_text())
        self.assertEqual(data["sha256"], hashlib.sha256(archive.read_bytes()).hexdigest())
        self.assertEqual(data["size"], archive.stat().st_size)
        self.assertEqual(data["version"], "1.2.3")
        with zipfile.ZipFile(archive) as zip_file:
            self.assertIn("ArcaneCasters.exe", zip_file.namelist())  # no wrapper directory
            config = json.loads(zip_file.read("ArcaneCasters_Data/StreamingAssets/desktop-update.json"))
            self.assertEqual(config["manifestUrl"],
                             "https://github.com/example/public-game/releases/latest/download/desktop-update-win-x64.json")

    def test_install_archive_matches_update_payload(self):
        release.prepare(self.root, self.helper, "example/public-game")
        archive, manifest = release.pack(self.root, self.base / "out", "example/public-game", "v1.2.3")
        self.assertTrue(json.loads(manifest.read_text())["url"].endswith("-update.zip"))
        with zipfile.ZipFile(archive) as payload, zipfile.ZipFile(archive.with_name(archive.name.replace("-update.zip", ".zip"))) as install:
            self.assertEqual(set(install.namelist()), {"ArcaneCasters/" + name for name in payload.namelist()})
            for name in payload.namelist():
                self.assertEqual(payload.read(name), install.read("ArcaneCasters/" + name))
                self.assertEqual(payload.getinfo(name).external_attr, install.getinfo("ArcaneCasters/" + name).external_attr)

    def test_no_overwrite_or_output_inside_game(self):
        release.prepare(self.root, self.helper, "example/public-game")
        with self.assertRaises(ValueError):
            release.pack(self.root, self.root / "out", "example/public-game", "v1")
        release.pack(self.root, self.base / "out", "example/public-game", "v1")
        with self.assertRaises(ValueError):
            release.pack(self.root, self.base / "out", "example/public-game", "v1")

    def test_missing_helper_and_wrong_repository(self):
        with self.assertRaises(ValueError):
            release.prepare(self.root, self.base, "example/public-game")
        with self.assertRaises(ValueError):
            release.prepare(self.root, self.helper, "https://token@github.com/example/game")

    def test_mismatched_config(self):
        release.prepare(self.root, self.helper, "example/public-game")
        with self.assertRaises(ValueError):
            release.pack(self.root, self.base / "out", "example/other", "v1")

    def test_version_validation(self):
        marker = self.root / "arcane-install.json"
        data = json.loads(marker.read_text())
        data["version"] = "1.2.3-beta"
        marker.write_text(json.dumps(data))
        with self.assertRaises(ValueError):
            release.marker(self.root)

    def test_macos_bundle_marker_and_names(self):
        root = self.base / "ArcaneCasters.app"
        (root / "Contents/Resources").mkdir(parents=True)
        (root / "Contents/MacOS").mkdir()
        (root / "Contents/MacOS/ArcaneCasters").write_bytes(b"fixture-game")
        (root / "Contents/Resources/arcane-install.json").write_text(json.dumps({
            "schemaVersion": 1, "appId": "arcane-casters", "version": "1.2.3",
            "platform": "osx-arm64", "executable": "Contents/MacOS/ArcaneCasters",
        }))
        (self.helper / "ArcaneUpdater").write_bytes(b"fixture-helper")
        (self.helper / "updater-platform.txt").write_text("osx-arm64")
        release.prepare(root, self.helper, "example/public-game")
        archive, _ = release.pack(root, self.base / "out", "example/public-game", "v1.2.3")
        with zipfile.ZipFile(archive) as payload:
            self.assertNotIn("arcane-install.json", payload.namelist())
            self.assertIn("Contents/Resources/arcane-install.json", payload.namelist())
            self.assertTrue((payload.getinfo("Contents/MacOS/ArcaneCasters").external_attr >> 16) & 0o111)


if __name__ == "__main__":
    unittest.main()
