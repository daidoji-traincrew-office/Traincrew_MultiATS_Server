# Traincrew_MultiATS_Server

Traincrew運転会用のマルチATSサーバー。

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

### 諸々の環境変数設定
discordのClientId, ClientSecret を設定する。
値は他の開発者に聞いてください
```
dotnet user-secrets set Discord:ClientId 値は聞いてね 
dotnet user-secrets set Discord:ClientSecret 値は聞いてね 
```

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
## デプロイ

### 最初だけ
- 環境変数
  - Database
  - discordClientId
  - discordClientSecret
  - discordGuildIdなどなど
    - できればDocker上でappsettings.jsonに置いときたい
- RedirectUriを設定する
- 本番用の証明書作成

### 毎回やること

## フォルダ構成について