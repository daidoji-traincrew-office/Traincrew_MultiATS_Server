# CLAUDE.md

## コーディング規約

C#のコードは、JetBrains Rider(ReSharper)の検査で指摘が出ない形で書くこと。特に次の点を守る。

- ローカル変数は、型が明らかでなくても `var` で宣言する(`var sb = new StringBuilder();`、`var n = list.Count;`)。
- フィールド・プロパティ・引数のオブジェクト生成は target-typed new を使う(`private readonly Foo _x = new();`、`new Foo()` ではなく `new()`)。`var x = new Foo()` はそのままでよい。
- コレクション初期化は collection expression (`[]`) を使える場合は使う。
- 要素を詰め替えるだけの `foreach` / `for` ループは書かず、LINQ (`Select` / `Where` / `ToList()` など) にする。
- 使っていないメンバー・`using`・変数を残さない。
- `if` / `else` / `for` / `foreach` / `while` / `do` / `using` / `lock` / `fixed` は本体が1行でも `{}` を付ける。

設定は `.editorconfig` と `Traincrew_MultiATS_Server.sln.DotSettings` にある。

### 自動検査 (PostToolUse / Stop / SubagentStop フック)

`.claude/settings.json` のフック (`.github/scripts/rider_lint.py hook`) が、Claude が `Edit` / `Write` / `MultiEdit` で触った `.cs` を記録し、応答の終了時にそのファイルの変更行へ `jb inspectcode` を実行する。

- 検査対象は PostToolUse で記録したファイルだけ(ユーザーが手で編集中のファイルは対象外)。何も触っていなければ `jb` は起動しない。
- 機械的に直せる指摘 (`var` 化、`new()` 化など) は `jb cleanupcode` で自動修正される。ファイルが書き換わるため、差し戻されたら編集前に再Readすること。
- 自動修正は**ファイル単位**で、Claude が触ったファイルの既存行にもかかる。Claude とユーザーが同じファイルを同時に編集すると、そのファイルも修正対象になる。
- 変更した行に残った指摘は exit 2 で差し戻される。直してから終了すること(差し戻しは1回まで。2回目は通知のみで終了する)。
- `stop_hook_active` が既に true のとき(他のフックが継続させた場合など)は、1回目でも差し戻さず通知のみで終了する。
- `.worktrees/<名前>/` など別の git worktree で編集したファイルは、その worktree の git ルートごとにまとめ、**その worktree の sln・`.editorconfig`・`*.sln.DotSettings`・git 差分(HEAD 基準)**で検査する。記録も差し戻しのパス(メインの作業ツリーのものを含む)も絶対パス。`CLAUDE_PROJECT_DIR` の外にある worktree(`/tmp` など)のファイルは記録されず、検査されない。
- sln は、各 git ルート直下の `--sln` で指定した名前のものを使う。無ければ、そのルート直下の `*.sln` が1つだけならそれを使う。どちらも無ければそのルートのファイルは検査しない。`--exclude` は、フックを動かしたプロジェクトのルートからの相対と、各 git ルート(submodule なら親のルート)からの相対の両方で判定する。
- sln 内のプロジェクトの `obj/project.assets.json` が無い(restore していない)worktree では、フックが `dotnet tool restore` と `dotnet restore` をしてから検査する(初回は時間がかかる)。全体で 270 秒の制限に収まらなければ、その回は検査せず通知して終了する。restore は残るので、次回から検査される。時間切れ・ロック待ち切れ・restore 失敗で検査できなかったファイルは記録を残す。メイン(Stop)の分はメイン自身の次の Stop で再検査し、サブエージェント(SubagentStop)の分は同じセッションの引き取り記録(`<セッション>.handover`)へ移して、メインの次の Stop が引き取って検査する(終了して再開しないサブエージェントの分もここで拾う。差し戻し中のサブエージェント自身の記録は、メインは読まない)。メインが引き取った分は、他のエージェントが作業中の worktree のファイルなので、自動修正はせず、検査結果の通知だけにする(restore に失敗した worktree では10分間 restore を試みず、通知だけ出す)。
- フックが restore を自動実行するので、その worktree 内の `nuget.config`・`Directory.Build.props`・`.config/dotnet-tools.json` などが評価される。エージェントが編集した内容がそのまま実行される点に注意する。
- フックの想定外のエラーは何もせず終了する(fail-open)。python3 が必要。
- 手動実行: `python3 .github/scripts/rider_lint.py check --sln Traincrew_MultiATS_Server.sln --base HEAD --fix`
- 手動実行(`check`)の初回は `dotnet tool restore` と `dotnet restore` が必要。フックは未 restore なら自動で行う。
- CI (`.github/workflows/riderLint.yml`) でも PR の変更行に対して同じ検査を行い、指摘があれば失敗する。
