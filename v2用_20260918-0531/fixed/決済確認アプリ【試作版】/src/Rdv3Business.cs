// ============================================================================
// Rdv3Business.cs -- the business block at the top of settings.json.
//
// The block names, in the operator's words, the input files, the columns
// cut out of them, how the tables are joined, what makes a row paid, what
// identifies a ledger row, what is searched, and what each frame of the
// fixed screen shows. On load it is expanded into the generic definition
// (data.tables / jobs / ledger and screen) the engine already runs; nothing
// below this layer changes. Keys are never guessed: a column is joined,
// searched or shown only where the block says so, and a name that does not
// line up is reported in the block's own words.
//
// C# 5 only, no verbatim strings. Member names and messages live in Rdv3Text.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class Rdv3Business
{
    // set while a definition made from the block is in use, so messages
    // that mention generated paths can be rewritten into the block's words
    public static bool Enabled;

    private static readonly string[] UserSlots = { "userId", "userName", "userCategory" };
    private static readonly string[] AppSlots = { "applicationDate", "applicationNumber", "cardNumber", "applicantName", "birthDate", "qualification", "office" };
    private static readonly string[] CandidateSlots = { "applicationNumber", "cardNumber", "applicant", "applicationDate" };

    private sealed class Ref
    {
        public string Text, Table, Column;
    }

    private sealed class Extract
    {
        public string Name, Table, Column, Expression;
        public Rdv3Json Node;
    }

    private sealed class Join
    {
        public Ref Left, Right;
        public string Keep;
    }

    private sealed class ScreenRow
    {
        public string Slot, Label, Field, DateFrom, DateTo;
        public bool Hidden;
    }

    private sealed class Model
    {
        public readonly List<string> Tables = new List<string>();
        public readonly Dictionary<string, string> Files = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly Dictionary<string, string> Matches = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly Dictionary<string, string[]> Keys = new Dictionary<string, string[]>(StringComparer.Ordinal);
        public readonly List<Extract> Extracts = new List<Extract>();
        public readonly List<Join> Joins = new List<Join>();
        public readonly List<string> JoinedTables = new List<string>();
        public readonly List<Ref[]> Conditions = new List<Ref[]>();     // [column, value-as-ref-text]
        public readonly List<string> ConditionValues = new List<string>();
        public readonly List<Ref> Identity = new List<Ref>();
        public readonly List<Ref> Search = new List<Ref>();
        public string DeleteTable = "";
        public readonly List<List<Ref>> DeleteLedgerSides = new List<List<Ref>>();
        public readonly List<Ref> DeleteSides = new List<Ref>();
        public readonly List<ScreenRow> Rows = new List<ScreenRow>();
        public readonly List<ScreenRow> Candidates = new List<ScreenRow>();
        public string SearchLabel = "", UserBox = "", AppBox = "", JudgeLabel = "", PaidText = "", UnpaidText = "";
        public readonly List<string> ExportDefaults = new List<string>();
        public readonly Dictionary<string, string> DateFormats = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly List<string> AllRefs = new List<string>();
        public string JudgeRef { get { return Rdv3Text.BizJoined + "." + Rdv3Text.BizJudgeColumn; } }
    }

    // ---- reading the block -----------------------------------------------------
    public static Rdv3Json Expand(Rdv3Json block)
    {
        Model m = new Model();
        block.Only(Rdv3Text.BizFiles, Rdv3Text.BizExtract, Rdv3Text.BizJoins, Rdv3Text.BizPaid, Rdv3Text.BizIdentity,
                   Rdv3Text.BizSearch, Rdv3Text.BizDelete, Rdv3Text.BizScreen);
        ReadFiles(m, block.Obj(Rdv3Text.BizFiles, true));
        ReadExtracts(m, block.Obj(Rdv3Text.BizExtract, false));
        ReadJoins(m, block.Member(Rdv3Text.BizJoins), block);
        ReadConditions(m, block.Member(Rdv3Text.BizPaid), block);
        ReadRefList(m, block.Member(Rdv3Text.BizIdentity), block, Rdv3Text.BizIdentity, m.Identity, Rdv3Text.BizNoIdentity);
        ReadRefList(m, block.Member(Rdv3Text.BizSearch), block, Rdv3Text.BizSearch, m.Search, Rdv3Text.BizNoSearch);
        ReadDelete(m, block.Obj(Rdv3Text.BizDelete, true));
        ReadScreen(m, block.Obj(Rdv3Text.BizScreen, true));
        foreach (Extract e in m.Extracts)
        {
            if (!m.JoinedTables.Contains(e.Table) && e.Table != m.DeleteTable)
            { throw e.Node.Fail(Rdv3Text.BizExtractUnusedTable.Replace("{name}", e.Name).Replace("{table}", e.Table)); }
        }
        string text = "{\"data\":" + DataJson(m) + ",\"screen\":" + ScreenJson(m) + "}";
        Rdv3Json generated = Rdv3Json.Parse(text);
        Enabled = true;
        return generated;
    }

    private static void ReadFiles(Model m, Rdv3Json files)
    {
        if (files.Order.Count == 0) { throw files.Fail(Rdv3Text.BizNoFiles); }
        foreach (string table in files.Order)
        {
            Rdv3Json entry = files.Member(table);
            if (table.Trim().Length == 0 || table.IndexOf('.') >= 0 || table.IndexOf(' ') >= 0 || table == "ledger")
            { throw entry.Fail(Rdv3Text.BizTableIdForm.Replace("{table}", table)); }
            if (entry.Kind != Rdv3Json.TObject) { throw entry.Fail("must be an object"); }
            entry.Only(Rdv3Text.BizFileName, Rdv3Text.BizFileMatch, Rdv3Text.BizKey);
            string file = entry.Need(Rdv3Text.BizFileName).Trim();
            string match = entry.StrOr(Rdv3Text.BizFileMatch, Rdv3Text.BizMatchPrefix).Trim();
            if (match != Rdv3Text.BizMatchExact && match != Rdv3Text.BizMatchPrefix)
            { throw entry.Member(Rdv3Text.BizFileMatch).Fail(Rdv3Text.BizMatchWord.Replace("{exact}", Rdv3Text.BizMatchExact).Replace("{prefix}", Rdv3Text.BizMatchPrefix).Replace("{value}", match)); }
            Rdv3Json keyNode = entry.Member(Rdv3Text.BizKey);
            if (keyNode == null) { throw entry.Fail(Rdv3Text.BizKey + " is required"); }
            string[] keys = Names(keyNode);
            m.Tables.Add(table);
            m.Files[table] = file;
            m.Matches[table] = match == Rdv3Text.BizMatchPrefix ? "prefix" : "exact";
            m.Keys[table] = keys;
            foreach (string k in keys) { Remember(m, table + "." + k); }
        }
    }

    private static string[] Names(Rdv3Json node)
    {
        List<string> names = new List<string>();
        if (node.Kind == Rdv3Json.TString) { names.Add(node.Str.Trim()); }
        else if (node.Kind == Rdv3Json.TArray)
        { for (int i = 0; i < node.Count; i++) { if (node.At(i).Kind != Rdv3Json.TString) { throw node.At(i).Fail("must be a column name"); } names.Add(node.At(i).Str.Trim()); } }
        else { throw node.Fail("specify a column name or an array of column names"); }
        if (names.Count == 0 || names.Contains("")) { throw node.Fail("must name at least one non-blank column"); }
        return names.ToArray();
    }

    private static Ref ParseRef(Model m, string text, Rdv3Json at, string where)
    {
        string value = (text == null) ? "" : text.Trim();
        int dot = value.IndexOf('.');
        if (dot <= 0 || dot == value.Length - 1) { throw at.Fail(Rdv3Text.BizRefForm.Replace("{ref}", value)); }
        Ref r = new Ref();
        r.Text = value;
        r.Table = value.Substring(0, dot);
        r.Column = value.Substring(dot + 1);
        if (!m.Tables.Contains(r.Table))
        { throw at.Fail(Rdv3Text.BizUnknownTable.Replace("{table}", r.Table).Replace("{tables}", string.Join(" / ", m.Tables.ToArray()))); }
        Remember(m, value);
        return r;
    }

    private static void Remember(Model m, string reference)
    {
        if (!m.AllRefs.Contains(reference)) { m.AllRefs.Add(reference); }
    }

    private static void ReadExtracts(Model m, Rdv3Json extracts)
    {
        if (extracts == null) { return; }
        foreach (string name in extracts.Order)
        {
            Rdv3Json node = extracts.Member(name);
            if (node.Kind != Rdv3Json.TString || node.Str.Trim().Length == 0) { throw node.Fail("must be an expression text"); }
            int dot = name.IndexOf('.');
            if (dot <= 0 || dot == name.Length - 1 || name.IndexOf('.', dot + 1) >= 0 || HasWhite(name.Substring(dot + 1)))
            { throw node.Fail(Rdv3Text.BizExtractName.Replace("{name}", name)); }
            Extract e = new Extract();
            e.Name = name.Trim();
            e.Table = name.Substring(0, dot).Trim();
            e.Column = name.Substring(dot + 1).Trim();
            e.Expression = node.Str.Trim();
            e.Node = node;
            if (!m.Tables.Contains(e.Table))
            { throw node.Fail(Rdv3Text.BizUnknownTable.Replace("{table}", e.Table).Replace("{tables}", string.Join(" / ", m.Tables.ToArray()))); }
            foreach (Extract other in m.Extracts) { if (other.Name == e.Name) { throw node.Fail(Rdv3Text.BizExtractDuplicate.Replace("{name}", e.Name)); } }
            foreach (string token in ExpressionRefs(e.Expression))
            {
                int td = token.IndexOf('.');
                if (td <= 0) { continue; }
                if (token.Substring(0, td) != e.Table)
                { throw node.Fail(Rdv3Text.BizExtractOtherTable.Replace("{name}", e.Name).Replace("{table}", e.Table).Replace("{ref}", token)); }
                Remember(m, token);
            }
            Remember(m, e.Name);
            m.Extracts.Add(e);
        }
    }

    private static bool HasWhite(string s)
    {
        for (int i = 0; i < s.Length; i++) { if (char.IsWhiteSpace(s[i])) { return true; } }
        return false;
    }

    // the column references an expression names, the way the expression parser cuts it up
    private static List<string> ExpressionRefs(string expression)
    {
        List<string> refs = new List<string>();
        int p = 0;
        while (p < expression.Length)
        {
            char ch = expression[p];
            if (char.IsWhiteSpace(ch) || "+-*/(),".IndexOf(ch) >= 0) { p++; continue; }
            if (ch == '\'')
            {
                p++;
                while (p < expression.Length)
                {
                    if (expression[p++] != '\'') { continue; }
                    if (p < expression.Length && expression[p] == '\'') { p++; continue; }
                    break;
                }
                continue;
            }
            int start = p;
            while (p < expression.Length && !char.IsWhiteSpace(expression[p]) && "+-*/(),".IndexOf(expression[p]) < 0) { p++; }
            string token = expression.Substring(start, p - start);
            int next = p;
            while (next < expression.Length && char.IsWhiteSpace(expression[next])) { next++; }
            if (next < expression.Length && expression[next] == '(') { continue; }
            decimal number;
            if (decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out number)) { continue; }
            refs.Add(token);
        }
        return refs;
    }

    private static void ReadJoins(Model m, Rdv3Json joins, Rdv3Json block)
    {
        if (joins == null || joins.Kind != Rdv3Json.TArray || joins.Count == 0) { throw block.Fail(Rdv3Text.BizNoJoins); }
        for (int i = 0; i < joins.Count; i++)
        {
            Rdv3Json node = joins.At(i);
            if (node.Kind != Rdv3Json.TObject) { throw node.Fail("must be an object"); }
            node.Only(Rdv3Text.BizJoinLeft, Rdv3Text.BizJoinRight, Rdv3Text.BizJoinKeep);
            Join j = new Join();
            j.Left = ParseRef(m, node.Need(Rdv3Text.BizJoinLeft), node.Member(Rdv3Text.BizJoinLeft), Rdv3Text.BizJoins);
            j.Right = ParseRef(m, node.Need(Rdv3Text.BizJoinRight), node.Member(Rdv3Text.BizJoinRight), Rdv3Text.BizJoins);
            string keep = node.StrOr(Rdv3Text.BizJoinKeep, Rdv3Text.BizKeepBoth).Trim();
            if (keep == Rdv3Text.BizKeepBoth) { j.Keep = "match"; }
            else if (keep == Rdv3Text.BizKeepLeft) { j.Keep = "left"; }
            else if (keep == Rdv3Text.BizKeepAll) { j.Keep = "full"; }
            else
            {
                throw node.Fail(Rdv3Text.BizKeepWord.Replace("{both}", Rdv3Text.BizKeepBoth).Replace("{left}", Rdv3Text.BizKeepLeft)
                    .Replace("{all}", Rdv3Text.BizKeepAll).Replace("{value}", keep));
            }
            string n = (i + 1).ToString(CultureInfo.InvariantCulture);
            if (i == 0) { m.JoinedTables.Add(j.Left.Table); }
            else if (!m.JoinedTables.Contains(j.Left.Table))
            { throw node.Member(Rdv3Text.BizJoinLeft).Fail(Rdv3Text.BizJoinLeftNotJoined.Replace("{n}", n).Replace("{ref}", j.Left.Text).Replace("{table}", j.Left.Table)); }
            if (j.Right.Table == j.Left.Table)
            { throw node.Member(Rdv3Text.BizJoinRight).Fail(Rdv3Text.BizJoinRightIsLeft.Replace("{n}", n).Replace("{table}", j.Right.Table)); }
            if (m.JoinedTables.Contains(j.Right.Table))
            { throw node.Member(Rdv3Text.BizJoinRight).Fail(Rdv3Text.BizJoinRightAgain.Replace("{n}", n).Replace("{table}", j.Right.Table)); }
            m.JoinedTables.Add(j.Right.Table);
            m.Joins.Add(j);
        }
    }

    private static void RequireJoined(Model m, Ref r, Rdv3Json at, string where)
    {
        if (m.JoinedTables.Contains(r.Table)) { return; }
        throw at.Fail(Rdv3Text.BizNotJoinedTable.Replace("{where}", where).Replace("{ref}", r.Text).Replace("{table}", r.Table));
    }

    private static void ReadConditions(Model m, Rdv3Json conditions, Rdv3Json block)
    {
        if (conditions == null || conditions.Kind != Rdv3Json.TArray || conditions.Count == 0) { throw block.Fail(Rdv3Text.BizNoConditions); }
        for (int i = 0; i < conditions.Count; i++)
        {
            Rdv3Json node = conditions.At(i);
            if (node.Kind != Rdv3Json.TObject) { throw node.Fail("must be an object"); }
            node.Only(Rdv3Text.BizCondColumn, Rdv3Text.BizCondValue);
            Ref column = ParseRef(m, node.Need(Rdv3Text.BizCondColumn), node.Member(Rdv3Text.BizCondColumn), Rdv3Text.BizPaid);
            RequireJoined(m, column, node.Member(Rdv3Text.BizCondColumn), Rdv3Text.BizPaid);
            string value = node.Need(Rdv3Text.BizCondValue);
            m.Conditions.Add(new Ref[] { column });
            m.ConditionValues.Add(value);
        }
    }

    private static void ReadRefList(Model m, Rdv3Json list, Rdv3Json block, string where, List<Ref> into, string emptyMessage)
    {
        if (list == null) { throw block.Fail(emptyMessage); }
        string[] names = Names(list);
        for (int i = 0; i < names.Length; i++)
        {
            Rdv3Json at = list.Kind == Rdv3Json.TArray ? list.At(i) : list;
            Ref r = ParseRef(m, names[i], at, where);
            RequireJoined(m, r, at, where);
            into.Add(r);
        }
    }

    private static void ReadDelete(Model m, Rdv3Json del)
    {
        del.Only(Rdv3Text.BizDeleteFile, Rdv3Text.BizDeleteKeys);
        string table = del.Need(Rdv3Text.BizDeleteFile).Trim();
        if (!m.Tables.Contains(table)) { throw del.Member(Rdv3Text.BizDeleteFile).Fail(Rdv3Text.BizDeleteFileUnknown.Replace("{table}", table)); }
        if (m.JoinedTables.Contains(table)) { throw del.Member(Rdv3Text.BizDeleteFile).Fail(Rdv3Text.BizDeleteFileJoined.Replace("{table}", table)); }
        m.DeleteTable = table;
        Rdv3Json keys = del.Member(Rdv3Text.BizDeleteKeys);
        if (keys == null || keys.Kind != Rdv3Json.TArray || keys.Count == 0) { throw del.Fail(Rdv3Text.BizDeleteNoKeys); }
        for (int i = 0; i < keys.Count; i++)
        {
            Rdv3Json node = keys.At(i);
            if (node.Kind != Rdv3Json.TObject) { throw node.Fail("must be an object"); }
            node.Only(Rdv3Text.BizDeleteLedger, Rdv3Text.BizDeleteSide);
            Rdv3Json ledgerNode = node.Member(Rdv3Text.BizDeleteLedger);
            if (ledgerNode == null) { throw node.Fail(Rdv3Text.BizDeleteLedger + " is required"); }
            List<Ref> alternatives = new List<Ref>();
            foreach (string name in Names(ledgerNode))
            {
                Ref r = ParseRef(m, name, ledgerNode, Rdv3Text.BizDelete);
                RequireJoined(m, r, ledgerNode, Rdv3Text.BizDelete);
                alternatives.Add(r);
            }
            Ref side = ParseRef(m, node.Need(Rdv3Text.BizDeleteSide), node.Member(Rdv3Text.BizDeleteSide), Rdv3Text.BizDelete);
            if (side.Table != table)
            { throw node.Member(Rdv3Text.BizDeleteSide).Fail(Rdv3Text.BizDeleteSideTable.Replace("{n}", (i + 1).ToString(CultureInfo.InvariantCulture)).Replace("{ref}", side.Text).Replace("{table}", table)); }
            m.DeleteLedgerSides.Add(alternatives);
            m.DeleteSides.Add(side);
        }
    }

    private static void ReadScreen(Model m, Rdv3Json screen)
    {
        screen.Only(Rdv3Text.BizSearchLabel, Rdv3Text.BizUserBox, Rdv3Text.BizAppBox, Rdv3Text.BizRemarks, Rdv3Text.BizPlan,
                    Rdv3Text.BizJudgment, Rdv3Text.BizCandidates, Rdv3Text.BizExportDefaults);
        m.SearchLabel = screen.StrOr(Rdv3Text.BizSearchLabel, "").Trim();
        ReadBox(m, screen.Obj(Rdv3Text.BizUserBox, true), Rdv3Text.BizUserBox, UserSlots, true);
        ReadBox(m, screen.Obj(Rdv3Text.BizAppBox, true), Rdv3Text.BizAppBox, AppSlots, false);
        ReadTextBox(m, screen.Obj(Rdv3Text.BizRemarks, true), "remarks");
        ReadTextBox(m, screen.Obj(Rdv3Text.BizPlan, true), "plan");
        Rdv3Json judge = screen.Obj(Rdv3Text.BizJudgment, true);
        judge.Only(Rdv3Text.BizBoxName, Rdv3Text.BizPaidText, Rdv3Text.BizUnpaidText);
        m.JudgeLabel = judge.StrOr(Rdv3Text.BizBoxName, "").Trim();
        m.PaidText = judge.StrOr(Rdv3Text.BizPaidText, Rdv3Text.BizPaidStored).Trim();
        m.UnpaidText = judge.StrOr(Rdv3Text.BizUnpaidText, "").Trim();
        Rdv3Json candidates = screen.Member(Rdv3Text.BizCandidates);
        if (candidates != null)
        {
            if (candidates.Kind != Rdv3Json.TArray) { throw candidates.Fail("must be an array"); }
            if (candidates.Count > CandidateSlots.Length)
            { throw candidates.Fail(Rdv3Text.BizScreenCandidates.Replace("{max}", CandidateSlots.Length.ToString(CultureInfo.InvariantCulture)).Replace("{n}", candidates.Count.ToString(CultureInfo.InvariantCulture))); }
            for (int i = 0; i < candidates.Count; i++)
            {
                ScreenRow row = ReadRow(m, candidates.At(i), CandidateSlots[i], Rdv3Text.BizHeader);
                m.Candidates.Add(row);
            }
        }
        for (int i = m.Candidates.Count; i < CandidateSlots.Length; i++)
        {
            ScreenRow hidden = new ScreenRow(); hidden.Slot = CandidateSlots[i]; hidden.Hidden = true; m.Candidates.Add(hidden);
        }
        Rdv3Json exports = screen.Member(Rdv3Text.BizExportDefaults);
        if (exports != null)
        {
            foreach (string name in Names(exports))
            {
                if (name == Rdv3Text.BizWorkColumn) { m.ExportDefaults.Add("$work"); continue; }
                Ref r = ParseRef(m, name, exports, Rdv3Text.BizExportDefaults);
                RequireJoined(m, r, exports, Rdv3Text.BizExportDefaults);
                m.ExportDefaults.Add(r.Text);
            }
        }
    }

    private static void ReadBox(Model m, Rdv3Json box, string name, string[] slots, bool user)
    {
        box.Only(Rdv3Text.BizBoxName, Rdv3Text.BizRows);
        string title = box.StrOr(Rdv3Text.BizBoxName, "").Trim();
        if (user) { m.UserBox = title; } else { m.AppBox = title; }
        Rdv3Json rows = box.Member(Rdv3Text.BizRows);
        int count = (rows == null) ? 0 : rows.Count;
        if (rows != null && rows.Kind != Rdv3Json.TArray) { throw rows.Fail("must be an array"); }
        if (count > slots.Length)
        { throw rows.Fail(Rdv3Text.BizScreenRows.Replace("{box}", name).Replace("{max}", slots.Length.ToString(CultureInfo.InvariantCulture)).Replace("{n}", count.ToString(CultureInfo.InvariantCulture))); }
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < count) { m.Rows.Add(ReadRow(m, rows.At(i), slots[i], Rdv3Text.BizLabel)); }
            else { ScreenRow hidden = new ScreenRow(); hidden.Slot = slots[i]; hidden.Hidden = true; m.Rows.Add(hidden); }
        }
    }

    private static void ReadTextBox(Model m, Rdv3Json box, string slot)
    {
        box.Only(Rdv3Text.BizBoxName, Rdv3Text.BizColumn);
        ScreenRow row = new ScreenRow();
        row.Slot = slot;
        row.Label = box.StrOr(Rdv3Text.BizBoxName, "").Trim();
        Ref r = ParseRef(m, box.Need(Rdv3Text.BizColumn), box.Member(Rdv3Text.BizColumn), Rdv3Text.BizScreen);
        RequireJoined(m, r, box.Member(Rdv3Text.BizColumn), Rdv3Text.BizScreen);
        row.Field = r.Text;
        m.Rows.Add(row);
    }

    private static ScreenRow ReadRow(Model m, Rdv3Json node, string slot, string captionMember)
    {
        if (node.Kind != Rdv3Json.TObject) { throw node.Fail("must be an object"); }
        node.Only(captionMember, Rdv3Text.BizColumn, Rdv3Text.BizDateFrom, Rdv3Text.BizDateTo);
        ScreenRow row = new ScreenRow();
        row.Slot = slot;
        row.Label = node.StrOr(captionMember, "").Trim();
        Ref r = ParseRef(m, node.Need(Rdv3Text.BizColumn), node.Member(Rdv3Text.BizColumn), Rdv3Text.BizScreen);
        RequireJoined(m, r, node.Member(Rdv3Text.BizColumn), Rdv3Text.BizScreen);
        row.Field = r.Text;
        row.DateFrom = node.StrOr(Rdv3Text.BizDateFrom, "").Trim();
        row.DateTo = node.StrOr(Rdv3Text.BizDateTo, "").Trim();
        if (row.DateFrom.Length > 0)
        {
            string known;
            if (m.DateFormats.TryGetValue(r.Text, out known) && known != row.DateFrom)
            { throw node.Member(Rdv3Text.BizDateFrom).Fail(Rdv3Text.BizDateConflict.Replace("{ref}", r.Text).Replace("{a}", known).Replace("{b}", row.DateFrom)); }
            m.DateFormats[r.Text] = row.DateFrom;
            if (row.DateTo.Length == 0) { row.DateTo = row.DateFrom; }
        }
        return row;
    }

    // ---- the generic definition -------------------------------------------------
    private static string Q(string s) { return Rdv3Json.Quote(s); }

    private static string Arr(IList<string> values)
    {
        StringBuilder sb = new StringBuilder("[");
        for (int i = 0; i < values.Count; i++) { if (i > 0) { sb.Append(','); } sb.Append(Q(values[i])); }
        return sb.Append(']').ToString();
    }

    private static string LabelOf(string reference)
    {
        int dot = reference.IndexOf('.');
        if (dot <= 0) { return reference; }
        return Rdv3Text.BizLabelOfFmt.Replace("{table}", reference.Substring(0, dot)).Replace("{column}", reference.Substring(dot + 1));
    }

    private static List<string> SavedColumns(Model m)
    {
        List<string> saved = new List<string>();
        foreach (ScreenRow row in m.Rows) { if (!row.Hidden && !saved.Contains(row.Field)) { saved.Add(row.Field); } }
        foreach (ScreenRow row in m.Candidates) { if (!row.Hidden && !saved.Contains(row.Field)) { saved.Add(row.Field); } }
        foreach (Ref r in m.Identity) { if (!saved.Contains(r.Text)) { saved.Add(r.Text); } }
        foreach (Ref r in m.Search) { if (!saved.Contains(r.Text)) { saved.Add(r.Text); } }
        foreach (List<Ref> alternatives in m.DeleteLedgerSides) { foreach (Ref r in alternatives) { if (!saved.Contains(r.Text)) { saved.Add(r.Text); } } }
        foreach (string e in m.ExportDefaults) { if (e != "$work" && !saved.Contains(e)) { saved.Add(e); } }
        if (!saved.Contains(m.JudgeRef)) { saved.Add(m.JudgeRef); }
        return saved;
    }

    private static string DataJson(Model m)
    {
        StringBuilder sb = new StringBuilder(8192);
        sb.Append("{\"encoding\":\"utf-8\",\"tables\":{");
        for (int i = 0; i < m.Tables.Count; i++)
        {
            string t = m.Tables[i];
            if (i > 0) { sb.Append(','); }
            sb.Append(Q(t)).Append(":{\"label\":").Append(Q(t)).Append(",\"file\":").Append(Q(m.Files[t]));
            sb.Append(",\"fileMatch\":").Append(Q(m.Matches[t])).Append(",\"key\":").Append(Arr(m.Keys[t]));
            sb.Append(",\"keyValidation\":{\"characters\":\"unicode\",\"length\":\"variable\",\"duplicates\":\"error\",\"empty\":\"error\"}}");
        }
        sb.Append("},\"types\":{");
        bool comma = false;
        foreach (KeyValuePair<string, string> date in m.DateFormats)
        {
            // a cut-out column has no file to check; its notation is only a display format
            bool derived = false;
            foreach (Extract e in m.Extracts) { if (e.Name == date.Key) { derived = true; break; } }
            if (derived) { continue; }
            if (comma) { sb.Append(','); }
            comma = true;
            sb.Append(Q(date.Key)).Append(":{\"type\":\"date\",\"format\":").Append(Q(date.Value)).Append('}');
        }
        sb.Append("},\"labels\":{");
        List<string> labelled = new List<string>();
        comma = false;
        foreach (string reference in m.AllRefs)
        {
            if (m.Tables.Contains(reference) || labelled.Contains(reference)) { continue; }
            labelled.Add(reference);
            if (comma) { sb.Append(','); }
            comma = true;
            sb.Append(Q(reference)).Append(':').Append(Q(LabelOf(reference)));
        }
        List<string> made = new List<string>();
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
        sb.Append("},\"jobs\":[").Append(deleteJob).Append(',').Append(updateJob).Append(']');
        sb.Append(",\"ledger\":{\"identity\":").Append(Arr(RefTexts(m.Identity)));
        sb.Append(",\"search\":{\"columns\":").Append(Arr(RefTexts(m.Search))).Append(",\"match\":\"exact\"}");
        sb.Append(",\"columns\":{\"source\":").Append(Arr(SavedColumns(m)));
        sb.Append(",\"application\":[{\"name\":\"workState\",\"onSourceChange\":\"reset\"}]},\"protectStates\":[\"TRUE\"]}}");
        return sb.ToString();
    }

    private static List<string> RefTexts(List<Ref> refs)
    {
        List<string> texts = new List<string>();
        foreach (Ref r in refs) { texts.Add(r.Text); }
        return texts;
    }

    private static string Calculate(string table, Extract e)
    {
        return "{\"operation\":\"calculate\",\"target1\":" + Q(table) + ",\"column\":" + Q(e.Column)
            + ",\"expression\":" + Q(e.Expression) + ",\"output\":" + Q(table) + "}";
    }

    private static string UpdateJob(Model m, List<string> made)
    {
        made.Add(Rdv3Text.BizJoined);
        StringBuilder sb = new StringBuilder(4096);
        sb.Append("{\"id\":").Append(Q(Rdv3Text.BizUpdateJob)).Append(",\"name\":").Append(Q(Rdv3Text.BizUpdateJobName)).Append(",\"kind\":\"update\",\"inputs\":[");
        for (int i = 0; i < m.JoinedTables.Count; i++) { if (i > 0) { sb.Append(','); } sb.Append("{\"table\":").Append(Q(m.JoinedTables[i])).Append('}'); }
        sb.Append("],\"steps\":[");
        List<string> steps = new List<string>();
        foreach (string table in m.JoinedTables)
        { foreach (Extract e in m.Extracts) { if (e.Table == table) { steps.Add(Calculate(table, e)); } } }
        string joined = Rdv3Text.BizJoined;
        for (int i = 0; i < m.Joins.Count; i++)
        {
            Join j = m.Joins[i];
            string left = (i == 0) ? j.Left.Table : joined;
            steps.Add("{\"operation\":\"join\",\"target1\":" + Q(left) + ",\"target2\":" + Q(j.Right.Table)
                + ",\"keys\":[" + Q(j.Left.Text) + "," + Q(j.Right.Text) + "],\"condition\":" + Q(j.Keep) + ",\"output\":" + Q(joined) + "}");
        }
        steps.Add("{\"operation\":\"calculate\",\"target1\":" + Q(joined) + ",\"column\":" + Q(Rdv3Text.BizJudgeColumn) + ",\"expression\":\"''\",\"output\":" + Q(joined) + "}");
        string rows = "";
        for (int i = 0; i < m.Conditions.Count; i++)
        {
            string name = Rdv3Text.BizCondPrefix + (i + 1).ToString(CultureInfo.InvariantCulture);
            made.Add(name);
            steps.Add("{\"operation\":\"extract\",\"target1\":" + Q(joined) + ",\"where\":{\"column\":" + Q(m.Conditions[i][0].Text)
                + ",\"operator\":\"equals\",\"value\":" + Q(m.ConditionValues[i]) + "},\"output\":" + Q(name) + "}");
            if (i == 0) { rows = name; continue; }
            string both = (i == m.Conditions.Count - 1) ? Rdv3Text.BizPaidRows : Rdv3Text.BizCondPrefix + "1-" + (i + 1).ToString(CultureInfo.InvariantCulture);
            made.Add(both);
            steps.Add("{\"operation\":\"extract\",\"target1\":" + Q(rows) + ",\"target2\":" + Q(name) + ",\"condition\":\"both\",\"output\":" + Q(both) + "}");
            rows = both;
        }
        if (m.Conditions.Count == 1)
        {
            made.Add(Rdv3Text.BizPaidRows);
            steps.Add("{\"operation\":\"extract\",\"target1\":" + Q(rows) + ",\"target2\":" + Q(rows) + ",\"condition\":\"both\",\"output\":" + Q(Rdv3Text.BizPaidRows) + "}");
        }
        steps.Add("{\"operation\":\"update\",\"target1\":" + Q(joined) + ",\"target2\":" + Q(Rdv3Text.BizPaidRows)
            + ",\"set\":[{\"column\":" + Q(m.JudgeRef) + ",\"expression\":" + Q("'" + Rdv3Text.BizPaidStored + "'") + "}],\"output\":" + Q(joined) + "}");
        string identity = Arr(RefTexts(m.Identity));
        steps.Add("{\"operation\":\"merge\",\"target1\":" + Q(joined) + ",\"target2\":\"ledger\",\"keys\":[" + identity + "," + identity
            + "],\"sourceOnly\":\"add\",\"both\":\"update\",\"targetOnly\":\"keep\",\"output\":\"ledger\"}");
        sb.Append(string.Join(",", steps.ToArray())).Append("]}");
        return sb.ToString();
    }

    private static string DeleteJob(Model m, List<string> made)
    {
        int combos = 1;
        foreach (List<Ref> alternatives in m.DeleteLedgerSides) { combos *= alternatives.Count; }
        made.Add(Rdv3Text.BizProcessedRows);
        made.Add(Rdv3Text.BizTargetRows);
        StringBuilder sb = new StringBuilder(4096);
        sb.Append("{\"id\":").Append(Q(Rdv3Text.BizDeleteJob)).Append(",\"name\":").Append(Q(Rdv3Text.BizDeleteJobName));
        sb.Append(",\"kind\":\"delete\",\"inputs\":[{\"table\":").Append(Q(m.DeleteTable)).Append("}],\"steps\":[");
        List<string> steps = new List<string>();
        foreach (Extract e in m.Extracts) { if (e.Table == m.DeleteTable) { steps.Add(Calculate(m.DeleteTable, e)); } }
        // one extract per combination of the ledger-side alternatives, joined by "either"
        List<int> choice = new List<int>();
        for (int g = 0; g < m.DeleteLedgerSides.Count; g++) { choice.Add(0); }
        List<string> matched = new List<string>();
        for (int c = 0; c < combos; c++)
        {
            List<string> ledgerSide = new List<string>();
            List<string> deleteSide = new List<string>();
            for (int g = 0; g < m.DeleteLedgerSides.Count; g++) { ledgerSide.Add(m.DeleteLedgerSides[g][choice[g]].Text); deleteSide.Add(m.DeleteSides[g].Text); }
            string name = Rdv3Text.BizMatchedPrefix + (c + 1).ToString(CultureInfo.InvariantCulture);
            made.Add(name);
            steps.Add("{\"operation\":\"extract\",\"target1\":\"ledger\",\"target2\":" + Q(m.DeleteTable) + ",\"keys\":[" + Arr(ledgerSide) + "," + Arr(deleteSide)
                + "],\"condition\":\"match\",\"output\":" + Q(name) + "}");
            matched.Add(name);
            for (int g = m.DeleteLedgerSides.Count - 1; g >= 0; g--)
            {
                choice[g]++;
                if (choice[g] < m.DeleteLedgerSides[g].Count) { break; }
                choice[g] = 0;
            }
        }
        string all = matched[0];
        for (int i = 1; i < matched.Count; i++)
        {
            string name = (i == matched.Count - 1) ? Rdv3Text.BizMatchedPrefix : Rdv3Text.BizMatchedPrefix + "1-" + (i + 1).ToString(CultureInfo.InvariantCulture);
            made.Add(name);
            steps.Add("{\"operation\":\"extract\",\"target1\":" + Q(all) + ",\"target2\":" + Q(matched[i]) + ",\"condition\":\"either\",\"output\":" + Q(name) + "}");
            all = name;
        }
        steps.Add("{\"operation\":\"extract\",\"target1\":\"ledger\",\"where\":{\"column\":\"$work\",\"operator\":\"equals\",\"value\":\"TRUE\"},\"output\":" + Q(Rdv3Text.BizProcessedRows) + "}");
        steps.Add("{\"operation\":\"extract\",\"target1\":" + Q(all) + ",\"target2\":" + Q(Rdv3Text.BizProcessedRows) + ",\"condition\":\"both\",\"output\":" + Q(Rdv3Text.BizTargetRows) + "}");
        steps.Add("{\"operation\":\"delete\",\"target1\":\"ledger\",\"target2\":" + Q(Rdv3Text.BizTargetRows) + ",\"output\":\"ledger\"}");
        sb.Append(string.Join(",", steps.ToArray())).Append("]}");
        return sb.ToString();
    }

    private static string Binding(Model m, ScreenRow row)
    {
        if (row.Hidden) { return "{\"hidden\":true}"; }
        StringBuilder sb = new StringBuilder("{\"label\":");
        sb.Append(Q(row.Label)).Append(",\"field\":").Append(Q(row.Field)).Append(",\"empty\":\"\"");
        if (row.DateFrom != null && row.DateFrom.Length > 0)
        { sb.Append(",\"format\":{\"kind\":\"date\",\"from\":").Append(Q(row.DateFrom)).Append(",\"to\":").Append(Q(row.DateTo)).Append('}'); }
        return sb.Append('}').ToString();
    }

    private static string ScreenJson(Model m)
    {
        StringBuilder sb = new StringBuilder(4096);
        sb.Append("{\"bindings\":{");
        for (int i = 0; i < m.Rows.Count; i++)
        {
            if (i > 0) { sb.Append(','); }
            sb.Append(Q(m.Rows[i].Slot)).Append(':').Append(Binding(m, m.Rows[i]));
        }
        sb.Append("},\"judgments\":{\"paymentStatus\":{\"label\":").Append(Q(m.JudgeLabel));
        sb.Append(",\"source\":{\"field\":").Append(Q(m.JudgeRef)).Append('}');
        sb.Append(",\"rules\":[{\"equals\":[").Append(Q(Rdv3Text.BizPaidStored)).Append("],\"result\":\"paid\"},{\"empty\":true,\"result\":\"unpaid\"}]");
        sb.Append(",\"results\":{\"paid\":{\"text\":").Append(Q(m.PaidText)).Append(",\"look\":\"ok\"}");
        sb.Append(",\"undefined\":{\"text\":").Append(Q(Rdv3Text.BizJudgeUndefined)).Append(",\"look\":\"undefined\"}");
        sb.Append(",\"error\":{\"text\":").Append(Q(Rdv3Text.BizJudgeError)).Append(",\"look\":\"error\"}");
        sb.Append(",\"unpaid\":{\"text\":").Append(Q(m.UnpaidText)).Append(",\"look\":\"ng\"}}}}");
        sb.Append(",\"workState\":{\"trigger\":\"automatic\",\"store\":{\"column\":").Append(Q(Rdv3Text.BizWorkColumn)).Append('}');
        sb.Append(",\"states\":[{\"id\":\"todo\",\"text\":\"未確認\",\"short\":\"未確認\",\"look\":\"neutral\",\"stored\":\"FALSE\"},");
        sb.Append("{\"id\":\"done\",\"text\":\"確認済\",\"short\":\"確認済\",\"look\":\"accent\",\"stored\":\"TRUE\"}],\"initial\":\"todo\"");
        sb.Append(",\"transitions\":[{\"from\":\"todo\",\"to\":\"done\",\"done\":\"確認済にしました\"},");
        sb.Append("{\"from\":\"done\",\"to\":\"todo\",\"confirm\":\"表示中の案件を未確認に戻します。よろしいですか。\",\"done\":\"未確認に戻しました\"}]");
        sb.Append(",\"automaticWhen\":{\"judgment\":\"paymentStatus\",\"result\":\"paid\"}}");
        List<string> exports = new List<string>(m.ExportDefaults);
        if (exports.Count == 0)
        {
            foreach (ScreenRow row in m.Rows) { if (!row.Hidden && !exports.Contains(row.Field)) { exports.Add(row.Field); } }
            exports.Add("$work");
        }
        sb.Append(",\"export\":{\"defaultFields\":").Append(Arr(exports)).Append('}');
        sb.Append(",\"actions\":{\"updateRecords\":").Append(Q(Rdv3Text.BizUpdateJob)).Append(",\"deleteRecords\":").Append(Q(Rdv3Text.BizDeleteJob)).Append('}');
        sb.Append(",\"candidates\":{");
        for (int i = 0; i < m.Candidates.Count; i++)
        {
            ScreenRow row = m.Candidates[i];
            if (i > 0) { sb.Append(','); }
            sb.Append(Q(row.Slot)).Append(":{\"header\":").Append(Q(row.Hidden ? "" : row.Label)).Append(",\"value\":");
            if (row.Hidden) { sb.Append("{\"hidden\":true}"); }
            else
            {
                sb.Append("{\"field\":").Append(Q(row.Field)).Append(",\"empty\":\"\"");
                if (row.DateFrom != null && row.DateFrom.Length > 0)
                { sb.Append(",\"format\":{\"kind\":\"date\",\"from\":").Append(Q(row.DateFrom)).Append(",\"to\":").Append(Q(row.DateTo)).Append('}'); }
                sb.Append('}');
            }
            sb.Append('}');
        }
        sb.Append(",\"payment\":{\"header\":").Append(Q(m.JudgeLabel)).Append(",\"value\":{\"field\":").Append(Q(m.JudgeRef)).Append(",\"empty\":").Append(Q(m.UnpaidText)).Append('}');
        sb.Append(",\"looks\":{").Append(Q(Rdv3Text.BizPaidStored)).Append(":\"accent\",\"*\":\"neutral\"}}}}");
        return sb.ToString();
    }

    // ---- captions the page applies (the box names and the search label) ------
    public static string SearchLabel = "";
    public static string UserBoxName = "";
    public static string AppBoxName = "";
    public static void RememberCaptions(Rdv3Json block)
    {
        Rdv3Json screen = block.Member(Rdv3Text.BizScreen);
        if (screen == null || screen.Kind != Rdv3Json.TObject) { return; }
        SearchLabel = screen.StrOr(Rdv3Text.BizSearchLabel, "").Trim();
        Rdv3Json user = screen.Member(Rdv3Text.BizUserBox);
        Rdv3Json app = screen.Member(Rdv3Text.BizAppBox);
        UserBoxName = (user == null || user.Kind != Rdv3Json.TObject) ? "" : user.StrOr(Rdv3Text.BizBoxName, "").Trim();
        AppBoxName = (app == null || app.Kind != Rdv3Json.TObject) ? "" : app.StrOr(Rdv3Text.BizBoxName, "").Trim();
    }

    // A message about the generated definition, in the words of the block.
    public static string Localize(string message)
    {
        if (!Enabled || message == null) { return message; }
        string text = message;
        int at = text.IndexOf("data.tables.", StringComparison.Ordinal);
        while (at >= 0)
        {
            int end = at + "data.tables.".Length;
            int keyAt = text.IndexOf(".key", end, StringComparison.Ordinal);
            if (keyAt < 0) { break; }
            string table = text.Substring(end, keyAt - end);
            if (table.IndexOf(' ') >= 0 || table.IndexOf('.') >= 0) { at = text.IndexOf("data.tables.", end, StringComparison.Ordinal); continue; }
            text = text.Substring(0, at) + Rdv3Text.BizPathKey.Replace("{table}", table) + text.Substring(keyAt + ".key".Length);
            at = text.IndexOf("data.tables.", StringComparison.Ordinal);
        }
        text = text.Replace("data.ledger.identity", Rdv3Text.BizPathIdentity);
        text = text.Replace("data.ledger.search.columns", Rdv3Text.BizPathSearch);
        text = text.Replace("data.ledger.columns.source", Rdv3Text.BizPathSource);
        text = text.Replace("screen.bindings", Rdv3Text.BizPathScreen).Replace("screen.candidates", Rdv3Text.BizPathScreen + "." + Rdv3Text.BizCandidates);
        return text;
    }
}
