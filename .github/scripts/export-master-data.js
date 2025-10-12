const { google } = require('googleapis');
const fs = require('fs').promises;
const path = require('path');

// スプレッドシートのID
const RENDO_SPREADSHEET_ID = '1Br5HldG5nWi2xsqZ-cxb6lG6t_DNzgrG_V2zOsp7mB0';
const MASTER_SPREADSHEET_ID = '1f6YxRuIrzWeSU33uqfcVRsVd_kG-pp02Lp0sHxPd9w0';

// データディレクトリ
const DATA_DIR = path.join(process.cwd(), 'Traincrew_MultiATS_Server.Crew', 'Data');
const RENDO_DIR = path.join(DATA_DIR, 'RendoTable');

// 連動図表の設定
const RENDO_RANGES = [
  ['館浜', 'TH76', ['C32:S50']],
  ['駒野', 'TH75', ['C28:S57', 'C8:S35']],
  ['津崎', 'TH71', ['C26:S41']],
  ['浜園', 'TH70', ['C22:S33']],
  ['新野崎', 'TH67', ['C28:S57']],
  ['江ノ原', 'TH66S', ['', 'C6:S53', 'C7:S57', 'C7:S18']],
  ['大道寺', 'TH65', ['C32:S56', 'C7:S56', 'C6:S58']],
  ['藤江', 'TH64', ['C34:S57']],
  ['水越', 'TH63', ['C30:S53']],
  ['高見沢', 'TH62', ['C34:S55']],
  ['日野森', 'TH61', ['C34:S49']],
  ['西赤山', 'TH59', ['C34:S46']],
  ['赤山町', 'TH58', ['C34:S55', 'C8:S18']]
];

// サーバーマスタの設定
const MASTER_RANGES = [
  ['駅・停車場', '駅・停車場', ['A1:D80']],
  ['進路', '進路', ['A1:L338']],
  ['信号', '信号リスト', ['A1:X365']],
  ['軌道回路', '軌道回路に対する計算するべき信号機リスト', ['A1:X371']],
  ['総括制御ペア一覧', '総括制御ペア一覧', ['A1:D68']],
  ['運転告知器', '運転告知器', ['A1:H63']],
  ['TTC列番窓', 'TTC列番窓', ['A1:I188']],
  ['TTC列番窓リンク設定', 'TTC列番窓リンク設定', ['A1:J415']],
  ['種別設定', '種別', ['A1:B1', 'A3:B25']],
  ['列車設定[通常]', '列車', ['A1:E313']]
];

const MASTER_RANGES_NO_REMOVE_FIRST_EMPTY = [
  ['信号何灯式リスト', '信号何灯式リスト', ['A1:F17']]
];

/**
 * Google Sheets APIクライアントを初期化
 */
async function getAuthClient() {
  const { GoogleAuth } = require('google-auth-library');

  const authOptions = {
    scopes: ['https://www.googleapis.com/auth/spreadsheets.readonly']
  };

  // GOOGLE_APPLICATION_CREDENTIALSが設定されている場合は明示的に指定
  if (process.env.GOOGLE_APPLICATION_CREDENTIALS) {
    authOptions.keyFile = process.env.GOOGLE_APPLICATION_CREDENTIALS;
  }

  const auth = new GoogleAuth(authOptions);
  return await auth.getClient();
}

/**
 * スプレッドシートからデータを取得
 */
async function getSpreadsheetData(sheets, spreadsheetId, sheetName, range) {
  const response = await sheets.spreadsheets.values.get({
    spreadsheetId,
    range: `${sheetName}!${range}`
  });
  return response.data.values || [];
}

/**
 * データをCSV形式に変換
 */
function convertToCSV(data, removeFirstEmpty, removeEmptyColumn) {
  // 空の行を除外
  const filteredRows = removeFirstEmpty
    ? data.filter(row => row[0] !== '' && row[0] !== 'なし')
    : data.filter(row => row.some(cell => cell !== ''));

  if (filteredRows.length === 0) {
    return '';
  }

  // 空の列を特定
  const columnCount = filteredRows[0]?.length || 0;
  const nonEmptyColumns = [];
  for (let col = 0; col < columnCount; col++) {
    if (!removeEmptyColumn || filteredRows.some(row => row[col] !== '' && row[col] !== undefined)) {
      nonEmptyColumns.push(col);
    }
  }

  // 空の列を除外
  const filteredData = filteredRows.map(row =>
    nonEmptyColumns.map(col => row[col] || '')
  );

  // CSV形式に変換
  return filteredData
    .map(row =>
      row.map(cell => String(cell).replace(/\n/g, '')).join(',')
    )
    .join('\r\n');
}

/**
 * スプレッドシートからCSVをエクスポート
 */
async function exportOneDataToCSV(
  sheets,
  spreadsheetId,
  ranges,
  removeFirstEmpty,
  removeEmptyColumn,
  outputDir
) {
  for (const [stationName, stationId, rangeList] of ranges) {
    let allData = [];

    // 各範囲をループしてデータを取得
    for (let index = 0; index < rangeList.length; index++) {
      const range = rangeList[index];
      if (!range) {
        continue;
      }

      const sheetName = rangeList.length === 1
        ? stationName
        : `${stationName}${index + 1}`;

      try {
        const values = await getSpreadsheetData(sheets, spreadsheetId, sheetName, range);
        allData = allData.concat(values);
      } catch (error) {
        console.error(`シートが見つかりません: ${sheetName}`, error.message);
        continue;
      }
    }

    // CSV形式に変換
    const csvContent = convertToCSV(allData, removeFirstEmpty, removeEmptyColumn);

    // ファイルに保存
    const fileName = `${stationId}.csv`;
    const filePath = path.join(outputDir, fileName);
    await fs.writeFile(filePath, csvContent, 'utf8');

    console.log(`CSVファイルが作成されました: ${fileName}`);
  }
}

/**
 * メイン処理
 */
async function main() {
  try {
    // 認証クライアントを取得
    const authClient = await getAuthClient();
    const sheets = google.sheets({ version: 'v4', auth: authClient });

    // ディレクトリを作成
    await fs.mkdir(DATA_DIR, { recursive: true });
    await fs.mkdir(RENDO_DIR, { recursive: true });

    console.log('連動図表のエクスポートを開始します...');
    await exportOneDataToCSV(
      sheets,
      RENDO_SPREADSHEET_ID,
      RENDO_RANGES,
      false,
      true,
      RENDO_DIR
    );

    console.log('サーバーマスタのエクスポートを開始します...');
    await exportOneDataToCSV(
      sheets,
      MASTER_SPREADSHEET_ID,
      MASTER_RANGES,
      true,
      false,
      DATA_DIR
    );

    console.log('信号何灯式リストのエクスポートを開始します...');
    await exportOneDataToCSV(
      sheets,
      MASTER_SPREADSHEET_ID,
      MASTER_RANGES_NO_REMOVE_FIRST_EMPTY,
      false,
      false,
      DATA_DIR
    );

    console.log('エクスポートが完了しました。');
  } catch (error) {
    console.error('エラーが発生しました:', error);
    process.exit(1);
  }
}

main();
