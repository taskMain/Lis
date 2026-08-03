import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const path = "E:/MedSync/MedicalRecognition/docs/参考资料/甘肃文件/甘肃省检查检验结果互认项目及系统对码表.xlsx";
const input = await FileBlob.load(path);
const workbook = await SpreadsheetFile.importXlsx(input);

const summary = await workbook.inspect({
  kind: "workbook,sheet,table",
  maxChars: 12000,
  tableMaxRows: 12,
  tableMaxCols: 20,
  tableMaxCellChars: 120,
});
console.log("=== SUMMARY ===");
console.log(summary.ndjson);

const matches = await workbook.inspect({
  kind: "match",
  searchTerm: "价格|单价|金额|费用|收费|医保|葡萄糖|乙型肝炎表面抗原|钾（K）",
  options: { useRegex: true, maxResults: 500 },
  maxChars: 16000,
});
console.log("=== MATCHES ===");
console.log(matches.ndjson);

for (const range of ["D1:E16", "D97:E99"]) {
  const region = await workbook.inspect({
    kind: "region",
    sheetId: "Sheet1",
    range,
    maxChars: 4000,
  });
  console.log(`=== ${range} ===`);
  console.log(region.ndjson);
}
