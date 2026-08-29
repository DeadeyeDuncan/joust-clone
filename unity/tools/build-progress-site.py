"""Generate the GitHub Pages progress site for the Joust Unity rebuild.

Reads the M0 findings document, the git log, and whatever screenshots exist,
and writes a single self-contained page to site/index.html. Re-run it at every
gate; it is deterministic, so an unchanged project produces an unchanged page.

Usage:
    python unity/tools/build-progress-site.py
"""

import html
import io
import os
import re
import shutil
import subprocess
import sys

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
FINDINGS = os.path.join(REPO_ROOT, "docs", "superpowers", "specs",
                        "2026-08-29-joust-unity-m0-findings.md")
SHOTS_SRC = os.path.join(REPO_ROOT, "unity", "artifacts", "screenshots")
SITE_DIR = os.path.join(REPO_ROOT, "site")
SHOTS_DST = os.path.join(SITE_DIR, "shots")

MILESTONES = [
    ("M0", "Verification spike",
     "Prove every engine assumption by execution before planning M1."),
    ("M1", "Core loop",
     "Flight, one arena, one buzzard tier, joust resolution, death and respawn, HUD."),
    ("M2", "Parity",
     "Three tiers, eggs, pterodactyl, lava troll, wave schedule, two-player, hi-score."),
    ("M3", "Presentation",
     "URP lighting and VFX, camera juice, audio mix, animation polish."),
    ("M4", "New content",
     "Arena variants, three new enemies, power-ups, the Skylord boss."),
    ("M5", "Ship",
     "IL2CPP Windows build and a user smoke test."),
]


def git(*args):
    try:
        out = subprocess.run(["git"] + list(args), cwd=REPO_ROOT,
                             capture_output=True, text=True, check=True)
        return out.stdout.strip()
    except Exception:
        return ""


def read_findings():
    """Return [(id, title, verdict, body_html)] for each F-section that has content."""
    if not os.path.exists(FINDINGS):
        return []

    text = io.open(FINDINGS, encoding="utf-8").read()
    sections = re.split(r"^## ", text, flags=re.M)[1:]
    results = []

    for section in sections:
        heading, _, body = section.partition("\n")
        heading = heading.strip()
        match = re.match(r"^(F\d+)\s+—\s+(.*)$", heading)
        if not match:
            continue

        fid, title = match.group(1), match.group(2)
        body = body.strip()
        if not body:
            verdict = "pending"
        elif re.search(r"\*\*Verdict:\s*HELD", body):
            verdict = "held"
        elif re.search(r"\*\*Verdict:\s*FAILED", body):
            verdict = "failed"
        else:
            verdict = "recorded"

        results.append((fid, title, verdict, body))

    return results


def summarize(body, limit=420):
    """First meaningful prose paragraph of a findings body, as plain text."""
    if not body:
        return "Not yet run."

    for para in body.split("\n\n"):
        para = para.strip()
        if not para:
            continue
        # Strip inline markup FIRST. A paragraph opening with "**Observed:**"
        # is prose, not a bullet, and must not be filtered out as one.
        para = re.sub(r"`([^`]*)`", r"\1", para)
        para = re.sub(r"\*\*([^*]*)\*\*", r"\1", para)
        para = re.sub(r"\*([^*]+)\*", r"\1", para)
        # Skip genuine non-prose blocks: fences, tables, headings, list items.
        if para.startswith(("```", "|", "#")) or re.match(r"^[-*+]\s", para):
            continue
        para = para.replace("\n", " ").strip()
        # Skip bare labels such as "Command" or "Observed:" that head a code
        # block rather than saying anything on their own.
        if len(para) < 40:
            continue
        if len(para) > limit:
            para = para[:limit].rsplit(" ", 1)[0] + "…"
        return para

    return "See the findings document."


def copy_screenshots():
    if not os.path.isdir(SHOTS_SRC):
        return []

    os.makedirs(SHOTS_DST, exist_ok=True)
    names = []
    for name in sorted(os.listdir(SHOTS_SRC)):
        if name.lower().endswith((".png", ".jpg")):
            shutil.copy2(os.path.join(SHOTS_SRC, name), os.path.join(SHOTS_DST, name))
            names.append(name)
    return names


def esc(text):
    return html.escape(text, quote=True)


def build():
    findings = read_findings()
    shots = copy_screenshots()
    commits = [line for line in git("log", "--oneline", "-15").splitlines() if line]
    branch = git("rev-parse", "--abbrev-ref", "HEAD") or "master"

    held = sum(1 for f in findings if f[2] == "held")
    total = len(findings)

    rows = []
    for fid, title, verdict, body in findings:
        rows.append(f"""
        <article class="finding {esc(verdict)}">
          <header><span class="fid">{esc(fid)}</span><h3>{esc(title)}</h3>
          <span class="badge {esc(verdict)}">{esc(verdict)}</span></header>
          <p>{esc(summarize(body))}</p>
        </article>""")

    milestone_rows = []
    for key, name, detail in MILESTONES:
        state = "active" if key == "M0" else "queued"
        label = "in progress" if key == "M0" else "queued"
        milestone_rows.append(f"""
        <tr class="{state}">
          <td class="key">{esc(key)}</td>
          <td><strong>{esc(name)}</strong><br><span class="muted">{esc(detail)}</span></td>
          <td class="state">{esc(label)}</td>
        </tr>""")

    if shots:
        shot_html = "\n".join(
            f'<figure><img src="shots/{esc(n)}" alt="{esc(n)}" loading="lazy">'
            f"<figcaption>{esc(n)}</figcaption></figure>"
            for n in shots
        )
    else:
        shot_html = '<p class="muted">No screenshots captured yet.</p>'

    commit_html = "\n".join(f"<li><code>{esc(c)}</code></li>" for c in commits)

    page = f"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Joust — Unity Rebuild Progress</title>
