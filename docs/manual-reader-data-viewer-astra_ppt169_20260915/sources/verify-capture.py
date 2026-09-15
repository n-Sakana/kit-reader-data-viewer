"""Read saved demonstration artifacts independently of the running application."""
import csv, hashlib, json, pathlib, warnings
from openpyxl import load_workbook

ROOT = pathlib.Path(__file__).resolve().parent
EVIDENCE = ROOT / 'capture-evidence'
warnings.filterwarnings('ignore', message='Workbook contains no default style')

def ledger(name):
    book = load_workbook(EVIDENCE / name, read_only=True, data_only=True)
    rows = list(book.active.values)
    assert rows[0][0] == '処理状態'
    headers = list(rows[0])
    records = [dict(zip(headers, row)) for row in rows[1:]]
    book.close()
    return records

sent = ledger('sent-ledger.xlsx')
deleted = ledger('deleted-ledger.xlsx')
with (EVIDENCE / 'sample-paid-80.csv').open(encoding='utf-8-sig', newline='') as f:
    exported = list(csv.reader(f))
assert len(sent) == 100
assert sum(r['処理状態'] == 'TRUE' for r in sent) == 1
assert sum(r['決済確認済'] == '済' for r in sent) == 80
assert sum(not r['申請番号'] for r in sent) == 20
assert len(deleted) == 99
assert all(r['会員番号照合用'] != 'AA10000000AA' for r in deleted)
assert sum(r['処理状態'] == 'TRUE' for r in deleted) == 0
assert len(exported) - 1 == 80
assert len(exported[0]) == 11
candidate = json.loads((EVIDENCE / 'candidate-observations.json').read_text())
assert '該当 2 件' in candidate['before']
assert '未送信 0 件' in candidate['after']
state = json.loads((EVIDENCE / 'state-observations.json').read_text())
zero_delete = next(row for row in state if row['name'] == '11-unsent-delete-zero')
assert '台帳件数 100' in zero_delete['text']
assert '未送信 1 件' in zero_delete['text']
report = {
    'baseline_commit': 'adcba63eae989a58d0ee7db411249a065882c181',
    'sent_rows': len(sent), 'sent_true': 1, 'paid_rows': 80, 'missing_third': 20,
    'unsent_delete_rows': 100, 'unsent_after_delete': 1,
    'deleted_rows': len(deleted), 'deleted_key_absent': 'AA10000000AA',
    'csv_rows': len(exported)-1, 'csv_columns': len(exported[0]),
    'candidate_rows': 2, 'manual_candidate_pending_after': 0,
    'checks': 'passed',
    'files': {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(EVIDENCE.iterdir()) if p.is_file()}
}
(ROOT / 'capture-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='files'}, ensure_ascii=False, indent=2))
