import html
import io
import json
import os
import threading
import urllib.error
import urllib.request
import zipfile
import xml.etree.ElementTree as ET
from functools import lru_cache
from pathlib import Path
from urllib.parse import quote
from mimetypes import guess_type

from flask import Flask, abort, jsonify, render_template

app = Flask(__name__)

BASE_DIR = Path(__file__).resolve().parent
# Optional local archives can be used for development. Production reads GitHub ZIPs
# directly using HTTP Range requests and never downloads or extracts the full archive.
ARCHIVE_DIR = Path(os.environ.get("CWK_WIKI_ARCHIVE_DIR", BASE_DIR / "data" / "archives"))
RANGE_TIMEOUT = int(os.environ.get("CWK_WIKI_RANGE_TIMEOUT", "30"))

EDITIONS = {
    "obsidian": {
        "archive": ARCHIVE_DIR / "cwk_obsidian_wiki_data.zip",
        "url": "https://github.com/SirEagle34/CWKAssetsV1/releases/download/cwk-wiki/cwk_obsidian_wiki_data.zip",
        "label": "Obsidian",
    },
    "darkmagic": {
        "archive": ARCHIVE_DIR / "cwk_darkmagic_wiki_data.zip",
        "url": "https://github.com/SirEagle34/CWKAssetsV1/releases/download/cwk-wiki/cwk_darkmagic_wiki_data.zip",
        "label": "Dark Magic",
    },
}

PAGES = {
    "index": "index.html",
    "cwk_obsidian": "cwk_obsidian.html",
    "cwk_darkmagic": "cwk_darkmagic.html",
    "cwk_obsidian_creatures": "cwk_obsidian_creatures.html",
    "cwk_obsidian_actionscards": "cwk_obsidian_actionscards.html",
    "cwk_darkmagic_creatures": "cwk_darkmagic_creatures.html",
    "cwk_darkmagic_actionscards": "cwk_darkmagic_actionscards.html",
}
ZIP_LOCK = threading.RLock()


class HTTPRangeReader:
    """Small seekable file-like adapter backed by HTTP Range requests."""

    def __init__(self, url):
        self.url = url
        self.position = 0
        self.size = self._discover_size()

    def _request_range(self, start, end):
        if start > end:
            return b""
        request = urllib.request.Request(
            self.url,
            headers={
                "Range": f"bytes={start}-{end}",
                "User-Agent": "CWK-Wiki/1.0",
                "Accept-Encoding": "identity",
            },
        )
        try:
            with urllib.request.urlopen(request, timeout=RANGE_TIMEOUT) as response:
                if response.status != 206:
                    raise OSError(
                        f"GitHub did not honor HTTP Range (HTTP {response.status}). "
                        "The wiki requires partial-content support."
                    )
                content_range = response.headers.get("Content-Range", "")
                if not content_range.startswith(f"bytes {start}-"):
                    raise OSError(f"Unexpected Content-Range header: {content_range!r}")
                data = response.read()
        except (urllib.error.URLError, TimeoutError) as exc:
            raise OSError(f"Could not read GitHub release range: {exc}") from exc
        expected_length = end - start + 1
        if len(data) != expected_length:
            raise OSError(
                f"Short range read at {start}-{end}: got {len(data)} of {expected_length} bytes"
            )
        return data

    def _discover_size(self):
        request = urllib.request.Request(
            self.url,
            headers={
                "Range": "bytes=0-0",
                "User-Agent": "CWK-Wiki/1.0",
                "Accept-Encoding": "identity",
            },
        )
        try:
            with urllib.request.urlopen(request, timeout=RANGE_TIMEOUT) as response:
                content_range = response.headers.get("Content-Range", "")
                if response.status != 206 or "/" not in content_range:
                    raise OSError(
                        f"GitHub release does not support byte ranges: "
                        f"HTTP {response.status}, Content-Range={content_range!r}"
                    )
                size = int(content_range.rsplit("/", 1)[1])
                if size <= 0:
                    raise OSError("GitHub returned an invalid ZIP size.")
                return size
        except (urllib.error.URLError, TimeoutError, ValueError) as exc:
            raise OSError(f"Could not determine GitHub ZIP size: {exc}") from exc

    def seek(self, offset, whence=io.SEEK_SET):
        if whence == io.SEEK_SET:
            position = offset
        elif whence == io.SEEK_CUR:
            position = self.position + offset
        elif whence == io.SEEK_END:
            position = self.size + offset
        else:
            raise ValueError("Invalid seek mode")
        if position < 0:
            raise ValueError("Negative seek position")
        self.position = position
        return self.position

    def tell(self):
        return self.position

    def read(self, size=-1):
        if size is None or size < 0:
            size = max(0, self.size - self.position)
        size = min(size, max(0, self.size - self.position))
        if size == 0:
            return b""
        start = self.position
        end = start + size - 1
        data = self._request_range(start, end)
        self.position += len(data)
        return data

    def readable(self):
        return True

    def seekable(self):
        return True

    def close(self):
        pass


def _normalized(name):
    return str(name).replace("\\", "/").strip("/").lower()


