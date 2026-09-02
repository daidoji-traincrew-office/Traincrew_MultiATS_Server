# DatabaseInitializer Snapshot Test

## 概要

このテストは、DBを空の状態から初期化した際に、期待されるスキーマとデータの状態になっているかをスナップショットで検証します。

## テストの種類

### 1. `InitializeAsync_EmptyDatabase_MatchesExpectedSnapshot`
空のDBから初期化した際に、期待されるスナップショットと一致することを検証するテストです。

- DBのすべてのテーブルをクリア
- DatabaseInitializationOrchestratorを実行してDBを初期化
- 初期化後のDBスナップショットを取得
- 期待されるスナップショットと比較

### 2. `GenerateExpectedSnapshot`
期待されるスナップショットファイルを生成するための手動実行用テストです。
通常のテスト実行ではスキップされます。

## スナップショットファイルの生成方法

初回実行時、または期待されるDB状態が変更された際には、スナップショットファイルを再生成する必要があります。

### 手順

1. DBが正しい初期状態にあることを確認
2. テストファイルで`GenerateExpectedSnapshot`のSkip属性を一時的にコメントアウト
3. テストを実行:
   ```bash
   dotnet test Traincrew_MultiATS_Server.IT/Traincrew_MultiATS_Server.IT.csproj --filter "FullyQualifiedName~DatabaseInitializerSnapshotTest.GenerateExpectedSnapshot"
   ```
4. `Traincrew_MultiATS_Server.IT/Initialization/Snapshots/expected_db_snapshot.txt` にスナップショットが生成される
5. Skip属性を元に戻す

## スナップショットの内容

スナップショットには以下の情報が含まれます:

- テーブル名
- 行数(RowCount)
- スキーマ(カラム名、データ型、NULL許容、デフォルト値)

### 除外対象

以下のテーブルはスナップショットから除外されます:

- `__EFMigrationsHistory`: EFマイグレーション履歴
- `OpenIddict*`: OpenIddictの認証テーブル

## テストの実行

通常のテスト実行:
```bash
dotnet test Traincrew_MultiATS_Server.IT/Traincrew_MultiATS_Server.IT.csproj --filter "FullyQualifiedName~DatabaseInitializerSnapshotTest.InitializeAsync_EmptyDatabase_MatchesExpectedSnapshot"
```

## トラブルシューティング

### スナップショットファイルが見つからない

```
Expected snapshot file not found at: ...
Please run the GenerateExpectedSnapshot test first to create the snapshot file.
```

上記の「スナップショットファイルの生成方法」に従ってスナップショットファイルを生成してください。

### スナップショットの相違がある

```
Database snapshot mismatch:
Row count differences:
  - station: Expected 10, Actual 11
```

このような相違が出た場合:

1. 期待されるDB状態が変更された場合は、スナップショットを再生成
2. バグの場合は、初期化処理を修正

## 実装の詳細

### DatabaseSnapshotHelper

DBスナップショットを取得・比較するためのユーティリティクラス:

- `CreateSnapshotAsync`: DBの現在の状態からスナップショットを生成
- `SerializeSnapshot`: スナップショットをテキスト形式にシリアライズ
- `CompareSnapshots`: 2つのスナップショットを比較して差分を返す

### スナップショット形式

```
Table: station
RowCount: 10
Schema:
id|character varying|NO|
name|character varying|NO|
is_station|boolean|NO|
is_passenger_station|boolean|NO|
```
