import json, sys
S = r"C:\Users\ynisi\AppData\Local\Temp\claude\C--repos\06669e18-efe0-4909-aeb5-daf89690e383\scratchpad"
old = json.load(open(S + r"\out\run1.json", encoding="utf-8"))        # generic form, v2 deliverable
new = json.load(open(S + r"\out\run_biz.json", encoding="utf-8"))     # business form
# generic-form column -> business-form column
def rename(c):
    c = c.replace("TXN.", "取引.").replace("PAY.", "決済.").replace("APP.", "受付.").replace("PAIRED.", "照合結果.")
    c = c.replace("識別番号照合用", "識別番号").replace("受付番号照合用", "受付番号（照合用）")
    return c
oc, orows = old["columns"], old["rows"]
nc, nrows = new["columns"], new["rows"]
print("old columns:", len(oc), "new columns:", len(nc))
mapped = [rename(c) for c in oc]
missing = [m for m in mapped if m not in nc]
extra = [c for c in nc if c not in mapped]
print("old columns missing in new:", missing)
print("new columns not in old:", extra)
def key(cols, row):
    return (row[cols.index("識別番号" if False else cols[0])],)
oid = oc.index("PAY.識別番号照合用"); nid = nc.index("決済.識別番号")
oby = {r[oid]: r for r in orows}; nby = {r[nid]: r for r in nrows}
print("rows old/new:", len(orows), len(nrows), "same ids:", set(oby) == set(nby))
diff = 0
for i, c in enumerate(oc):
    m = rename(c)
    if m not in nc: continue
    j = nc.index(m)
    for k in oby:
        if oby[k][i] != nby[k][j]:
            diff += 1
            if diff <= 5: print("DIFF", k, c, repr(oby[k][i])[:60], "->", repr(nby[k][j])[:60])
print("cell differences over mapped columns:", diff)
print("new states:", set(new["states"]), "summary:", new["summary"], "joins:", new["joins"])
print("new notes:", new["notes"], "warnings:", new["warnings"][:3])
