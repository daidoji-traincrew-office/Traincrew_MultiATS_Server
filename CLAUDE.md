# CLAUDE.md

## コーディング規約

C#のコードは、JetBrains Rider(ReSharper)の検査で指摘が出ない形で書くこと。特に次の点を守る。

- ローカル変数は、型が明らかでなくても `var` で宣言する(`var sb = new StringBuilder();`、`var n = list.Count;`)。
- フィールド・プロパティ・引数のオブジェクト生成は target-typed new を使う(`private readonly Foo _x = new();`、`new Foo()` ではなく `new()`)。`var x = new Foo()` はそのままでよい。
- コレクション初期化は collection expression (`[]`) を使える場合は使う。
- 要素を詰め替えるだけの `foreach` / `for` ループは書かず、LINQ (`Select` / `Where` / `ToList()` など) にする。
- 使っていないメンバー・`using`・変数を残さない。

設定は `.editorconfig` と `Traincrew_MultiATS_Server.sln.DotSettings` にある。

### 自動検査 (SessionStart / Stop / SubagentStop フック)

`.claude/settings.json` のフック (`.claude/hooks/rider_lint_hook.sh`) が、応答の終了時に変更した `.cs` へ `jb inspectcode` を実行する。

- 機械的に直せる指摘 (`var` 化、`new()` 化など) は `jb cleanupcode` で自動修正される。ファイルが書き換わるため、差し戻されたら編集前に再Readすること。
- セッション開始時点ですでに変更済み・未追跡だったファイルは、自動修正の対象外(検査のみ)。
- 変更した行に残った指摘は exit 2 で差し戻される。直してから終了すること(同一セッションで差し戻されるのは2回まで)。
- 手動実行: `.github/scripts/rider_lint.sh --sln Traincrew_MultiATS_Server.sln --base HEAD --fix`
- 初回は `dotnet tool restore` と `dotnet restore` が必要。
- CI (`.github/workflows/riderLint.yml`) でも PR の変更行に対して同じ検査を行う。
