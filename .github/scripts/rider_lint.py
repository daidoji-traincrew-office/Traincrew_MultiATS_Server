#!/usr/bin/env python3
"""ReSharper CLI (jb inspectcode / cleanupcode) で、変更した .cs の変更行だけを検査する。標準ライブラリのみ。

  rider_lint.py check --sln X.sln [--exclude d/]... [--base REF] [--fix] [--format text|github]   (CI・手動実行)
  rider_lint.py hook  --sln X.sln [--exclude d/]...                                               (Claude Code フック。stdinのJSONを読む)

check の終了コード: 指摘あり=1 / なし=0 / ツール失敗=2。
前提: sln は gitルート直下にある(SARIFのuri=slnからの相対パス を gitルート相対パスとして突き合わせる)。
      `dotnet jb` は呼び出し側リポジトリの .config/dotnet-tools.json で解決される。

hook が依存する Claude Code フック仕様 (変わったらここを見直す):
  - stdin JSON: hook_event_name ("PostToolUse" / "Stop" / "SubagentStop"), session_id,
    stop_hook_active (Stop系で、差し戻し後の再実行なら true), tool_input.file_path (PostToolUse。絶対パス),
    agent_id (サブエージェント内のイベントにだけ付く。.touched をエージェント単位に分けるのに使う)
  - settings.json の PostToolUse matcher は "Edit|Write|MultiEdit"
  - exit 2 + stderr = Stop を差し戻して stderr を Claude に見せる。stdout の {"systemMessage": ...} = ユーザーへの通知
  - fail-open: exit 2 を返すのは「指摘あり かつ stop_hook_active=false」のときだけ。想定外は全て exit 0。
"""
import argparse
import contextlib
import functools
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
from contextlib import contextmanager
from pathlib import Path
from urllib.parse import unquote

MAX_AGE = 14 * 24 * 3600
JB_TIMEOUT = 270  # hook でだけ使う制限秒数 (settings.json のフック timeout 300 より短くする。cleanupcode+inspectcode の合計)。check (CI) には適用しない


class LintError(Exception):
    pass


class LintRetry(LintError):
    """時間切れ・restore 失敗など、時間をおけば通る可能性がある失敗。記録を残して次回の Stop で再検査する。"""


def git(*args, root=None):
    return subprocess.run(["git", "-c", "core.quotePath=false", *args], cwd=root, capture_output=True,
                          text=True, encoding="utf-8", check=True).stdout


def git_root(start="."):
    return git("rev-parse", "--show-toplevel", root=start).strip()


def split_z(out):
    return [p for p in out.split("\0") if p]


def excluded(path, excludes):
    return any(path.startswith(e.rstrip("/") + "/") for e in excludes if e.rstrip("/"))


def untracked_cs():
    return set(split_z(git("ls-files", "-z", "--others", "--exclude-standard", "--", "*.cs")))


def changed_files(base, excludes):
    """(対象ファイル一覧, 未追跡ファイルの集合)。-z: 日本語・スペース入りパスを扱うため。"""
    untracked = untracked_cs()
    paths = split_z(git("diff", "-z", "--name-only", "--diff-filter=d", base, "--", "*.cs")) + sorted(untracked)
    return [p for p in paths if os.path.isfile(p) and not excluded(p, excludes)], untracked


def changed_lines(base):
    """{path: set(行番号)}。git diff -U0 のhunkの新側の行範囲。"""
    out = git("diff", "-U0", "--no-color", "--no-ext-diff", "--no-prefix", base, "--", "*.cs")
    result, cur, prev = {}, None, ""
    for line in out.splitlines():
        header = line.startswith("+++ ") and prev.startswith("--- ")  # 直前が '--- ' のときだけファイルヘッダ
        prev = line
        if header:
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


