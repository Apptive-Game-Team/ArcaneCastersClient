import pathlib
import sys
import time
import urllib.error
import urllib.request

base, directory = sys.argv[1:]
public = pathlib.Path(directory)


def request(path):
    try:
        return urllib.request.urlopen(
            urllib.request.Request(base + path, method="HEAD"), timeout=10)
    except urllib.error.HTTPError as error:
        return error


for attempt in range(30):
    try:
        if request("/").status == 200:
            break
    except OSError:
        pass
    time.sleep(1)
else:
    raise RuntimeError("Nginx가 시작되지 않았습니다.")

for path in ("/", "/index.html", "/ServiceWorker.js"):
    response = request(path)
    assert response.status == 200, path
    assert response.headers.get("Cache-Control") == "no-cache", path

wasm_found = False
for file in public.joinpath("Build").iterdir():
    suffix = file.name.removesuffix(".gz").removesuffix(".br")
    content_type = next((mime for ext, mime in (
        (".wasm", "application/wasm"),
        (".js", "application/javascript"),
        (".data", "application/octet-stream"),
    ) if suffix.endswith(ext)), None)
    if content_type is None:
        continue
    wasm_found |= suffix.endswith(".wasm")
    response = request("/Build/" + file.name)
    assert response.status == 200, file.name
    assert response.headers.get_content_type() == content_type, file.name
    assert int(response.headers["Content-Length"]) == file.stat().st_size, file.name
    encoding = "gzip" if file.name.endswith(".gz") else "br" if file.name.endswith(".br") else None
    assert response.headers.get("Content-Encoding") == encoding, file.name
assert wasm_found, "WASM 빌드 파일이 없습니다."
for path in ("/Build/missing.wasm", "/Build/missing.data", "/.git/config", "/.env"):
    assert request(path).status == 404, path
print("WebGL Nginx HTTP 검사 통과")
