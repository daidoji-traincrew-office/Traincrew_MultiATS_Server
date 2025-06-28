# Traincrew_MultiATS_Server

Traincrew運転会用のマルチATSサーバー。

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/daidoji-traincrew-office/Traincrew_MultiATS_Server)

## 開発者向け初期セットアップ

### Docker
Dockerを入れる
#### Windows/Mac
Docker Desktopを入れる
https://docs.docker.com/desktop/setup/install/windows-install/

https://docs.docker.com/desktop/setup/install/mac-install/
#### Linux
Dockerを入れる

https://docs.docker.com/engine/install/

### Postgres
#### (Optional) マイグレーションを含めた開発
Atlasによるマイグレーションを用いた開発を行う場合、compose.ymlを編集し、コンテナ起動時のスキーマ自動適用を無効にする

```yaml
    volumes:
      - ./data:/var/lib/postgresql/data
      # 以下の行をコメントアウトする
      # - ./schema.sql:/docker-entrypoint-initdb.d/00_schema.sql
```

#### 起動方法
DockerでPostgresを立ち上げる
```
cd Database
docker compose up -d
```
#### DBのデータを消す方法(特に開発中にスキーマを変更した場合)
※この操作を行うと、DBのデータが全て消えます。注意してください。
※マイグレーションを使用している場合は、一般にはマイグレーションの適用で十分です。

docker compose down でコンテナを停止し、削除する
```
cd Database
docker compose down -v
```

`Database/data`以下にDBのデータが保存されているので、そのフォルダを削除すればOK

### Atlas

まずAtlasを入れる
https://atlasgo.io/docs

#### マイグレーションの適用
開発を始めた時及び誰かがマイグレーションを追加した時に実行する
```
atlas migrate apply --env local
```
#### マイグレーションの作成
スキーマを変更したらやる マイグレーション名は適宜変更
```
atlas migrate diff --env local add_column_to_table
```
## デバッグ(サーバーの起動方法)
### Visual Studio

### Rider
起動プロファイルを以下に設定

- `Traincrew_MultiATS_Server.Crew` (ATS、信号盤、TID、司令卓用サーバー)
- `Traincrew_MultiATS_Server.Passenger` (お客様用アプリ向けサーバー)

その後、デバッグボタンを押す


詳しくはこちらを参照すること。
https://pleiades.io/help/rider/Debugging_Code.html

### Terminal
```
# 乗務員用サーバー
cd Traincrew_MultiATS_Server.Crew
dotnet run -lp https

# お客様用サーバー
cd Traincrew_MultiATS_Server.Passenger
dotnet run -lp https
```

## フォルダ構成について

- `Database` フォルダ
  - PostgresのDocker Composeファイルとスキーマ定義、マイグレーションファイルが入ってる
- `Traincrew_MultiATS_Server` フォルダ
  - ATS、信号盤、TID、司令卓用のサーバー。
- `Traincrew_MultiATS_Server.Passenger` フォルダ
  - お客様用アプリ向けのサーバー。
- `Traincrew_MultiATS_Server.Common` フォルダ
  - サーバーとクライアント間で共通使用するスキーマ定義を入れる
- `Traincrew_MultiATS_Server.Core` フォルダ
  - ATS用サーバーとお客様アプリ用サーバーで共通で使うコードを入れる
- `Traincrew_MultiATS_Server.UT` フォルダ
  - ユニットテスト用のコードを入れる
- `Traincrew_MultiATS_Server.IT` フォルダ
  - 統合テスト用のコードを入れる