@functools.lru_cache(maxsize=None)
def cache_root():  # 相対指定でも最初の呼び出し時の cwd で固定する(後の chdir で動かさない)
    return Path(os.environ.get("RIDER_LINT_CACHES") or Path.home() / ".cache" / "rider-lint").expanduser().absolute()


def caches_dir(sln):
    """設定ファイルの内容ハッシュでキー付けする(設定変更後に古い結果を返さないため)。"""
    h = hashlib.sha256()
    for f in [".editorconfig", *sorted(Path().glob("*.sln.DotSettings")), ".config/dotnet-tools.json"]:
        with contextlib.suppress(OSError):
            h.update(Path(f).read_bytes())
    h.update(sln.encode())
    root, key = cache_root().resolve(), h.hexdigest()[:12]
    d = root / key
    d.mkdir(parents=True, exist_ok=True)
    d.touch()
    if root not in (Path("/"), Path.home().resolve()):  # 誤設定で危険な場所を消さない
        for old in root.iterdir():  # 14日より古い他キーだけ削除(他リポジトリ/worktreeの並行実行を壊さない)
            if old.is_dir() and old.name != key and time.time() - old.stat().st_mtime > MAX_AGE:
                shutil.rmtree(old, ignore_errors=True)
    return d


def run_tool(cmd, log, deadline):
    remaining = None if deadline is None else deadline - time.monotonic()
    if remaining is not None and remaining <= 0:
        raise LintRetry(f"{log} skipped: {JB_TIMEOUT}秒の制限を超えました")
    try:
        r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                           errors="replace", timeout=remaining)
    except subprocess.TimeoutExpired:
        raise LintRetry(f"{log} timed out ({JB_TIMEOUT}秒の制限)") from None
    except OSError as e:
        raise LintError(f"{log} を起動できません (dotnet がありませんか?): {e}") from e
    if r.returncode != 0:
        lines = [l for l in (r.stdout + r.stderr).splitlines() if not l.lstrip().startswith("at ")]
        raise LintError(f"{log} failed:\n" + "\n".join(lines[-20:]))


def jb(args, log, deadline):
    run_tool(["dotnet", "jb", *args], log, deadline)


RESTORE_RETRY_WAIT = 600  # restore に失敗した worktree で、次に restore を試みるまでの秒数


def ensure_restored(sln, deadline):
    """sln 内のプロジェクトに obj/project.assets.json が無ければ、dotnet tool restore と dotnet restore をする。
    失敗は LintRetry。失敗した worktree では RESTORE_RETRY_WAIT 秒は restore を試みない(毎回の Stop で時間を使わないため)。"""
    base = Path(sln).parent
    projects = [base / p.replace("\\", "/") for p in re.findall(r'"([^"]+\.csproj)"', Path(sln).read_text(encoding="utf-8-sig"))]
    existing = [p for p in projects if p.exists()]
    if existing and all((p.parent / "obj" / "project.assets.json").exists() for p in existing):
        return
    marker = cache_root() / f"restore-fail-{hashlib.sha256(str(base.resolve()).encode()).hexdigest()[:12]}.lock"
    with contextlib.suppress(OSError):
        if time.time() - marker.stat().st_mtime < RESTORE_RETRY_WAIT:
            raise LintRetry(f"最近 restore に失敗したため、{RESTORE_RETRY_WAIT}秒は restore を試みません")
    try:
        run_tool(["dotnet", "tool", "restore"], "dotnet tool restore", deadline)
        run_tool(["dotnet", "restore", sln], "dotnet restore", deadline)
    except LintRetry:
        raise
    except LintError as e:
        marker.touch()
        raise LintRetry(str(e)) from e
    marker.unlink(missing_ok=True)


def run_jb(sln, files, fix_files, caches, sarif, timeout=None):
    deadline = None if timeout is None else time.monotonic() + timeout
    common = [f"--caches-home={caches}", "--no-build"]
    if fix_files:
        jb(["cleanupcode", sln, "--profile=ClaudeCleanup", f"--include={';'.join(fix_files)}", *common],
           "cleanupcode", deadline)
    jb(["inspectcode", sln, f"--include={';'.join(files)}", "--severity=SUGGESTION", "--format=Sarif",
        f"--output={sarif}", *common], "inspectcode", deadline)


