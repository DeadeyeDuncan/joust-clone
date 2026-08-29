"""Search and download CC0 models from Poly Pizza into the Unity project.

The API key is read from, in order:
  1. the POLY_PIZZA_KEY environment variable
  2. unity/tools/.polypizza-key   (gitignored; this repo is public)

The key is never printed, never passed on the command line, and never written
into any file this script creates.

Usage:
    python unity/tools/fetch-model.py search ostrich
    python unity/tools/fetch-model.py search bird --limit 20
    python unity/tools/fetch-model.py get <model-id> --name Ostrich
"""

import argparse
import io
import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
KEY_FILE = os.path.join(REPO_ROOT, "unity", "tools", ".polypizza-key")
ART_DIR = os.path.join(REPO_ROOT, "unity", "JoustUnity", "Assets", "Art", "PolyPizza")
ATTRIBUTION = os.path.join(ART_DIR, "ATTRIBUTION.md")
API = "https://api.poly.pizza/v1.1"
# The asset CDN rejects urllib's default user-agent with a 403.
BROWSER_UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"


def read_key():
    key = os.environ.get("POLY_PIZZA_KEY", "").strip()
    if key:
        return key

    if os.path.exists(KEY_FILE):
        key = io.open(KEY_FILE, encoding="utf-8").read().strip()
        if key:
            return key

    sys.exit(
        "No API key found.\n"
        f"  Put it in {KEY_FILE} (already gitignored), or set POLY_PIZZA_KEY."
    )


def call(path, params=None):
    url = f"{API}/{path}"
    if params:
        url += "?" + urllib.parse.urlencode(params)

    request = urllib.request.Request(url, headers={"x-auth-token": read_key()})
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as error:
        # Deliberately does not echo the key or the full header set.
        sys.exit(f"API error {error.code} on {path}: {error.reason}")


def search(term, limit):
    data = call(f"search/{urllib.parse.quote(term)}", {"Limit": limit})
    results = data.get("results", data if isinstance(data, list) else [])
    if not results:
        print("no results")
        return

    print(f"{len(results)} result(s):\n")
    for item in results:
        title = item.get("Title") or item.get("title") or "?"
        creator = (item.get("Creator") or {}).get("Username", "?")
        licence = item.get("Licence") or item.get("License") or "?"
        tris = item.get("Tris") or item.get("tris") or "?"
        model_id = item.get("ID") or item.get("id") or "?"
        print(f"  {title:34} {licence:10} tris={tris:>8}  by {creator:18} id={model_id}")


def get(model_id, name):
    item = call(f"model/{urllib.parse.quote(model_id)}")
    download = item.get("Download") or item.get("download")
    if not download:
        sys.exit(f"model {model_id} exposes no download url")

    os.makedirs(ART_DIR, exist_ok=True)
    title = item.get("Title") or name
    creator = (item.get("Creator") or {}).get("Username", "unknown")
    licence = item.get("Licence") or item.get("License") or "unknown"

    out = os.path.join(ART_DIR, f"{name}.glb")
    request = urllib.request.Request(download, headers={"User-Agent": BROWSER_UA})
    try:
        with urllib.request.urlopen(request, timeout=120) as response, open(out, "wb") as handle:
            handle.write(response.read())
    except urllib.error.HTTPError as error:
        sys.exit(f"download failed for {model_id}: HTTP {error.code} {error.reason}")
    except urllib.error.URLError as error:
        sys.exit(f"download failed for {model_id}: {error.reason}")

    size = os.path.getsize(out) / 1e6
    print(f"saved {out} ({size:.2f} MB)")
    print(f"  title={title} creator={creator} licence={licence}")

    # Every third-party asset gets an attribution row, whatever its licence.
    header = "| Asset | Title | Creator | Licence | Source |\n|---|---|---|---|---|\n"
    row = f"| `{name}.glb` | {title} | {creator} | {licence} | https://poly.pizza/m/{model_id} |\n"
    if not os.path.exists(ATTRIBUTION):
        io.open(ATTRIBUTION, "w", encoding="utf-8", newline="\n").write(
            "# Poly Pizza attribution\n\n"
            "Assets fetched by `unity/tools/fetch-model.py`. An asset with no row "
            "here must not ship.\n\n" + header + row
        )
    else:
        with io.open(ATTRIBUTION, "a", encoding="utf-8", newline="\n") as handle:
            handle.write(row)
    print(f"  attribution appended to {ATTRIBUTION}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    s = sub.add_parser("search")
    s.add_argument("term")
    s.add_argument("--limit", type=int, default=12)

    g = sub.add_parser("get")
    g.add_argument("model_id")
    g.add_argument("--name", required=True, help="file name to save as, without extension")

    args = parser.parse_args()
    if args.command == "search":
        search(args.term, args.limit)
    else:
        get(args.model_id, args.name)


if __name__ == "__main__":
    main()