<style>
  :root {{
    --bg: #f6f6f4; --panel: #fff; --ink: #17171a; --muted: #6a6a72;
    --line: #e2e2de; --accent: #b4451f; --ok: #2f6f43; --pending: #8a6d1f;
  }}
  @media (prefers-color-scheme: dark) {{
    :root {{
      --bg: #131316; --panel: #1c1c20; --ink: #ececef; --muted: #9a9aa3;
      --line: #2c2c33; --accent: #ff8a5c; --ok: #6bc48b; --pending: #d9b352;
    }}
  }}
  * {{ box-sizing: border-box; }}
  body {{ margin: 0; background: var(--bg); color: var(--ink);
    font: 16px/1.6 ui-sans-serif, system-ui, -apple-system, "Segoe UI", sans-serif; }}
  .wrap {{ max-width: 1000px; margin: 0 auto; padding: 48px 24px 80px; }}
  h1 {{ font-size: 2.4rem; margin: 0 0 .2em; letter-spacing: -0.02em; }}
  h2 {{ font-size: 1.3rem; margin: 2.5em 0 .8em; letter-spacing: -0.01em; }}
  .lede {{ color: var(--muted); max-width: 62ch; margin: 0 0 1.6em; }}
  .stats {{ display: flex; gap: 12px; flex-wrap: wrap; margin-bottom: 8px; }}
  .stat {{ background: var(--panel); border: 1px solid var(--line);
    border-radius: 10px; padding: 12px 16px; }}
  .stat b {{ display: block; font-size: 1.5rem; }}
  .stat span {{ color: var(--muted); font-size: .85rem; }}
  table {{ width: 100%; border-collapse: collapse; background: var(--panel);
    border: 1px solid var(--line); border-radius: 10px; overflow: hidden; }}
  td {{ padding: 12px 14px; border-top: 1px solid var(--line); vertical-align: top; }}
  tr:first-child td {{ border-top: 0; }}
  .key {{ font-weight: 700; width: 56px; }}
  .state {{ text-align: right; color: var(--muted); white-space: nowrap; }}
  tr.active {{ background: color-mix(in srgb, var(--accent) 8%, transparent); }}
  .muted {{ color: var(--muted); }}
  .finding {{ background: var(--panel); border: 1px solid var(--line);
    border-radius: 10px; padding: 14px 16px; margin-bottom: 10px; }}
  .finding header {{ display: flex; align-items: center; gap: 10px; margin-bottom: 6px; }}
  .finding h3 {{ font-size: 1rem; margin: 0; flex: 1; }}
  .fid {{ font-weight: 700; color: var(--accent); }}
  .finding p {{ margin: 0; color: var(--muted); font-size: .93rem; }}
  .badge {{ font-size: .72rem; text-transform: uppercase; letter-spacing: .06em;
    padding: 3px 8px; border-radius: 999px; border: 1px solid var(--line); }}
  .badge.held {{ color: var(--ok); border-color: color-mix(in srgb, var(--ok) 40%, transparent); }}
  .badge.pending {{ color: var(--pending); }}
  figure {{ margin: 0 0 16px; }}
  figure img {{ width: 100%; border-radius: 10px; border: 1px solid var(--line); display: block; }}
  figcaption {{ color: var(--muted); font-size: .85rem; margin-top: 6px; }}
  ul.commits {{ list-style: none; padding: 0; margin: 0; }}
  ul.commits li {{ padding: 6px 0; border-top: 1px solid var(--line); }}
  ul.commits li:first-child {{ border-top: 0; }}
  code {{ font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: .85rem; }}
  footer {{ margin-top: 48px; color: var(--muted); font-size: .85rem; }}
  a {{ color: var(--accent); }}
</style>
</head>
<body>
<div class="wrap">
  <h1>Joust — Unity Rebuild</h1>
  <p class="lede">Rebuilding a pygame Joust clone as a native Unity 6 game with
  full 3D graphics on a 2.5D gameplay plane. This page tracks the build as it
  happens: milestones, spike findings, screenshots, and commits.</p>

  <div class="stats">
    <div class="stat"><b>{held}/{total}</b><span>M0 findings held</span></div>
    <div class="stat"><b>{len(commits)}</b><span>recent commits</span></div>
    <div class="stat"><b>{esc(branch)}</b><span>branch</span></div>
  </div>

  <h2>Milestones</h2>
  <table>{''.join(milestone_rows)}
  </table>

  <h2>M0 spike findings</h2>
  {''.join(rows) if rows else '<p class="muted">No findings recorded yet.</p>'}

  <h2>Screenshots</h2>
  {shot_html}

  <h2>Recent commits</h2>
  <ul class="commits">{commit_html}</ul>

  <footer>
    Generated from the repository by <code>unity/tools/build-progress-site.py</code>.
    Source: <a href="https://github.com/DeadeyeDuncan/joust-clone">DeadeyeDuncan/joust-clone</a>.
  </footer>
</div>
</body>
</html>
"""

    os.makedirs(SITE_DIR, exist_ok=True)
    io.open(os.path.join(SITE_DIR, "index.html"), "w", encoding="utf-8", newline="\n").write(page)
    io.open(os.path.join(SITE_DIR, ".nojekyll"), "w", encoding="utf-8").write("")

    print(f"site written: {os.path.join(SITE_DIR, 'index.html')}")
    print(f"findings={total} held={held} screenshots={len(shots)} commits={len(commits)}")
    return 0


if __name__ == "__main__":
    sys.exit(build())