def filter_sarif(sarif, changed, untracked):
    """変更行(未追跡は全行)に入る指摘 [(path, line, rule, msg)] を返す。"""
    found, seen_uri, any_result = set(), False, False
    for run in json.loads(Path(sarif).read_text(encoding="utf-8")).get("runs", []):
        for r in run.get("results", []):
            any_result = True
            for loc in r.get("locations", []):
                pl = loc.get("physicalLocation", {})
                path = unquote(pl.get("artifactLocation", {}).get("uri", "")).replace("\\", "/")
                line = pl.get("region", {}).get("startLine")
                if not path or line is None:
                    continue
                seen_uri = seen_uri or os.path.isfile(path)
                if path in untracked or line in changed.get(path, ()):
                    found.add((path, line, r.get("ruleId", "?"), r.get("message", {}).get("text", "")))
    if any_result and not seen_uri:
        print("rider_lint: 警告: SARIFのuriが gitルート上のファイルに一致しません(slnがリポジトリ直下にない?)。"
              "指摘が0件に見えている可能性があります。", file=sys.stderr)
    return sorted(found)


def lint(sln, files, fix_files, untracked, base, timeout=None):
    """(指摘一覧, 自動修正でファイルが変わったか)。ツール失敗は LintError。"""
    # --include は ';' 区切りなので、';' を含むパスは渡せない
    for f in files:
        if ";" in f:
            print(f"rider_lint: ';' を含むパスは検査できないため除外: {f}", file=sys.stderr)
    files, fix_files = [f for f in files if ";" not in f], [f for f in fix_files if ";" not in f]
    if not files:
        return [], False
    def digest():
        return [hashlib.sha256(Path(f).read_bytes()).digest() for f in fix_files]
    before = digest()
    with tempfile.TemporaryDirectory() as tmp:
        sarif = Path(tmp) / "result.sarif"
        run_jb(sln, files, fix_files, caches_dir(sln), sarif, timeout)
        found = filter_sarif(sarif, changed_lines(base), untracked)
    return found, digest() != before


def esc(s, prop=False):
    s = s.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")
    return s.replace(":", "%3A").replace(",", "%2C") if prop else s


def fmt_finding(f, fmt="text"):
    path, line, rule, msg = f
    msg = msg.replace("\n", " ")
    if fmt == "github":
        return f"::warning file={esc(path, True)},line={line}::{esc(f'[{rule}] {msg}')}"
    return f"{path}:{line}: [{rule}] {msg}"


def cmd_check(a):
    files, untracked = changed_files(a.base, a.exclude)
    if not files:
        return 0
    try:
        found, _ = lint(a.sln, files, files if a.fix else [], untracked, a.base)
    except LintError as e:
        print(e, file=sys.stderr)
        return 2
    for f in found:
        print(fmt_finding(f, a.format))
    return 1 if found else 0


# ---- hook ----

def say(msg):
    print(json.dumps({"systemMessage": msg}, ensure_ascii=False))


@contextmanager
def flock(path, deadline=None):
    """deadline(time.monotonic の値)を指定すると、それまでしか待たず、取れなければ LintRetry。"""
    import fcntl  # Windows では存在しない。hook 経路でだけ必要
    with open(path, "w") as f:
        while deadline is not None:
            try:
                fcntl.flock(f, fcntl.LOCK_EX | fcntl.LOCK_NB)
                break
            except BlockingIOError:
                if time.monotonic() >= deadline:
                    raise LintRetry(f"他の検査のロック待ちで{JB_TIMEOUT}秒の制限を超えました") from None
                time.sleep(0.5)
        else:
            fcntl.flock(f, fcntl.LOCK_EX)
        yield


