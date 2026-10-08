# CLAUDE.md

## コーディング規約

C#のコードは、JetBrains Rider(ReSharper)の検査で指摘が出ない形で書くこと。特に次の点を守る。

- ローカル変数は、型が明らかでなくても `var` で宣言する(`var sb = new StringBuilder();`、`var n = list.Count;`)。
- フィールド・プロパティ・引数のオブジェクト生成は target-typed new を使う(`private readonly Foo _x = new();`、`new Foo()` ではなく `new()`)。`var x = new Foo()` はそのままでよい。
- コレクション初期化は collection expression (`[]`) を使える場合は使う。
- 要素を詰め替えるだけの `foreach` / `for` ループは書かず、LINQ (`Select` / `Where` / `ToList()` など) にする。
- 使っていないメンバー・`using`・変数を残さない。

設定は `.editorconfig` と `Traincrew_MultiATS_Server.sln.DotSettings` にある。

### 自動検査 (PostToolUse / Stop / SubagentStop フック)

`.claude/settings.json` のフック (`.github/scripts/rider_lint.py hook`) が、Claude が `Edit` / `Write` / `MultiEdit` で触った `.cs` を記録し、応答の終了時にそのファイルの変更行へ `jb inspectcode` を実行する。

- 検査対象は PostToolUse で記録したファイルだけ(ユーザーが手で編集中のファイルは対象外)。何も触っていなければ `jb` は起動しない。
- 機械的に直せる指摘 (`var` 化、`new()` 化など) は `jb cleanupcode` で自動修正される。ファイルが書き換わるため、差し戻されたら編集前に再Readすること。
- 自動修正は**ファイル単位**で、Claude が触ったファイルの既存行にもかかる。Claude とユーザーが同じファイルを同時に編集すると、そのファイルも修正対象になる。
- 変更した行に残った指摘は exit 2 で差し戻される。直してから終了すること(差し戻しは1回まで。2回目は通知のみで終了する)。
- `stop_hook_active` が既に true のとき(他のフックが継続させた場合など)は、1回目でも差し戻さず通知のみで終了する。
- フックの想定外のエラーは何もせず終了する(fail-open)。python3 が必要。
- 手動実行: `python3 .github/scripts/rider_lint.py check --sln Traincrew_MultiATS_Server.sln --base HEAD --fix`
- 初回は `dotnet tool restore` と `dotnet restore` が必要。
- CI (`.github/workflows/riderLint.yml`) でも PR の変更行に対して同じ検査を行い、指摘があれば失敗する。
