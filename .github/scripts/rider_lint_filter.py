#!/usr/bin/env python3
"""SARIF の指摘から、git diff の追加・変更行に入るものだけを出力する。未追跡ファイルは全行対象。

exit: 指摘あり=1 / なし=0 / 内部エラー=2
SARIFのuri(slnからの相対パス)は、slnがgitルート直下にある前提でgitルート相対パスと一致する。
"""
import argparse
import json
import re
import subprocess
import sys
from urllib.parse import unquote


def changed_lines(base, root):
    """{path: set(行番号)}。git diff -U0 のhunkの新側の行範囲。"""
    out = subprocess.run(
        ["git", "-c", "core.quotePath=false", "diff", "-U0", "--no-color", "--no-ext-diff",
         "--no-prefix", base, "--", "*.cs"],
        cwd=root, capture_output=True, text=True, encoding="utf-8", check=True).stdout
    result, cur = {}, None
    for line in out.splitlines():
        if line.startswith("+++ "):
            p = line[4:].rstrip("\t")
            cur = None if p == "/dev/null" else p
            if cur is not None:
                result.setdefault(cur, set())
        elif line.startswith("@@") and cur is not None:
            m = re.match(r"@@ -\S+ \+(\d+)(?:,(\d+))? @@", line)
            if m:
                start, count = int(m.group(1)), int(m.group(2) or 1)
                result[cur].update(range(start, start + count))
    return result


def esc_data(s):
    return s.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")


def esc_prop(s):
    return esc_data(s).replace(":", "%3A").replace(",", "%2C")


def run():
    ap = argparse.ArgumentParser()
    ap.add_argument("--sarif", required=True)
    ap.add_argument("--base", required=True)
    ap.add_argument("--root", required=True)
    ap.add_argument("--format", default="text")
    ap.add_argument("--untracked", nargs="*", default=[])
    a = ap.parse_args()

    changed = changed_lines(a.base, a.root)
    untracked = set(a.untracked)
    with open(a.sarif, encoding="utf-8") as f:
        sarif = json.load(f)

    found = set()
    for run_ in sarif.get("runs", []):
        for r in run_.get("results", []):
            for loc in r.get("locations", []):
                pl = loc.get("physicalLocation", {})
                path = unquote(pl.get("artifactLocation", {}).get("uri", "")).replace("\\", "/")
                line = pl.get("region", {}).get("startLine")
                if not path or line is None:
                    continue
                if path in untracked or line in changed.get(path, ()):
                    found.add((path, line, r.get("ruleId", "?"), r.get("message", {}).get("text", "")))

    for path, line, rule, msg in sorted(found):
        msg = msg.replace("\n", " ")
        if a.format == "github":
            print(f"::warning file={esc_prop(path)},line={line}::{esc_data(f'[{rule}] {msg}')}")
        else:
            print(f"{path}:{line}: [{rule}] {msg}")
    return 1 if found else 0


if __name__ == "__main__":
    try:
        sys.exit(run())
    except SystemExit:
        raise
    except Exception as e:  # noqa: BLE001
        print(f"rider_lint_filter: {e!r}", file=sys.stderr)
        sys.exit(2)
