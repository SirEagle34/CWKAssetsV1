import json
import os
import threading
import zipfile
from functools import lru_cache
from pathlib import Path
from urllib.parse import quote

from flask import Flask, abort, jsonify, render_template, send_file, send_from_directory

app = Flask(__name__)

BASE_DIR = Path(__file__).resolve().parent
ARCHIVE_DIR = Path(os.environ.get("CWK_WIKI_ARCHIVE_DIR", BASE_DIR / "data" / "archives"))

EDITIONS = {
    "obsidian": {
        "archive": ARCHIVE_DIR / "cwk_obsidian_wiki_data.zip",
        "label": "Obsidian",
    },
    "darkmagic": {
        "archive": ARCHIVE_DIR / "cwk_darkmagic_wiki_data.zip",
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


def _archive(edition):
    config = EDITIONS.get(edition)
    if not config:
        abort(404)
    path = config["archive"]
    if not path.is_file():
        abort(503, description=(
            f"{config['label']} data archive is missing. Place "
            f"{path.name} in {ARCHIVE_DIR}; do not extract the ZIP."
        ))
    return path


def _normalized(name):
    return str(name).replace("\\", "/").strip("/").lower()


@lru_cache(maxsize=8)
def _zip_index(edition, mtime_ns, size):
    path = _archive(edition)
    with ZIP_LOCK, zipfile.ZipFile(path, "r") as archive:
        return tuple(name for name in archive.namelist() if not name.endswith("/"))


def _members(edition):
    path = _archive(edition)
    stat = path.stat()
    return _zip_index(edition, stat.st_mtime_ns, stat.st_size)


def _find_member(edition, filename):
    wanted = _normalized(filename)
    members = _members(edition)
    # Prefer an exact archive path.
    for member in members:
        if _normalized(member) == wanted:
            return member
    # Archive roots may contain an edition folder; match a full path suffix next.
    for member in members:
        if _normalized(member).endswith("/" + wanted):
            return member
    # JSON and localization files can safely be located by basename.
    if "/" not in wanted:
        matches = [m for m in members if _normalized(m).rsplit("/", 1)[-1] == wanted]
        if matches:
            return matches[0]
    return None


def _read_member(edition, member):
    path = _archive(edition)
    with ZIP_LOCK, zipfile.ZipFile(path, "r") as archive:
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
    return jsonify({"edition": edition, "kind": kind, "count": len(items), "items": items})


@app.get("/wiki-asset/<edition>/<path:filename>")
def wiki_asset(edition, filename):
    if edition not in EDITIONS:
        abort(404)
    # ZIP member paths are looked up inside the archive. Nothing is extracted to disk.
    member = _find_member(edition, filename)
    if not member:
        abort(404)
    import io
    from mimetypes import guess_type
    data = _read_member(edition, member)
    response = app.response_class(data, mimetype=guess_type(member)[0] or "application/octet-stream")
    response.headers["Cache-Control"] = "public, max-age=86400"
    return response


@app.get("/health")
def health():
    available = {
        edition: config["archive"].is_file()
        for edition, config in EDITIONS.items()
    }
    return {"status": "ok", "app": "CWK Wiki", "archives": available}


if __name__ == "__main__":
    app.run(debug=True)