def read_names(touched):
    """.touched の記録を重複なしで読む。"""
    if not touched.exists():
        return []
    return list(dict.fromkeys(touched.read_text(encoding="utf-8").splitlines()))


def write_names(touched, names):
    touched.write_text("".join(n + "\n" for n in names), encoding="utf-8")


def sanitize(s):
    return re.sub(r"[^A-Za-z0-9_-]", "_", str(s or ""))


def cmd_hook(a):
    data = json.load(sys.stdin)
    event = data.get("hook_event_name")
    if event not in ("PostToolUse", "Stop", "SubagentStop"):
        return 0
    root = git_root(os.environ.get("CLAUDE_PROJECT_DIR") or ".")
    session = sanitize(data.get("session_id")) or "nosession"
    # .touched はエージェント単位。PostToolUse に agent_id が来ない仕様だった場合は、サブエージェントの編集が
    # メインの .touched に入ってメインの Stop で検査され、SubagentStop は空なので何もしない(どちらでも安全側)。
    agent = sanitize(data.get("agent_id"))
    state = cache_root()  # chdir 前に求めた絶対パス。グループごとの chdir の影響を受けない
    state.mkdir(parents=True, exist_ok=True)
    for pattern in ("*.touched", "*.handover", "*.lock"):
        for f in state.glob(pattern):
            with contextlib.suppress(OSError):
                if f.name != "jb.lock" and time.time() - f.stat().st_mtime > MAX_AGE:
                    f.unlink(missing_ok=True)
    touched = state / (f"{session}_{agent}.touched" if agent else f"{session}.touched")
    state_lock = state / f"{session}.lock"  # .touched / .handover の読み書きだけを守る短いロック
    # サブエージェントが時間切れ等で検査できなかった記録の引き渡し先。同じセッションのメインの Stop が引き取る
    handover = state / f"{session}.handover"
    if event == "PostToolUse":
        with flock(state_lock):
            return record(data, root, touched, a.exclude)
    return on_stop(a, root, bool(data.get("stop_hook_active")), touched, state_lock, state / "jb.lock", handover,
                   is_sub=bool(agent) or event == "SubagentStop")


def record(data, root, touched, excludes):
    path = (data.get("tool_input") or {}).get("file_path")
    if not path or not path.endswith(".cs"):
        return 0
    real_root = os.path.realpath(root)
    rel = os.path.relpath(os.path.realpath(path), real_root)
    if rel.startswith("..") or excluded(rel, excludes):
        return 0
    # 記録は絶対パス。どの git ルート(worktree)のファイルかは Stop 時に求める(PostToolUse は 10 秒制限なので git を呼ばない)
    real = os.path.realpath(path)
    if real not in read_names(touched):
        with open(touched, "a", encoding="utf-8") as f:
            f.write(real + "\n")
    return 0


def pick_sln(root, name):
    """root 直下の name の sln。無ければ、直下の *.sln が1つだけならそれ。どちらも無ければ None。"""
    if (Path(root) / name).is_file():
        return name
    slns = sorted(p.name for p in Path(root).glob("*.sln"))
    return slns[0] if len(slns) == 1 else None


def group_by_root(items):
    """items=[(記録名, 絶対パス)] を ({git ルート: [(記録名, ルート相対パス)]}, git ルートが取れなかった記録名) にする。
    ディレクトリごとに git を1回だけ呼ぶ。"""
    roots, groups, noroot = {}, {}, []
    for name, p in items:
        d = os.path.dirname(p)
        if d not in roots:
            try:
                roots[d] = os.path.realpath(git_root(d))
            except (subprocess.CalledProcessError, OSError):
                roots[d] = None
        if roots[d]:
            groups.setdefault(roots[d], []).append((name, os.path.relpath(p, roots[d])))
        else:
            noroot.append(name)
    return groups, noroot


def flush(notes):
    if notes:
        say("\n".join(notes))


