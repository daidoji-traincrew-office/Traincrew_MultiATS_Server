---
name: DBスキーマ変更
description: DBスキーマを変更します。C#モデルクラス、DbContext、schema.sqlを更新し、Atlasコマンドでマイグレーションを自動生成します。
---

# DBスキーマ変更コマンド

このコマンドは、ユーザーからの追加メッセージに基づいてデータベーススキーマを変更します。

**重要**: マイグレーションファイルは手動で作成せず、schema.sql更新後にAtlasコマンドで自動生成します。

## 実行手順

### 1. スキーマ変更内容の確認
ユーザーからの追加メッセージでスキーマ変更の詳細を確認してください。以下の情報を把握します：
- 新しいテーブル/カラムの追加
- 既存テーブル/カラムの修正
- リレーション（外部キー）の設定
- Enum型の追加
- インデックスの設定

### 2. C#モデルクラスの作成/修正

**ディレクトリ**: `Traincrew_MultiATS_Server\Models\`

以下のガイドラインに従ってモデルクラスを作成/修正してください：

- **命名規則**: パスカルケース（例: `TrainState`, `RouteType`）
- **必須属性**:
  - `[Table("テーブル名")]` - snake_caseでテーブル名を指定
  - `[Key]` - 主キープロパティに付与
  - `[Column("カラム名")]` - 必要に応じてカラム名を明示的に指定
  - `[ForeignKey("ナビゲーションプロパティ名")]` - 外部キー設定
- **継承関係**: 必要に応じて `InterlockingObject` などの基底クラスを継承
- **ナビゲーションプロパティ**: リレーションがある場合は必ず定義

**例**:
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Traincrew_MultiATS_Server.Models
{
    [Table("example_table")]
    public class ExampleTable
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("station_id")]
        public int StationId { get; set; }

        // ナビゲーションプロパティ
        [ForeignKey(nameof(StationId))]
        public Station? Station { get; set; }
    }
}
```

### 3. ApplicationDbContext.cs の更新

**ファイルパス**: `Traincrew_MultiATS_Server\Data\ApplicationDbContext.cs`

以下を実施してください：

#### 3.1 DbSetプロパティの追加
```csharp
public DbSet<新しいモデルクラス名> 新しいモデルクラス名の複数形 { get; set; }
```

**例**:
```csharp
public DbSet<ExampleTable> ExampleTables { get; set; }
```

#### 3.2 OnModelCreatingメソッドの更新（必要に応じて）
リレーション、インデックス、制約などの追加設定が必要な場合：

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // 外部キー設定
    modelBuilder.Entity<ExampleTable>()
        .HasOne(e => e.Station)
        .WithMany()
        .HasForeignKey(e => e.StationId);

    // インデックス設定
    modelBuilder.Entity<ExampleTable>()
        .HasIndex(e => e.Name);
}
```

**注意**: このプロジェクトはCamelCaseを自動的にsnake_caseに変換するため、明示的なカラム名マッピングは最小限に留めてください。

### 4. Enum型マッピング（新しいEnum型を追加する場合）

**ファイルパス**: `Traincrew_MultiATS_Server\Data\EnumTypeMapper.cs`

新しいEnum型を追加する場合：

```csharp
public static class EnumTypeMapper
{
    public static void MapEnumTypes(NpgsqlConnection connection)
    {
        // 既存のマッピング...

        // 新しいEnum型を追加
        connection.TypeMapper.MapEnum<新しいEnumクラス名>("enum_type_name");
    }
}
```

**新しいEnum型の定義場所**: `Traincrew_MultiATS_Server.Common\Models\`

### 5. schema.sql の更新（最優先）

**ファイルパス**: `Database\schema.sql`

**重要**: schema.sqlを先に更新してください。このファイルがAtlasのソースとなります。

schema.sqlに変更内容を反映してください：

**新しいテーブルを追加する場合**:
```sql
-- 新しいテーブルの作成
CREATE TABLE example_table (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    station_id INTEGER NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_station FOREIGN KEY (station_id) REFERENCES stations(id)
);

