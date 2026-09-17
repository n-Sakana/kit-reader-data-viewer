import io, sys
p = r"C:\repos\kit\reader-data-viewer\v2用_20260918-0531\fixed\決済確認アプリ【試作版】\src\Rdv3Business.cs"
s = open(p, encoding="utf-8").read()

def rep(old, new):
    global s
    assert old in s, old[:80]
    s = s.replace(old, new)

# 1. types only for raw CSV columns (a cut-out column is typed by its display format alone)
rep('''        foreach (KeyValuePair<string, string> date in m.DateFormats)
        {
            if (comma) { sb.Append(','); }''',
'''        foreach (KeyValuePair<string, string> date in m.DateFormats)
        {
            // a cut-out column has no file to check; its notation is only a display format
            bool derived = false;
            foreach (Extract e in m.Extracts) { if (e.Name == date.Key) { derived = true; break; } }
            if (derived) { continue; }
            if (comma) { sb.Append(','); }''')

# 2. labels for every name the jobs make up: build the jobs first, then the labels
rep('''        string[][] fixedLabels =
        {
            new string[] { m.JudgeRef, LabelOf(m.JudgeRef) },
            new string[] { Rdv3Text.BizJoined, Rdv3Text.BizJoined },
            new string[] { Rdv3Text.BizPaidRows, Rdv3Text.BizPaidRows },
            new string[] { "ledger", Rdv3Text.BizLedgerLabel },
            new string[] { Rdv3Text.BizProcessedRows, Rdv3Text.BizProcessedRows },
            new string[] { Rdv3Text.BizTargetRows, Rdv3Text.BizTargetRows },
            new string[] { "$work", Rdv3Text.BizWorkLabel }
        };
        foreach (string[] pair in fixedLabels)
        {
            if (labelled.Contains(pair[0])) { continue; }
            labelled.Add(pair[0]);
            sb.Append(',').Append(Q(pair[0])).Append(':').Append(Q(pair[1]));
        }
        for (int i = 0; i < m.Conditions.Count; i++)
        {
            string name = Rdv3Text.BizCondPrefix + (i + 1).ToString(CultureInfo.InvariantCulture);
            sb.Append(',').Append(Q(name)).Append(':').Append(Q(name));
        }
        int combos = 1;
        foreach (List<Ref> alternatives in m.DeleteLedgerSides) { combos *= alternatives.Count; }
        for (int i = 0; i < combos; i++)
        {
            string name = Rdv3Text.BizMatchedPrefix + (i + 1).ToString(CultureInfo.InvariantCulture);
            sb.Append(',').Append(Q(name)).Append(':').Append(Q(name));
        }
        if (combos > 1) { sb.Append(',').Append(Q(Rdv3Text.BizMatchedPrefix)).Append(':').Append(Q(Rdv3Text.BizMatchedPrefix)); }
        sb.Append("},\\"jobs\\":[").Append(DeleteJob(m, combos)).Append(',').Append(UpdateJob(m)).Append(']');''',
'''        List<string> made = new List<string>();
        string deleteJob = DeleteJob(m, made);
        string updateJob = UpdateJob(m, made);
        string[][] fixedLabels =
        {
            new string[] { m.JudgeRef, LabelOf(m.JudgeRef) },
            new string[] { "ledger", Rdv3Text.BizLedgerLabel },
            new string[] { "$work", Rdv3Text.BizWorkLabel }
        };
        foreach (string[] pair in fixedLabels)
        {
            if (labelled.Contains(pair[0])) { continue; }
            labelled.Add(pair[0]);
            sb.Append(',').Append(Q(pair[0])).Append(':').Append(Q(pair[1]));
        }
        foreach (string name in made)
        {
            if (labelled.Contains(name)) { continue; }
            labelled.Add(name);
            sb.Append(',').Append(Q(name)).Append(':').Append(Q(name));
        }
        sb.Append("},\\"jobs\\":[").Append(deleteJob).Append(',').Append(updateJob).Append(']');''')

rep('    private static string UpdateJob(Model m)\n    {',
    '    private static string UpdateJob(Model m, List<string> made)\n    {\n        made.Add(Rdv3Text.BizJoined);')

rep('''            string name = Rdv3Text.BizCondPrefix + (i + 1).ToString(CultureInfo.InvariantCulture);
            steps.Add("{\\"operation\\":\\"extract\\",\\"target1\\":" + Q(joined) + ",\\"where\\":{\\"column\\":" + Q(m.Conditions[i][0].Text)
                + ",\\"operator\\":\\"equals\\",\\"value\\":" + Q(m.ConditionValues[i]) + "},\\"output\\":" + Q(name) + "}");
            if (i == 0) { rows = name; continue; }
            string both = (i == m.Conditions.Count - 1) ? Rdv3Text.BizPaidRows : name + "&";
            steps.Add(''',
'''            string name = Rdv3Text.BizCondPrefix + (i + 1).ToString(CultureInfo.InvariantCulture);
            made.Add(name);
            steps.Add("{\\"operation\\":\\"extract\\",\\"target1\\":" + Q(joined) + ",\\"where\\":{\\"column\\":" + Q(m.Conditions[i][0].Text)
                + ",\\"operator\\":\\"equals\\",\\"value\\":" + Q(m.ConditionValues[i]) + "},\\"output\\":" + Q(name) + "}");
            if (i == 0) { rows = name; continue; }
            string both = (i == m.Conditions.Count - 1) ? Rdv3Text.BizPaidRows : Rdv3Text.BizCondPrefix + "1-" + (i + 1).ToString(CultureInfo.InvariantCulture);
            made.Add(both);
            steps.Add(''')

rep('''        if (m.Conditions.Count == 1)
        {
            steps.Add(''',
'''        if (m.Conditions.Count == 1)
        {
            made.Add(Rdv3Text.BizPaidRows);
            steps.Add(''')

rep('    private static string DeleteJob(Model m, int combos)\n    {',
    '    private static string DeleteJob(Model m, List<string> made)\n    {\n        int combos = 1;\n        foreach (List<Ref> alternatives in m.DeleteLedgerSides) { combos *= alternatives.Count; }\n        made.Add(Rdv3Text.BizProcessedRows);\n        made.Add(Rdv3Text.BizTargetRows);')

rep('''            string name = Rdv3Text.BizMatchedPrefix + (c + 1).ToString(CultureInfo.InvariantCulture);
            steps.Add(''',
'''            string name = Rdv3Text.BizMatchedPrefix + (c + 1).ToString(CultureInfo.InvariantCulture);
            made.Add(name);
            steps.Add(''')

rep('''            string name = (i == matched.Count - 1) ? Rdv3Text.BizMatchedPrefix : matched[i] + "|";
            steps.Add(''',
'''            string name = (i == matched.Count - 1) ? Rdv3Text.BizMatchedPrefix : Rdv3Text.BizMatchedPrefix + "1-" + (i + 1).ToString(CultureInfo.InvariantCulture);
            made.Add(name);
            steps.Add(''')

open(p, "w", encoding="utf-8", newline="\n").write(s)
print("business patched")
