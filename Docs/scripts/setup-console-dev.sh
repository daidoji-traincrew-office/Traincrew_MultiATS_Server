#!/usr/bin/env bash
#
# 連動盤クライアント(TatehamaInterlockingConsole)をローカルで動かすための下準備。
# 「連動盤クライアントのてこチャタリング_引き継ぎ.md」の検証手順から呼ばれる。
#
# やること:
#   1. クライアントリポジトリを clone (既にあれば skip)
#   2. gitignore されていてリポジトリに入っていない ServerAddress.cs を生成する
#      (これが無いとビルドが通らない。初回の詰まりどころ)
#
# ビルド・実行は WPF (net8.0-windows) のため Windows 環境が必要。
#
set -euo pipefail

REPO_URL="https://github.com/daidoji-traincrew-office/TatehamaInterlockingConsole.git"
DEST="${1:-./TatehamaInterlockingConsole}"
SERVER_ADDRESS="${SERVER_ADDRESS:-http://localhost:5000}"

if [ -e "$DEST" ]; then
    echo "==> $DEST は既に存在するので clone を skip します"
else
    echo "==> clone: $REPO_URL -> $DEST"
    git clone "$REPO_URL" "$DEST"
fi

# CI (.github/workflows/build.yml) と同じ形式で生成する。
# namespace は同ワークフローの env.NAMESPACE と揃えること。
ADDRESS_FILE="$DEST/TatehamaInterlockingConsole/ServerAddress.cs"
if [ -e "$ADDRESS_FILE" ]; then
    echo "==> $ADDRESS_FILE は既に存在するので上書きしません"
else
    cat > "$ADDRESS_FILE" <<EOF
namespace TatehamaInterlockingConsole;

/// <summary>
/// 接続先設定。gitignore されているためリポジトリには含まれない。
/// CI では .github/workflows/build.yml が Secrets から生成する。
/// </summary>
public static class ServerAddress
{
    public const string SignalAddress = "$SERVER_ADDRESS";
    public static bool IsDebug = true;
}
EOF
    echo "==> 生成: $ADDRESS_FILE (SignalAddress=$SERVER_ADDRESS, IsDebug=true)"
fi

# 注意: クライアントの .gitignore が無視しているのは Manager/ と Properties/ 配下の
# ServerAddress.cs だけで、CI と同じ置き場所である上記パスは無視されない。
# そのまま放置すると untracked として現れ、接続先ごと誤ってコミットされうる。
# 追跡対象の .gitignore は触らず、ローカル除外に入れておく。
EXCLUDE_FILE="$DEST/.git/info/exclude"
EXCLUDE_ENTRY="TatehamaInterlockingConsole/ServerAddress.cs"
if [ -f "$EXCLUDE_FILE" ] && ! grep -qxF "$EXCLUDE_ENTRY" "$EXCLUDE_FILE"; then
    printf '%s\n' "$EXCLUDE_ENTRY" >> "$EXCLUDE_FILE"
    echo "==> ローカル除外に追加: $EXCLUDE_ENTRY ($EXCLUDE_FILE)"
fi

cat <<EOF

次の手順:

  1. サーバーを起動する (このリポジトリで)
       cd Database && docker compose up -d
       cd .. && dotnet run --project Traincrew_MultiATS_Server.Crew

  2. Windows 環境で連動盤をビルド・起動する
       dotnet run --project $DEST/TatehamaInterlockingConsole

  3. てこを繰り返し倒し、新位置 -> 旧位置 -> 新位置 の巻き戻りが出ないことを確認する

接続先を変える場合は SERVER_ADDRESS を指定して再実行するか、
$ADDRESS_FILE を直接編集してください。

  SERVER_ADDRESS=https://example.invalid $0 $DEST

EOF