-- インデックスの作成
CREATE INDEX idx_example_table_name ON example_table(name);
```

**新しいEnum型を追加する場合**:
```sql
-- Enum型の定義（ファイルの先頭付近、他のEnum型定義の近くに配置）
CREATE TYPE example_enum AS ENUM ('value1', 'value2', 'value3');
```

**既存テーブルにカラムを追加する場合**:
既存のテーブル定義を直接編集してカラムを追加します。
```sql
CREATE TABLE existing_table (
    id SERIAL PRIMARY KEY,
    existing_column VARCHAR(100),
    new_column VARCHAR(100) NOT NULL,  -- 新しいカラムを追加
    ...
);
```

**重要な注意事項**:
- テーブル名とカラム名は必ず **snake_case** を使用
- 外部キー制約は `CONSTRAINT` で名前を付けて定義
- NOT NULL制約やデフォルト値を適切に設定
- インデックスが必要な場合は作成
- **配置場所**: 関連するテーブルの近くに配置し、論理的なグルーピングを維持
- Enum型定義はファイルの先頭付近に配置

### 6. Atlasコマンドでマイグレーション生成

schema.sql更新後、Atlasコマンドでマイグレーションファイルを自動生成します。

**作業ディレクトリ**: `Database\`

**実行コマンド**:
```bash
cd Database
atlas migrate diff <変更内容の説明> --env local
```

**コマンドの説明**:
- `<変更内容の説明>`: マイグレーション名（例: `add_user_preferences`, `add_station_is_active`）
- `--env local`: atlas.hclで定義された環境を指定（通常は`local`を使用）

**実行例**:
```bash
cd Database
atlas migrate diff add_user_preferences --env local
```

Atlasは以下を自動的に行います：
1. schema.sqlと現在のデータベーススキーマを比較
2. 差分を検出
3. `Database\migrations\` に新しいマイグレーションファイルを生成（タイムスタンプ付き）
4. `Database\migrations\atlas.sum` を更新

**生成されるファイル例**:
```
Database\migrations\20251220123456_add_user_preferences.sql
```

**注意事項**:
- Atlasコマンドを実行する前に、必ずschema.sqlを更新してください
- 環境によっては `--env dev`, `--env raspi` などを使用する場合があります
- マイグレーション名は英語のsnake_caseで記述してください

### 7. 変更内容の確認

以下を確認してください：

1. **C#モデルクラス** - 正しい属性とプロパティが定義されているか
2. **ApplicationDbContext.cs** - DbSetが追加され、リレーションが正しく設定されているか
3. **EnumTypeMapper.cs** - 新しいEnum型がマッピングされているか（必要な場合）
4. **マイグレーションSQL** - 正しいSQLコマンドが記述されているか
5. **schema.sql** - マスタースキーマが最新状態に更新されているか

### 8. ビルド確認

変更後、プロジェクトがビルドできることを確認してください：

```bash
dotnet build
```

エラーが発生した場合は修正してください。

## 既存のモデルクラス参考例

プロジェクト内の既存モデルクラスを参考にしてください：
- `Traincrew_MultiATS_Server\Models\Route.cs`
- `Traincrew_MultiATS_Server\Models\Signal.cs`
- `Traincrew_MultiATS_Server\Models\TrackCircuit.cs`
- `Traincrew_MultiATS_Server\Models\RouteState.cs`

## 技術スタック情報

- **ORM**: Entity Framework Core 8
- **データベース**: PostgreSQL
- **マイグレーション管理**: Atlas.io + カスタムSQLマイグレーション
- **命名規則**: C# (PascalCase/camelCase) → DB (snake_case) 自動変換
- **Enum管理**: Npgsql TypeMapping（PostgreSQL native enum型）

## 完了報告

すべての変更が完了したら、以下の情報をユーザーに報告してください：

1. **作成/修正したC#モデルクラス**
   - ファイルパスとクラス名をリスト化

2. **ApplicationDbContext.csの変更**
   - 追加したDbSetプロパティ
   - OnModelCreatingでの設定内容（ある場合）

3. **schema.sqlの変更内容**
   - 追加/修正したテーブル定義
   - 新しいEnum型（ある場合）

4. **Atlasで生成されたマイグレーションファイル**
   - ファイル名（例: `20251220123456_add_user_preferences.sql`）
   - 生成されたSQL文の概要

5. **ビルド結果**
   - ビルドが成功したかどうか
   - エラーがあれば修正内容

## 次のステップ（マイグレーション適用方法）

ユーザーに以下の手順を伝えてください：

### マイグレーションの適用

**開発環境（local）に適用**:
```bash
cd Database
atlas migrate apply --env local
```

**他の環境に適用**:
```bash
# dev環境
atlas migrate apply --env dev

# raspi環境
atlas migrate apply --env raspi

# 本番環境
atlas migrate apply --env prod
```

### マイグレーション状態の確認

```bash
cd Database
atlas migrate status --env local
```

これにより、適用済みマイグレーションと未適用マイグレーションを確認できます。

## トラブルシューティング

### Atlasコマンドが失敗する場合

1. **接続エラー**: atlas.hclの接続文字列を確認
2. **スキーマ差分が検出されない**: schema.sqlが正しく更新されているか確認
3. **マイグレーション競合**: atlas.sumファイルを確認し、必要に応じて再生成

### ビルドエラーが発生する場合

1. **名前空間エラー**: using文が正しく記述されているか確認
2. **型エラー**: モデルクラスのプロパティ型がDBスキーマと一致しているか確認
3. **DbSet未定義エラー**: ApplicationDbContext.csにDbSetプロパティが追加されているか確認