def split_group(a, root, items):
    """(検査する [(名前, 相対パス)], 検査しない名前, sln)。--exclude は各 git ルート(submodule なら親のルート)相対でも判定する。"""
    tops = [root]  # worktree 内の public/ を外すため
    with contextlib.suppress(subprocess.CalledProcessError, OSError):
        parent = git("rev-parse", "--show-superproject-working-tree", root=root).strip()
        if parent:
            tops.append(parent)
    sln = pick_sln(root, a.sln)
    checked = [] if sln is None else [
        (n, r) for n, r in items if not any(excluded(os.path.relpath(os.path.join(root, r), t), a.exclude) for t in tops)]
    return checked, [n for n, _ in items if n not in {n for n, _ in checked}], sln


def on_stop(a, project_root, active, touched, state_lock, jb_lock, handover, is_sub):
    """is_sub: サブエージェントの Stop。時間切れ等の分は handover へ移す。メインの Stop は handover も読んで検査する
    (引き取り分は他エージェントの worktree のファイルなので、自動修正せず検査と通知だけにする)。"""
    with flock(state_lock):
        own = read_names(touched)
        taken = [] if is_sub else [n for n in read_names(handover) if n not in own]
    names = own + taken
    if not names:
        return 0

    def consume(done):  # 指定した分だけ除く(jb 実行中に PostToolUse が追記した分は残す)
        with flock(state_lock):
            for f in (touched,) if is_sub else (touched, handover):  # サブエージェントは handover を読まないので触らない
                write_names(f, [n for n in read_names(f) if n not in done])

    project_root = os.path.realpath(project_root)
    items = [(n, os.path.realpath(os.path.join(project_root, n))) for n in names]  # 旧形式(相対)の記録も読めるように
    # 実在しない・--exclude 対象の記録は、検査せずに捨てる
    items = [(n, p) for n, p in items if os.path.isfile(p) and not excluded(os.path.relpath(p, project_root), a.exclude)]
    taken_paths = {p for n, p in items if n in set(taken)}  # 引き取り分(自動修正しない・指摘は通知だけ)
    groups, noroot = group_by_root(items)
    drop = set(names) - {n for n, _ in items} | set(noroot)  # 今回で記録から除く分(検査しない・この内容では再実行しない)
    retry = {}  # 時間切れ・restore 失敗で検査できなかった分 {記録名: 理由}。記録に残して(サブエージェントなら引き渡して)再検査する
    notes = []  # stdout の JSON は1回の Stop で1つにまとめる
    if noroot:
        notes.append("rider_lint: git ルートが取れないため検査しませんでした: " + ", ".join(noroot))
    own_lines, taken_lines, fixed_any = [], [], False
    deadline = time.monotonic() + JB_TIMEOUT  # ロック待ちも含めて、フックの timeout を超えないようにする
    with contextlib.ExitStack() as stack:
        try:
            stack.enter_context(flock(jb_lock, deadline))  # jb とキャッシュの同時実行を防ぐ(全セッション共通)
        except LintRetry as e:  # ロックが取れなかった。全グループを次回に回す
            retry.update((n, f"{e}") for n, _ in items)
            groups = {}
        for root, group_items in groups.items():
            checked, skipped, sln = split_group(a, root, group_items)
            drop.update(skipped)
            if not checked:
                continue
            rels = list(dict.fromkeys(r for _, r in checked))
            fix_rels = [r for r in rels if os.path.join(root, r) not in taken_paths]
            try:
                found, fixed = lint_group(root, sln, rels, fix_rels, deadline)
            except LintRetry as e:
                retry.update((n, f"[{root}]: {e}") for n, _ in checked)
                continue
            except Exception as e:  # noqa: BLE001  想定外でも他のグループの結果と記録の整理は続ける
                notes.append(f"rider_lint 実行失敗(この内容では再実行しません) [{root}]: {e if isinstance(e, LintError) else repr(e)}")
                drop.update(n for n, _ in checked)
                continue
            fixed_any = fixed_any or fixed
            for f in found:
                path = os.path.join(root, f[0])
                (taken_lines if os.path.realpath(path) in taken_paths else own_lines).append(fmt_finding((path, *f[1:])))
    if retry:
        reasons = list(dict.fromkeys(retry.values()))
        if is_sub:  # このエージェントは再検査しない。同じセッションのメインの Stop に引き渡す
            with flock(state_lock):
                write_names(handover, list(dict.fromkeys(read_names(handover) + list(retry))))
            drop.update(retry)
            suffix = "(時間切れ等のため、メインのセッションの応答終了時に検査します)"
        else:
            suffix = "(記録を残したので、次回の応答終了時に再検査します)"
        notes += [f"rider_lint 検査できませんでした {r}{suffix}" for r in reasons]
    # exit 2 のときは stdout の JSON が読まれないので、引き取り分の指摘は stderr 側に載せる
    taken_note = ("サブエージェントが検査できず、メインが引き取ったファイルの指摘です(自動修正はしていません):\n" + "\n".join(taken_lines)
                  if taken_lines else "")
    done = set(names) - set(retry) | set(drop)  # 検査を終えた分
    out = "\n".join(own_lines)
    if not own_lines:
        if taken_note:
            notes.append(taken_note)
        consume(done)
        if fixed_any:
            notes.append("Rider検査: 指摘は cleanupcode による自動修正で解消しました(ファイルが書き換わっています)。")
        flush(notes)
        return 0
    if active:  # 差し戻しは1回まで
        if taken_note:
            notes.append(taken_note)
        consume(done)
        notes.append("Rider検査の指摘が残っています(差し戻し済みのため終了しました):\n" + out)
        flush(notes)
        return 0
    note = "(機械的に直せるものは cleanupcode で自動修正済みです。ファイルが書き換わっているので、編集前に必ず再Readしてください)\n" if fixed_any else ""
    flush(notes)
    # 自分の検査済みの分は、差し戻し後の再実行のために残す。引き取り分は通知済みなので記録から除く
    consume(drop | (set(taken) - set(retry)))
    taken_part = f"\n\n{taken_note}\n(上は、あなたの変更ではなく、引き取ったファイルの指摘です。このあと記録から除きます)" if taken_note else ""
    print(f"Rider(ReSharper CLI)の検査で、あなたが変更した行に指摘があります。修正してください。\n{note}\n{out}{taken_part}", file=sys.stderr)
    return 2