def _open_archive(edition):
    config = EDITIONS.get(edition)
    if not config:
        abort(404)
    local_path = config["archive"]
    if local_path.is_file():
        return zipfile.ZipFile(local_path, "r")
    try:
        return zipfile.ZipFile(HTTPRangeReader(config["url"]), "r")
    except (OSError, zipfile.BadZipFile, ValueError) as exc:
        app.logger.exception("Unable to open %s wiki ZIP via HTTP ranges", edition)
        abort(503, description=(
            f"{config['label']} archive could not be read from GitHub using HTTP Range "
            f"requests: {exc}"
        ))


@lru_cache(maxsize=8)
def _zip_index(edition):
    with ZIP_LOCK, _open_archive(edition) as archive:
        return tuple(name for name in archive.namelist() if not name.endswith("/"))


def _members(edition):
    return _zip_index(edition)


def _find_member(edition, filename):
    wanted = _normalized(filename)
    members = _members(edition)
    for member in members:
        if _normalized(member) == wanted:
            return member
    for member in members:
        if _normalized(member).endswith("/" + wanted):
            return member

    wanted_base = wanted.rsplit("/", 1)[-1]
    wanted_stem = wanted_base.rsplit(".", 1)[0]
    matches = []
    for member in members:
        member_base = _normalized(member).rsplit("/", 1)[-1]
        member_stem = member_base.rsplit(".", 1)[0]
        if member_base == wanted_base or member_stem == wanted_stem:
            matches.append(member)
    if matches:
        preferred = (".png", ".jpg", ".jpeg", ".webp", ".tga", ".json", ".txt", ".xml")
        matches.sort(key=lambda m: (
            preferred.index(Path(m).suffix.lower())
            if Path(m).suffix.lower() in preferred else len(preferred),
            len(m),
        ))
        return matches[0]
    return None


def _read_member(edition, member):
    with ZIP_LOCK, _open_archive(edition) as archive:
        return archive.read(member)


def _load_json(edition, filename):
    member = _find_member(edition, filename)
    if not member:
        abort(503, description=f"{filename} was not found inside the {edition} ZIP archive.")
    try:
        value = json.loads(_read_member(edition, member).decode("utf-8-sig"))
    except (UnicodeDecodeError, json.JSONDecodeError):
        abort(503, description=f"{filename} exists but is not valid UTF-8 JSON.")
    if isinstance(value, list):
        return value
    if isinstance(value, dict):
        for key in ("items", "data", "cards", "creatures", "results"):
            if isinstance(value.get(key), list):
                return value[key]
        return [value]
    return []


@lru_cache(maxsize=2)
def _localization(edition):
    """Load <entry name="KEY">Value</entry> records from En_Sheet1.txt."""
    member = _find_member(edition, "En_Sheet1.txt")
    if not member:
        app.logger.warning("En_Sheet1.txt was not found in the %s archive", edition)
        return {}
    raw = _read_member(edition, member)
    try:
        root = ET.fromstring(raw.decode("utf-8-sig"))
    except (UnicodeDecodeError, ET.ParseError):
        app.logger.exception("Could not parse En_Sheet1.txt for %s", edition)
        return {}

    translations = {}
    for entry in root.iter():
        key = entry.attrib.get("name") or entry.attrib.get("Name") or entry.attrib.get("key")
        if not key:
            continue
        value = "".join(entry.itertext()).strip()
        if value:
            translations[key.strip()] = html.unescape(value)
    return translations


def _resolve_localized_value(value, translations):
    if not isinstance(value, str):
        return value
    key = value.strip()
    if key.startswith("!!"):
        return translations.get(key, value)
    return value


def _localize_items(edition, items):
    translations = _localization(edition)
    if not translations:
        return items

    def localize(value):
        if isinstance(value, dict):
            return {key: localize(item) for key, item in value.items()}
        if isinstance(value, list):
            return [localize(item) for item in value]
        return _resolve_localized_value(value, translations)

    return localize(items)


def _asset_url(edition, value):
    if not isinstance(value, str) or not value.strip():
        return None
    return f"/wiki-asset/{edition}/{quote(value.strip().replace(chr(92), '/'), safe='/')}"


@app.get("/")
def home():
    return render_template(PAGES["index"])


@app.get("/<page>.html")
def html_page(page):
    template = PAGES.get(page)
    if not template:
        abort(404)
    return render_template(template)


@app.get("/api/catalogue/<edition>/<kind>")
def catalogue_api(edition, kind):
    if edition not in EDITIONS or kind not in ("creatures", "actioncards"):
        abort(404)
    filename = "db_Creatures.json" if kind == "creatures" else "db_ActionCards.json"
    items = _load_json(edition, filename)
    items = _localize_items(edition, items)
    return jsonify({"edition": edition, "kind": kind, "count": len(items), "items": items})


@app.get("/wiki-asset/<edition>/<path:filename>")
def wiki_asset(edition, filename):
    if edition not in EDITIONS:
        abort(404)
    member = _find_member(edition, filename)
    if not member:
        abort(404)
    data = _read_member(edition, member)
    response = app.response_class(
        data,
        mimetype=guess_type(member)[0] or "application/octet-stream",
    )
    response.headers["Cache-Control"] = "public, max-age=86400"
    return response


@app.get("/health")
def health():
    return {
        "status": "ok",
        "app": "CWK Wiki",
        "archives": {
            edition: {
                "local": config["archive"].is_file(),
                "remote": bool(config.get("url")),
                "range_enabled": True,
            }
            for edition, config in EDITIONS.items()
        },
    }


if __name__ == "__main__":
    app.run(debug=True)
