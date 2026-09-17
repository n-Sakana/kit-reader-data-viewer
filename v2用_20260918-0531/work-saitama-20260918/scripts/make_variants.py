import csv, json, os, shutil, io, sys
S = r"C:\Users\ynisi\AppData\Local\Temp\claude\C--repos\06669e18-efe0-4909-aeb5-daf89690e383\scratchpad"
SRC = r"C:\repos\kit\reader-data-viewer\v2用_20260918-0531\dummy_out"
APP = r"C:\repos\kit\reader-data-viewer\v2用_20260918-0531\fixed\決済確認アプリ【試作版】"

def load(name):
    with open(os.path.join(SRC, name), encoding="utf-8-sig", newline="") as f:
        return list(csv.reader(f))

def save(path, rows, encoding="utf-8-sig", delimiter=",", newline="\r\n"):
    with open(path, "w", encoding=encoding, newline="") as f:
        w = csv.writer(f, delimiter=delimiter, lineterminator=newline, quoting=csv.QUOTE_MINIMAL)
        w.writerows(rows)

def fresh(name):
    d = os.path.join(S, "variants", name)
    if os.path.isdir(d): shutil.rmtree(d)
    os.makedirs(d)
    return d

r1, r2, r3, r4 = load("01_取引データ.csv"), load("02_決済データ.csv"), load("03_受付データ.csv"), load("04_削除用_手続完了.csv")

# --- report on run1 ------------------------------------------------------
rep = json.load(open(os.path.join(S, "out", "run1.json"), encoding="utf-8"))
cols = rep["columns"]; rows = rep["rows"]
print("run1 columns:", cols)
print("run1 row0:", dict(zip(cols, rows[0])))
ix = cols.index("PAIRED.決済確認済")
print("run1 決済確認済 counts:", {v: sum(1 for r in rows if r[ix] == v) for v in set(r[ix] for r in rows)})
print("run1 notes:", rep.get("notes"), "warnings:", rep.get("warnings"))

# --- variant A: some 02 rows 決済済 -------------------------------------------
dA = fresh("A_paid")
h2 = r2[0]; st = h2.index("決済ステータス")
r2a = [list(r) for r in r2]
paid_ids = []
for i in (1, 2, 3, 4, 5):
    r2a[i][st] = "決済済"; paid_ids.append(r2a[i][h2.index("オーダーID")])
r2a[6][st] = "決済済 "      # trailing space: still paid (padding absorbed)
paid_ids.append(r2a[6][h2.index("オーダーID")])
save(os.path.join(dA, "01_取引データ.csv"), r1)
save(os.path.join(dA, "02_決済データ.csv"), r2a)
save(os.path.join(dA, "03_受付データ.csv"), r3)
save(os.path.join(dA, "04_削除用_手続完了.csv"), r4)
json.dump(paid_ids, open(os.path.join(S, "variants", "A_paid_ids.json"), "w", encoding="utf-8"), ensure_ascii=False)

# --- variant B: format tolerance --------------------------------------------
dB = fresh("B_tolerant")
# 01: shift_jis without BOM, header cells with surrounding spaces, file name with a date suffix
r1b = [[" " + c + " " for c in r1[0]]] + [list(r) for r in r1[1:]]
save(os.path.join(dB, "01_取引データ_20260918.csv"), r1b, encoding="cp932")
# 02: tab separated, utf-8 without BOM, full-width digits/letters in a few order ids and 備考2
r2b = [list(r) for r in r2]
oid = r2[0].index("オーダーID"); b2 = r2[0].index("備考2")
def fw(s):
    return "".join(chr(ord(c) + 0xFEE0) if ("0" <= c <= "9" or "A" <= c <= "Z") else ("－" if c == "-" else c) for c in s)
for i in (1, 2, 3):
    r2b[i][oid] = fw(r2b[i][oid])
    r2b[i][b2] = fw(r2b[i][b2].split("/")[0]) + r2b[i][b2][len(r2b[i][b2].split("/")[0]):]
save(os.path.join(dB, "02_決済データ_0918.csv"), r2b, encoding="utf-8", delimiter="\t")
# 03: header 受付時_識別番号 with half-width underscore and lower-case-ish spacing, 申込日 as yyyy/M/d, id with trailing spaces
r3b = [list(r) for r in r3]
h3 = r3b[0]
h3[h3.index("受付時＿識別番号")] = "受付時_識別番号"
h3[h3.index("申込日")] = "申込日 "
d3 = r3[0].index("申込日"); i3 = r3[0].index("受付時＿識別番号")
for i in range(1, len(r3b)):
    v = r3b[i][d3]
    if i % 2 == 0 and len(v) == 8: r3b[i][d3] = "%s/%d/%d" % (v[:4], int(v[4:6]), int(v[6:]))
    if i <= 3: r3b[i][i3] = "  " + r3b[i][i3] + "  "
save(os.path.join(dB, "03_受付データ_x.csv"), r3b)
save(os.path.join(dB, "04_削除用_手続完了.csv"), r4)
# settings copy with prefix matching for the three tables
cfg = json.load(open(os.path.join(APP, "settings.json"), encoding="utf-8"))
for t in ("TXN", "PAY", "APP"):
    cfg["data"]["tables"][t]["fileMatch"] = "prefix"
json.dump(cfg, open(os.path.join(S, "variants", "B_settings.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=2)

# --- variant C: a broken 02 (key column renamed) -----------------------------
dC = fresh("C_broken")
r2c = [list(r) for r in r2]; r2c[0][oid] = "注文番号"
save(os.path.join(dC, "01_取引データ.csv"), r1)
save(os.path.join(dC, "02_決済データ.csv"), r2c)
save(os.path.join(dC, "03_受付データ.csv"), r3)
save(os.path.join(dC, "04_削除用_手続完了.csv"), r4)

# --- variant D: old-format shift_jis sample from the original zip -----------
dD = fresh("D_oldsample")
old = os.path.join(r"C:\repos\kit\reader-data-viewer\v2用_20260918-0531\fixed\_original_backup_20260918", "data")
for n in os.listdir(old): shutil.copy(os.path.join(old, n), dD)
print("variants ready:", dA, dB, dC, dD)