def lint_group(root, sln, files, fix_files, deadline):
    """git ルート root(メイン作業ツリーまたは worktree)内の files を、そのルートの sln・設定・差分で検査する。"""
    cwd = os.getcwd()
    os.chdir(root)
    try:
        ensure_restored(sln, deadline)
        return lint(sln, files, fix_files, untracked_cs() & set(files), "HEAD", timeout=deadline - time.monotonic())
    finally:
        os.chdir(cwd)


def main():
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    sub = ap.add_subparsers(dest="cmd", required=True)
    for name in ("check", "hook"):
        p = sub.add_parser(name)
        p.add_argument("--sln", required=True)
        p.add_argument("--exclude", action="append", default=[])
        if name == "check":
            p.add_argument("--base", default="HEAD")
            p.add_argument("--fix", action="store_true")
            p.add_argument("--format", choices=["text", "github"], default="text")
    a = ap.parse_args()
    try:
        if a.cmd == "check":
            os.chdir(git_root())
            return cmd_check(a)
        return cmd_hook(a)
    except Exception as e:  # noqa: BLE001
        print(f"rider_lint {a.cmd}: {e!r}", file=sys.stderr)
        return 2 if a.cmd == "check" else 0  # hook は fail-open: フックの不具合で作業を止めない


if __name__ == "__main__":
    sys.exit(main())
