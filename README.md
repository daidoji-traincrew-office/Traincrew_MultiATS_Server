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
DockerでPostgresを立ち上げる
```
cd Database
docker compose up -d
```

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