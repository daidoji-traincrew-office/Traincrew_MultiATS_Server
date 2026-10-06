# CLAUDE.md

## コーディング規約

C#のコードは、JetBrains Rider(ReSharper)の検査で指摘が出ない形で書くこと。特に次の点を守る。

- オブジェクト生成は target-typed new を使う。型が左辺で分かる場合は `Foo x = new Foo();` ではなく `Foo x = new();`、フィールド・プロパティ・引数でも `new Foo()` ではなく `new()`。
- コレクション初期化は collection expression (`[]`) を使える場合は使う。
- 要素を詰め替えるだけの `foreach` / `for` ループは書かず、LINQ (`Select` / `Where` / `ToList()` など) にする。
- 使っていないメンバー・`using`・変数を残さない。

設定は `.editorconfig` と `Traincrew_MultiATS_Server.sln.DotSettings` にある。

### 自動検査 (Stop / SubagentStop フック)

`.claude/settings.json` の Stop / SubagentStop フック (`.claude/hooks/rider_lint_hook.sh`) が、応答の終了時に変更した `.cs` へ `jb inspectcode` を実行する。

- 機械的に直せる指摘 (`new()` 化など) は `jb cleanupcode` で自動修正される。ファイルが書き換わるため、差し戻されたら編集前に再Readすること。
- 変更した行に残った指摘は exit 2 で差し戻される。直してから終了すること(同一セッションで差し戻されるのは2回まで)。
- 手動実行: `.github/scripts/rider_lint.sh --sln Traincrew_MultiATS_Server.sln --base HEAD --fix`
- 初回は `dotnet tool restore` と `dotnet restore` が必要。
- CI (`.github/workflows/riderLint.yml`) でも PR の変更行に対して同じ検査を行う。
