// ============================================================================
// SettingsEditor.cs -- the settings editor that runs in the black window.
//
// It changes settings.json by talking, not by opening the file: the screen's
// rows, the captions, the input file names and the paid conditions. What has
// to move with a change -- an input file that is not listed yet, a table that
// is not joined yet, a column that has to be cut out of another one -- it
// asks about and writes in the same pass.
//
// Two rules hold the whole file together:
//   * every column is picked from a list built out of the CSV that is really
//     in the data folder, with the first row's value beside it, so the person
//     sees the data and not a name they have to remember;
//   * the file is edited as text, one line at a time. A line nobody touched
//     keeps every byte it had, comments and order included.
//
// The CSV is only ever read. Nothing from it is written into settings.json
// or into the backup.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public static class Rdv3SettingsEditor
{
    // ---- what the file holds ------------------------------------------------
    private sealed class Col
    {
        public string Name;          // the column as its file heads it
        public string Reference;     // "決済.備考2"
        public bool Derived;         // made by a computed column
        public string Sample = "";   // the first data row's value
    }

    private sealed class Table
    {
        public string Id, File, Match, Path = "", Why = "";
        public bool Found;
        public List<Col> Cols = new List<Col>();
        public List<string[]> Rows = new List<string[]>();   // values, in Cols order
        public int Line;
    }

    private sealed class Row
    {
        public string Box, BoxTitle, CaptionKey;
        public int Line;
        public string Label = "", Field = "", DateFrom = "", DateTo = "";
        public bool TextFrame;
    }

    private sealed class Edit
    {
        public int Line, Seq, Kind;   // 0 = replace the line, 1 = put a line after it
        public string Text;
    }

    private const string Files = "入力ファイル";
    private const string Calc = "計算列";
    private const string Joins = "結合";
    private const string Paid = "支払済の判定条件";
    private const string Screen = "画面";
    private const string KeyLabel = "ラベル";
    private const string KeyCaption = "見出し";
    private const string KeyColumn = "列";
    private const string KeyDateFrom = "CSVの日付";
    private const string KeyDateTo = "画面の日付";
    private static readonly string[] TextFrames = { "文章枠1", "文章枠2", "文章枠3" };

    private static string appDir = "", settingsPath = "", dataDir = "";
    private static List<string> lines = new List<string>();
    private static Rdv3Json root, biz, screen;
    private static List<Table> tables = new List<Table>();
    private static List<Row> rows = new List<Row>();
    private static List<Edit> edits = new List<Edit>();
    private static List<string> planned = new List<string>();
    private static bool rebuild;
    private static int seq;

    // ---- the run ------------------------------------------------------------
    public static int Run(string baseDirectory)
    {
        appDir = baseDirectory;
        settingsPath = Path.Combine(appDir, "settings.json");
        if (!File.Exists(settingsPath))
        {
            Console.WriteLine("settings.json が見つかりません: " + settingsPath);
            return 3;
        }
        try { Load(); }
        catch (Exception error)
        {
            Console.WriteLine("settings.json を読めません: " + error.Message);
            return 3;
        }
        while (true)
        {
            ShowScreen();
            ShowMenu();
            string answer = Ask("> ");
            if (answer.Length == 0) { continue; }
            if (answer == "q" || answer == "Q") { Console.WriteLine("終わります。"); return 0; }
            Begin();
            try
            {
                if (answer == "c" || answer == "C") { EditCalc(); }
                else if (answer == "s" || answer == "S") { EditCaptions(); }
                else if (answer == "f" || answer == "F") { EditFiles(); }
                else if (answer == "p" || answer == "P") { EditPaid(); }
                else
                {
                    int n;
                    if (!int.TryParse(answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)
                        || n < 1 || n > rows.Count)
                    { Console.WriteLine("その番号はありません。"); continue; }
                    EditRow(rows[n - 1]);
                }
            }
            catch (Stop) { Console.WriteLine("やめました。"); continue; }
            Commit();
        }
    }

    private sealed class Stop : Exception { }

    // ---- reading ------------------------------------------------------------
    private static void Load()
    {
        string text = File.ReadAllText(settingsPath, new UTF8Encoding(false));
        lines = new List<string>(text.Replace("\r\n", "\n").Split('\n'));
        root = Rdv3Json.Parse(text);
        biz = root.Member("業務設定");
        if (biz == null) { throw new Rdv3LoadError("業務設定 がありません。", 0); }
        screen = biz.Member(Screen);
        Rdv3Json places = root.Member("保存先");
        string where = (places == null) ? "data" : StrOr(places, "データ", "data");
        dataDir = Rdv3Files.Full(where, appDir);
        ReadTables();
        ReadRows();
    }

    private static string StrOr(Rdv3Json at, string name, string fallback)
    {
        Rdv3Json v = (at == null) ? null : at.Member(name);
        return (v != null && v.Kind == Rdv3Json.TString) ? v.Str : fallback;
    }

    // Every input file, with its first data row. A file that is not there is
    // said so and its columns are simply not offered.
    private static void ReadTables()
    {
        tables = new List<Table>();
        Rdv3Json files = biz.Member(Files);
        if (files == null) { return; }
        foreach (string id in files.Order)
        {
            Rdv3Json entry = files.Member(id);
            Table t = new Table();
            t.Id = id;
            t.Line = entry.Line;
            t.File = StrOr(entry, "ファイル名", "");
            t.Match = StrOr(entry, "一致", "前方一致") == "完全一致" ? "exact" : "prefix";
            try
            {
                string note;
                t.Path = Rdv3Files.ResolveInput(t.File, t.Match, dataDir, out note);
                t.Found = t.Path != null && t.Path.Length > 0 && File.Exists(t.Path);
            }
            catch (Exception) { t.Found = false; }
            if (!t.Found) { t.Why = "（ファイルが見つかりません）"; }
            else
            {
                try { ReadCsv(t); }
                catch (Exception error) { t.Found = false; t.Why = "（読めません: " + Short(error.Message) + "）"; }
            }
            tables.Add(t);
        }
        foreach (Table t in tables) { AddCalcColumns(t); }
    }

    private static void ReadCsv(Table t)
    {
        byte[] bytes = File.ReadAllBytes(t.Path);
        Rdv3InputCounts counts = new Rdv3InputCounts();
        Encoding encoding = Rdv3Csv.ResolveEncoding(bytes, new UTF8Encoding(false), counts, t.Path);
        string[] head;
        string[][] data;
        int[] numbers;
        Rdv3Csv.Read(t.Path, bytes, encoding, false, out head, out data, out numbers, "文字コード", null, counts);
        for (int i = 0; i < head.Length; i++)
        {
            Col c = new Col();
            c.Name = head[i];
            c.Reference = t.Id + "." + head[i];
            c.Sample = (data.Length > 0 && i < data[0].Length) ? data[0][i] : "";
            t.Cols.Add(c);
        }
        for (int r = 0; r < data.Length && r < 200; r++) { t.Rows.Add(data[r]); }
    }

    // A computed column stands at the end of its table's list, with the value
    // the expression really produces for the first row. One computed column may
    // stand on another, so each is evaluated onto the rows before the next.
    private static void AddCalcColumns(Table t)
    {
        Rdv3Json calc = biz.Member(Calc);
        if (calc == null || calc.Kind != Rdv3Json.TArray) { return; }
        for (int i = 0; i < calc.Count; i++)
        {
            Rdv3Json entry = calc.At(i);
            string name = StrOr(entry, "列名", "");
            string expression = StrOr(entry, "式", "");
            if (name.Length == 0 || TableOf(expression) != t.Id) { continue; }
            string[] names = ReferencesAll(t);
            Col c = new Col();
            c.Name = name;
            c.Reference = t.Id + "." + name;
            c.Derived = true;
            c.Sample = (t.Rows.Count > 0) ? Evaluate(names, t.Rows[0], expression) : "";
            t.Cols.Add(c);
            for (int r = 0; r < t.Rows.Count; r++)
            {
                string[] was = t.Rows[r];
                string[] now = new string[was.Length + 1];
                Array.Copy(was, now, was.Length);
                now[was.Length] = Evaluate(names, was, expression);
                t.Rows[r] = now;
            }
        }
    }

    private static string Evaluate(string[] names, string[] values, string expression)
    {
        try
        {
            Rdv3Expression compiled = Rdv3Expression.Compile(expression, names);
            return compiled.Evaluate(values);
        }
        catch (Exception) { return "（計算できません）"; }
    }

    // the table a computed column belongs to: the one its expression reads
    private static string TableOf(string expression)
    {
        string found = null;
        foreach (Table t in tables)
        {
            if (expression.IndexOf(t.Id + ".", StringComparison.Ordinal) < 0) { continue; }
            if (found != null && found != t.Id) { return ""; }
            found = t.Id;
        }
        return found == null ? "" : found;
    }

    private static void ReadRows()
    {
        rows = new List<Row>();
        if (screen == null) { return; }
        AddBoxRows("左上の枠");
        AddBoxRows("右上の枠");
        foreach (string frame in TextFrames)
        {
            Rdv3Json box = screen.Member(frame);
            if (box == null) { continue; }
            Row row = new Row();
            row.Box = frame;
            row.TextFrame = true;
            row.CaptionKey = KeyCaption;
            row.BoxTitle = StrOr(box, KeyCaption, "");
            row.Label = row.BoxTitle;
            row.Field = StrOr(box, KeyColumn, "");
            row.Line = box.Line;
            rows.Add(row);
        }
    }

    private static void AddBoxRows(string box)
    {
        Rdv3Json at = (screen == null) ? null : screen.Member(box);
        if (at == null) { return; }
        string title = StrOr(at, KeyCaption, "");
        Rdv3Json list = at.Member("行");
        if (list == null || list.Kind != Rdv3Json.TArray) { return; }
        for (int i = 0; i < list.Count; i++)
        {
            Rdv3Json entry = list.At(i);
            Row row = new Row();
            row.Box = box;
            row.BoxTitle = title;
            row.CaptionKey = KeyLabel;
            row.Label = StrOr(entry, KeyLabel, "");
            row.Field = StrOr(entry, KeyColumn, "");
            row.DateFrom = StrOr(entry, KeyDateFrom, "");
            row.DateTo = StrOr(entry, KeyDateTo, "");
            row.Line = entry.Line;
            rows.Add(row);
        }
    }

    // ---- what is on the screen now -----------------------------------------
    private static void ShowScreen()
    {
        Console.WriteLine();
        Console.WriteLine("いまの画面");
        string box = "";
        for (int i = 0; i < rows.Count; i++)
        {
            Row row = rows[i];
            if (!row.TextFrame && row.Box != box)
            {
                box = row.Box;
                Console.WriteLine("[" + row.Box + "] " + row.BoxTitle);
            }
            string number = Right((i + 1).ToString(CultureInfo.InvariantCulture), 3);
            string head = row.TextFrame
                ? number + "  [" + row.Box + "] " + row.Label
                : number + "  " + row.Label;
            Console.WriteLine(Pad(head, 34) + " ← " + Pad(row.Field + Explain(row), 46)
                + "  " + OneLine(Sample(row.Field), 40));
        }
        Rdv3Json band = (screen == null) ? null : screen.Member("決済状況の帯");
        if (band != null) { Console.WriteLine("[決済状況の帯] " + StrOr(band, KeyCaption, "")); }
    }

    private static string Explain(Row row)
    {
        if (row.DateFrom.Length > 0)
        { return "（日付 " + row.DateFrom + " → " + (row.DateTo.Length > 0 ? row.DateTo : row.DateFrom) + "）"; }
        string expression = ExpressionOf(row.Field);
        return expression.Length == 0 ? "" : "（計算列: " + Japanese(expression) + "）";
    }

    private static string ExpressionOf(string reference)
    {
        Rdv3Json calc = biz.Member(Calc);
        if (calc == null || calc.Kind != Rdv3Json.TArray) { return ""; }
        for (int i = 0; i < calc.Count; i++)
        {
            Rdv3Json entry = calc.At(i);
            string name = StrOr(entry, "列名", "");
            string expression = StrOr(entry, "式", "");
            if (TableOf(expression) + "." + name == reference) { return expression; }
        }
        return "";
    }

    // "splitPart(決済.氏名, '-', 0)" -> "決済.氏名 の '-' 0番目"
    private static string Japanese(string expression)
    {
        string name, source, first, second;
        if (!Parts(expression, out name, out source, out first, out second)) { return expression; }
        if (name == "splitPart") { return source + " の '" + first + "' " + second + "番目"; }
        if (name == "regexExtract") { return source + " の 形に合う部分"; }
        if (name == "substring") { return source + " の 位置 " + first + " から " + second + " 文字"; }
        return expression;
    }

    private static bool Parts(string expression, out string name, out string source, out string first, out string second)
    {
        name = source = first = second = "";
        int open = expression.IndexOf('(');
        int close = expression.LastIndexOf(')');
        if (open <= 0 || close < open) { return false; }
        name = expression.Substring(0, open).Trim();
        if (!Rdv3Expression.IsFunctionName(name)) { return false; }
        string inside = expression.Substring(open + 1, close - open - 1);
        List<string> parts = new List<string>();
        int depth = 0, start = 0;
        char quote = '\0';
        for (int i = 0; i < inside.Length; i++)
        {
            char c = inside[i];
            if (quote != '\0') { if (c == quote) { quote = '\0'; } continue; }
            if (c == '\'' || c == '"') { quote = c; continue; }
            if (c == '(') { depth++; }
            else if (c == ')') { depth--; }
            else if (c == ',' && depth == 0) { parts.Add(inside.Substring(start, i - start).Trim()); start = i + 1; }
        }
        parts.Add(inside.Substring(start).Trim());
        if (parts.Count < 2) { return false; }
        source = parts[0];
        first = Unquote(parts[1]);
        second = parts.Count > 2 ? Unquote(parts[2]) : "";
        return true;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && (value[0] == '\'' || value[0] == '"') && value[value.Length - 1] == value[0])
        { return value.Substring(1, value.Length - 2); }
        return value;
    }

    private static string Sample(string reference)
    {
        foreach (Table t in tables)
        {
            foreach (Col c in t.Cols) { if (c.Reference == reference) { return c.Sample; } }
        }
        return "";
    }

    private static void ShowMenu()
    {
        Console.WriteLine();
        Console.WriteLine("  番号を入れると、その行のラベルと列を続けて聞きます（変えない方は Enter）。");
        Console.WriteLine("    c  計算列を変える（単体）");
        Console.WriteLine("    s  検索欄のラベル・候補一覧・枠の見出し");
        Console.WriteLine("    f  CSV のファイル名");
        Console.WriteLine("    p  支払済の判定条件");
        Console.WriteLine("    q  終わる");
    }

    // ---- one row ------------------------------------------------------------
    private static void EditRow(Row row)
    {
        string label = Ask(row.CaptionKey + " [" + row.Label + "] > ");
        if (label.Length > 0 && label != row.Label)
        {
            SetMember(row.Line, row.CaptionKey, label);
            Plan(row.Box + " の " + row.Label + " のラベル: " + row.Label + " → " + label, false);
        }
        Console.WriteLine("列 [" + row.Field + "]（変えるならテーブルを選ぶ。Enter で今のまま）");
        string field = PickColumn(true);
        if (field.Length == 0 || field == row.Field) { return; }
        string from = "", to = "";
        if (!row.TextFrame)
        {
            from = Ask("CSV の日付の形（yyyyMMdd など、無ければ Enter）> ");
            if (from.Length > 0) { to = Ask("画面の日付の形 > "); if (to.Length == 0) { to = from; } }
        }
        if (row.TextFrame)
        { WriteObject(row.Line, new string[] { KeyCaption, KeyColumn }, new string[] { Caption(row, KeyCaption), field }); }
        else
        {
            WriteObject(row.Line,
                new string[] { KeyLabel, KeyColumn, KeyDateFrom, KeyDateTo },
                new string[] { Caption(row, KeyLabel), field,
                               from.Length > 0 ? from : null, from.Length > 0 ? to : null });
        }
        Plan(row.Box + " の " + row.Label + " の列: " + row.Field + " → " + field, true);
        if (from.Length > 0) { Plan(row.Box + " の " + row.Label + " の日付: " + from + " → " + to, false); }
    }

    // the caption as it will stand after this pass
    private static string Caption(Row row, string key)
    {
        string value = ValueOnLine(Current(row.Line), key);
        return value == null ? row.Label : value;
    }

    // ---- the column list ----------------------------------------------------
    // Nothing is typed here: the tables in the settings, the columns their CSV
    // really has, and the computed columns that stand on them.
    private static string PickColumn(bool allowKeep)
    {
        while (true)
        {
            StringBuilder menu = new StringBuilder("    どのテーブル");
            for (int i = 0; i < tables.Count; i++)
            { menu.Append("  ").Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append(' ').Append(tables[i].Id); }
            menu.Append("  ").Append((tables.Count + 1).ToString(CultureInfo.InvariantCulture)).Append(' ').Append(Calc);
            menu.Append("  t テーブルを足す");
            Console.WriteLine(menu.ToString());
            string answer = Ask("    番号 > ");
            if (answer.Length == 0)
            {
                if (allowKeep) { return ""; }
                Console.WriteLine("    番号を入れてください。");
                continue;
            }
            if (answer == "t" || answer == "T")
            {
                Table made = AddTable();
                if (made == null) { continue; }
                string got = PickFrom(made, true);
                if (got.Length > 0) { return got; }
                continue;
            }
            int n;
            if (!int.TryParse(answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)
                || n < 1 || n > tables.Count + 1)
            { Console.WriteLine("    その番号はありません。"); continue; }
            if (n == tables.Count + 1) { return PickDerived(); }
            Table t = tables[n - 1];
            if (!t.Found && t.Cols.Count == 0)
            { Console.WriteLine("    " + t.Id + " " + t.Why); continue; }
            string picked = PickFrom(t, true);
            if (picked.Length > 0) { return picked; }
        }
    }

    private static string PickFrom(Table t, bool allowNew)
    {
        Console.WriteLine("    " + t.Id + " の列（1 行目の値つき）" + (t.Found ? "" : " " + t.Why));
        for (int i = 0; i < t.Cols.Count; i++)
        {
            Col c = t.Cols[i];
            string head = Right((i + 1).ToString(CultureInfo.InvariantCulture), 6) + "  "
                + Pad(c.Name + (c.Derived ? "〔計算列〕" : ""), 30);
            Console.WriteLine(head + " " + OneLine(c.Sample, 46));
        }
        if (allowNew) { Console.WriteLine("        n  この表に計算列を作る"); }
        string answer = Ask("    番号 > ");
        if (answer.Length == 0) { return ""; }
        if (allowNew && (answer == "n" || answer == "N")) { return NewCalc(t); }
        int n;
        if (!int.TryParse(answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)
            || n < 1 || n > t.Cols.Count)
        { Console.WriteLine("    その番号はありません。"); return ""; }
        return t.Cols[n - 1].Reference;
    }

    private static string PickDerived()
    {
        List<Col> all = new List<Col>();
        List<string> owners = new List<string>();
        foreach (Table t in tables)
        {
            foreach (Col c in t.Cols) { if (c.Derived) { all.Add(c); owners.Add(t.Id); } }
        }
        if (all.Count == 0) { Console.WriteLine("    計算列はまだありません。"); return ""; }
        Console.WriteLine("    計算列（1 行目の値つき）");
        for (int i = 0; i < all.Count; i++)
        {
            Console.WriteLine(Right((i + 1).ToString(CultureInfo.InvariantCulture), 6) + "  "
                + Pad(all[i].Reference, 30) + " " + OneLine(all[i].Sample, 46));
        }
        string answer = Ask("    番号 > ");
        int n;
        if (!int.TryParse(answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)
            || n < 1 || n > all.Count)
        { return ""; }
        return all[n - 1].Reference;
    }

    // ---- a computed column --------------------------------------------------
    private static string NewCalc(Table t)
    {
        string name = Ask("    列名 > ");
        if (name.Length == 0) { return ""; }
        if (name.IndexOf('.') >= 0)
        { Console.WriteLine("    計算列の列名にテーブル名は書きません: " + name); return ""; }
        string expression = BuildExpression(t, "", "", "");
        if (expression.Length == 0) { return ""; }
        AddCalcLine(name, expression);
        Plan("計算列を足す: " + t.Id + "." + name + " ＝ " + Japanese(expression), true);
        return t.Id + "." + name;
    }

    // how it is cut out, which column it is cut from, and the arguments, with
    // the first three rows shown as "the value → what comes out"
    private static string BuildExpression(Table t, string wasWay, string wasFirst, string wasSecond)
    {
        while (true)
        {
            Console.WriteLine("    作り方  1 区切りで何番目  2 形に合う部分  3 位置で切る"
                + (wasWay.Length > 0 ? "  [いまは " + wasWay + "]" : ""));
            string way = Ask("    番号 > ");
            if (way.Length == 0) { way = wasWay; }
            if (way != "1" && way != "2" && way != "3") { return ""; }
            Console.WriteLine("    元の列");
            string source = PickFrom(t, false);
            if (source.Length == 0) { return ""; }
            string expression;
            if (way == "1")
            {
                string separator = Ask("    区切り" + Was(wasFirst) + " > ");
                if (separator.Length == 0) { separator = wasFirst; }
                string at = Ask("    何番目（0 から）" + Was(wasSecond) + " > ");
                if (at.Length == 0) { at = wasSecond; }
                expression = "splitPart(" + source + ", '" + separator + "', " + at + ")";
                wasFirst = separator; wasSecond = at;
            }
            else if (way == "2")
            {
                string pattern = Ask("    形" + Was(wasFirst) + " > ");
                if (pattern.Length == 0) { pattern = wasFirst; }
                expression = "regexExtract(" + source + ", '" + pattern + "')";
                wasFirst = pattern;
            }
            else
            {
                string at = Ask("    位置（0 から）" + Was(wasFirst) + " > ");
                if (at.Length == 0) { at = wasFirst; }
                string count = Ask("    文字数" + Was(wasSecond) + " > ");
                if (count.Length == 0) { count = wasSecond; }
                expression = "substring(" + source + ", " + at + ", " + count + ")";
                wasFirst = at; wasSecond = count;
            }
            wasWay = way;
            if (Preview(t, source, expression)) { return expression; }
        }
    }

    private static string Was(string value) { return value.Length > 0 ? " [" + value + "]" : ""; }

    private static bool Preview(Table t, string source, string expression)
    {
        string[] names = ReferencesAll(t);
        int shown = 0;
        for (int r = 0; r < t.Rows.Count && shown < 3; r++, shown++)
        {
            int at = Array.IndexOf(names, source);
            string was = (at >= 0 && at < t.Rows[r].Length) ? t.Rows[r][at] : "";
            Console.WriteLine("      " + Pad(OneLine(was, 56), 56) + "  →  " + OneLine(Evaluate(names, t.Rows[r], expression), 30));
        }
        if (shown == 0) { Console.WriteLine("      （データ行がないので試せません）"); }
        return Ask("    これでよいですか（y/n）> ").ToLowerInvariant() != "n";
    }

    private static string[] ReferencesAll(Table t)
    {
        List<string> names = new List<string>();
        foreach (Col c in t.Cols) { names.Add(c.Reference); }
        return names.ToArray();
    }

    private static void EditCalc()
    {
        Rdv3Json calc = biz.Member(Calc);
        if (calc == null || calc.Kind != Rdv3Json.TArray) { Console.WriteLine("計算列がありません。"); throw new Stop(); }
        Console.WriteLine("計算列");
        for (int i = 0; i < calc.Count; i++)
        {
            Rdv3Json entry = calc.At(i);
            string expression = StrOr(entry, "式", "");
            Console.WriteLine(Right((i + 1).ToString(CultureInfo.InvariantCulture), 4) + "  "
                + Pad(TableOf(expression) + "." + StrOr(entry, "列名", ""), 28) + " " + Japanese(expression));
        }
        Console.WriteLine("    n  新しく作る");
        string answer = Ask("番号 > ");
        if (answer.Length == 0) { throw new Stop(); }
        if (answer == "n" || answer == "N")
        {
            Table t = PickTable();
            if (NewCalc(t).Length == 0) { throw new Stop(); }
            return;
        }
        int n;
        if (!int.TryParse(answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)
            || n < 1 || n > calc.Count)
        { Console.WriteLine("その番号はありません。"); throw new Stop(); }
        Rdv3Json chosen = calc.At(n - 1);
        string name = StrOr(chosen, "列名", "");
        string old = StrOr(chosen, "式", "");
        Table owner = TableById(TableOf(old));
        if (owner == null) { Console.WriteLine("元の表が分かりません。"); throw new Stop(); }
        string kind, source, first, second;
        Parts(old, out kind, out source, out first, out second);
        string way = kind == "splitPart" ? "1" : kind == "regexExtract" ? "2" : "3";
        string made = BuildExpression(owner, way, first, second);
        if (made.Length == 0) { throw new Stop(); }
        WriteObject(chosen.Line, new string[] { "列名", "式" }, new string[] { name, made });
        Plan("計算列: " + owner.Id + "." + name + " ＝ " + Japanese(old) + " → " + Japanese(made), true);
    }

    private static Table PickTable()
    {
        StringBuilder menu = new StringBuilder("    どのテーブル");
        for (int i = 0; i < tables.Count; i++)
        { menu.Append("  ").Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append(' ').Append(tables[i].Id); }
        Console.WriteLine(menu.ToString());
        string answer = Ask("    番号 > ");
        int n;
        if (!int.TryParse(answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)
            || n < 1 || n > tables.Count)
        { throw new Stop(); }
        return tables[n - 1];
    }

    private static Table TableById(string id)
    {
        foreach (Table t in tables) { if (t.Id == id) { return t; } }
        return null;
    }

    // ---- the captions -------------------------------------------------------
    private static void EditCaptions()
    {
        Rdv3Json at = screen.Member("検索欄のラベル");
        if (at != null)
        {
            string was = at.Str;
            string now = Ask("検索欄のラベル [" + was + "] > ");
            if (now.Length > 0 && now != was)
            { SetMember(at.Line, "検索欄のラベル", now); Plan("検索欄のラベル: " + was + " → " + now, false); }
        }
        foreach (string box in new string[] { "左上の枠", "右上の枠", "文章枠1", "文章枠2", "文章枠3", "決済状況の帯" })
        {
            Rdv3Json frame = screen.Member(box);
            if (frame == null) { continue; }
            string was = StrOr(frame, KeyCaption, "");
            string now = Ask(box + " の見出し [" + was + "] > ");
            if (now.Length == 0 || now == was) { continue; }
            Rdv3Json caption = frame.Member(KeyCaption);
            if (TextFrame(box)) { WriteObject(frame.Line, new string[] { KeyCaption, KeyColumn }, new string[] { now, StrOr(frame, KeyColumn, "") }); }
            else if (caption != null) { SetMember(caption.Line, KeyCaption, now); }
            Plan(box + " の見出し: " + was + " → " + now, false);
        }
        Rdv3Json list = screen.Member("候補一覧");
        if (list != null && list.Kind == Rdv3Json.TArray)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Rdv3Json entry = list.At(i);
                string wasHead = StrOr(entry, "見出し", "");
                string wasField = StrOr(entry, KeyColumn, "");
                string head = Ask("候補一覧 " + (i + 1).ToString(CultureInfo.InvariantCulture) + " の見出し [" + wasHead + "] > ");
                Console.WriteLine("候補一覧 " + (i + 1).ToString(CultureInfo.InvariantCulture) + " の列 [" + wasField + "]（Enter で今のまま）");
                string field = PickColumn(true);
                if ((head.Length == 0 || head == wasHead) && (field.Length == 0 || field == wasField)) { continue; }
                string nowHead = head.Length > 0 ? head : wasHead;
                string nowField = field.Length > 0 ? field : wasField;
                WriteObject(entry.Line,
                    new string[] { "見出し", KeyColumn, KeyDateFrom, KeyDateTo },
                    new string[] { nowHead, nowField,
                                   Blank(StrOr(entry, KeyDateFrom, "")), Blank(StrOr(entry, KeyDateTo, "")) });
                if (nowHead != wasHead) { Plan("候補一覧 の見出し: " + wasHead + " → " + nowHead, false); }
                if (nowField != wasField) { Plan("候補一覧 の列: " + wasField + " → " + nowField, true); }
            }
        }
    }

    private static bool TextFrame(string box)
    {
        foreach (string frame in TextFrames) { if (frame == box) { return true; } }
        return false;
    }

    private static string Blank(string value) { return value.Length == 0 ? null : value; }

    // ---- the input file names ----------------------------------------------
    private static void EditFiles()
    {
        foreach (Table t in tables)
        {
            string now = Ask(t.Id + " のファイル名 [" + t.File + "] > ");
            if (now.Length == 0 || now == t.File) { continue; }
            SetMember(t.Line, "ファイル名", now);
            Plan(t.Id + " のファイル名: " + t.File + " → " + now, false);
        }
    }

    // ---- the paid conditions ------------------------------------------------
    private static void EditPaid()
    {
        Rdv3Json paid = biz.Member(Paid);
        if (paid == null || paid.Kind != Rdv3Json.TArray) { Console.WriteLine(Paid + " がありません。"); throw new Stop(); }
        for (int i = 0; i < paid.Count; i++)
        {
            Rdv3Json entry = paid.At(i);
            string wasField = StrOr(entry, KeyColumn, "");
            string wasValue = StrOr(entry, "値", "");
            Console.WriteLine((i + 1).ToString(CultureInfo.InvariantCulture) + " 件目 の列 [" + wasField + "]（Enter で今のまま）");
            string field = PickColumn(true);
            string value = Ask("値 [" + wasValue + "] > ");
            if ((field.Length == 0 || field == wasField) && (value.Length == 0 || value == wasValue)) { continue; }
            string nowField = field.Length > 0 ? field : wasField;
            string nowValue = value.Length > 0 ? value : wasValue;
            WriteObject(entry.Line, new string[] { KeyColumn, "値" }, new string[] { nowField, nowValue });
            Plan(Paid + ": " + wasField + " が " + wasValue + " → " + nowField + " が " + nowValue, true);
        }
    }

    // ---- the input file and the join a new table needs ----------------------
    // A table the settings do not list yet: its file and key column go into
    // the input files, then the column that joins it to the table the others
    // hang off. Its CSV is read at once, so its columns can be picked here.
    private static Table AddTable()
    {
        string id = Ask("    テーブルの名前 > ");
        if (id.Length == 0) { return null; }
        if (TableById(id) != null) { Console.WriteLine("    その名前はもうあります。"); return null; }
        if (Ask("「" + id + "」というテーブルは入力ファイルにありません。足しますか（y/n）> ").ToLowerInvariant() != "y")
        { return null; }
        string file = Ask("ファイル名（前方一致）> ");
        string key = Ask("キー列 > ");
        if (file.Length == 0 || key.Length == 0) { Console.WriteLine("    ファイル名とキー列が要ります。"); return null; }

        Rdv3Json files = biz.Member(Files);
        int at = files.Order.Count > 0 ? files.Member(files.Order[files.Order.Count - 1]).Line : files.Line;
        Comma(at);
        Insert(at, Indent(at) + Rdv3Json.Quote(id) + ": { " + Rdv3Json.Quote("ファイル名") + ": " + Rdv3Json.Quote(file)
            + ", " + Rdv3Json.Quote("一致") + ": " + Rdv3Json.Quote("前方一致")
            + ", " + Rdv3Json.Quote("キー列") + ": " + Rdv3Json.Quote(key) + " }");
        Plan("入力ファイルに足す: " + id + " ＝ " + file + "（前方一致、キー列 " + key + "）", true);

        Table made = new Table();
        made.Id = id; made.File = file; made.Match = "prefix"; made.Line = at;
        try
        {
            string note;
            made.Path = Rdv3Files.ResolveInput(file, "prefix", dataDir, out note);
            made.Found = made.Path != null && made.Path.Length > 0 && File.Exists(made.Path);
        }
        catch (Exception) { made.Found = false; }
        if (made.Found) { try { ReadCsv(made); } catch (Exception error) { made.Found = false; made.Why = "（読めません: " + Short(error.Message) + "）"; } }
        else { made.Why = "（ファイルが見つかりません）"; }
        tables.Add(made);

        Table spine = Spine();
        if (spine != null)
        {
            Console.WriteLine("「" + spine.Id + "」とつなぐ列");
            string left = PickFrom(spine, false);
            string right = PickFrom(made, false);
            if (left.Length > 0 && right.Length > 0)
            {
                Rdv3Json joins = biz.Member(Joins);
                int line = (joins != null && joins.Count > 0) ? joins.At(joins.Count - 1).Line : joins.Line;
                Comma(line);
                Insert(line, Indent(line) + "{ " + Rdv3Json.Quote("左") + ": " + Rdv3Json.Quote(left)
                    + ", " + Rdv3Json.Quote("右") + ": " + Rdv3Json.Quote(right)
                    + ", " + Rdv3Json.Quote("残す行") + ": " + Rdv3Json.Quote("左の全行") + " }");
                Plan("結合に足す: " + left + " ↔ " + right + "（左の全行）", true);
            }
        }
        return made;
    }

    // the table the others are joined onto: the left side of the first join
    private static Table Spine()
    {
        Rdv3Json joins = biz.Member(Joins);
        if (joins == null || joins.Count == 0) { return null; }
        string left = StrOr(joins.At(0), "左", "");
        int dot = left.IndexOf('.');
        return dot <= 0 ? null : TableById(left.Substring(0, dot));
    }

    // ---- writing the lines --------------------------------------------------
    private static void Begin()
    {
        edits = new List<Edit>();
        planned = new List<string>();
        rebuild = false;
        seq = 0;
    }

    private static void Plan(string text, bool needsRebuild)
    {
        planned.Add(text);
        if (needsRebuild) { rebuild = true; }
    }

    private static void SetMember(int line, string key, string value)
    {
        string text = Current(line);
        string made = Replace(text, key, value);
        if (made == null) { Console.WriteLine("その項目は 1 行で書かれていないので直せません（" + key + "）。"); throw new Stop(); }
        Put(line, 0, made);
    }

    private static string Replace(string text, string key, string value)
    {
        string needle = Rdv3Json.Quote(key);
        int at = text.IndexOf(needle, StringComparison.Ordinal);
        if (at < 0) { return null; }
        int colon = text.IndexOf(':', at + needle.Length);
        if (colon < 0) { return null; }
        int open = text.IndexOf('"', colon + 1);
        if (open < 0) { return null; }
        int close = open + 1;
        while (close < text.Length && text[close] != '"') { if (text[close] == '\\') { close++; } close++; }
        if (close >= text.Length) { return null; }
        return text.Substring(0, open) + Rdv3Json.Quote(value) + text.Substring(close + 1);
    }

    private static string ValueOnLine(string text, string key)
    {
        string needle = Rdv3Json.Quote(key);
        int at = text.IndexOf(needle, StringComparison.Ordinal);
        if (at < 0) { return null; }
        int colon = text.IndexOf(':', at + needle.Length);
        if (colon < 0) { return null; }
        int open = text.IndexOf('"', colon + 1);
        if (open < 0) { return null; }
        int close = open + 1;
        StringBuilder sb = new StringBuilder();
        while (close < text.Length && text[close] != '"')
        {
            if (text[close] == '\\' && close + 1 < text.Length) { close++; }
            sb.Append(text[close]);
            close++;
        }
        return sb.ToString();
    }

    // one object on one line, written the way the file writes them
    private static void WriteObject(int line, string[] keys, string[] values)
    {
        string text = Current(line);
        string indent = Indent(line);
        bool comma = text.TrimEnd().EndsWith(",", StringComparison.Ordinal);
        StringBuilder sb = new StringBuilder(indent);
        sb.Append("{ ");
        bool first = true;
        for (int i = 0; i < keys.Length; i++)
        {
            if (values[i] == null) { continue; }
            if (!first) { sb.Append(", "); }
            first = false;
            sb.Append(Rdv3Json.Quote(keys[i])).Append(": ").Append(Rdv3Json.Quote(values[i]));
        }
        sb.Append(" }");
        if (comma) { sb.Append(','); }
        Put(line, 0, sb.ToString());
    }

    private static void AddCalcLine(string name, string expression)
    {
        Rdv3Json calc = biz.Member(Calc);
        int at = (calc != null && calc.Count > 0) ? calc.At(calc.Count - 1).Line : calc.Line;
        Comma(at);
        Insert(at, Indent(at) + "{ " + Rdv3Json.Quote("列名") + ": " + Rdv3Json.Quote(name)
            + ", " + Rdv3Json.Quote("式") + ": " + Rdv3Json.Quote(expression) + " }");
    }

    private static void Comma(int line)
    {
        string text = Current(line);
        if (text.TrimEnd().EndsWith(",", StringComparison.Ordinal)) { return; }
        Put(line, 0, text.TrimEnd() + ",");
    }

    private static void Insert(int line, string text) { Put(line, 1, text); }

    private static void Put(int line, int kind, string text)
    {
        Edit e = new Edit();
        e.Line = line; e.Kind = kind; e.Text = text; e.Seq = seq++;
        edits.Add(e);
    }

    // the line as this pass will leave it, so two changes to one line agree
    private static string Current(int line)
    {
        for (int i = edits.Count - 1; i >= 0; i--)
        { if (edits[i].Line == line && edits[i].Kind == 0) { return edits[i].Text; } }
        return lines[line - 1];
    }

    private static string Indent(int line)
    {
        string text = lines[line - 1];
        int at = 0;
        while (at < text.Length && (text[at] == ' ' || text[at] == '\t')) { at++; }
        return text.Substring(0, at);
    }

    // ---- showing and writing ------------------------------------------------
    private static void Commit()
    {
        if (planned.Count == 0) { Console.WriteLine("変わるものはありません。"); return; }
        Console.WriteLine();
        Console.WriteLine("これから直すもの");
        foreach (string item in planned) { Console.WriteLine("  " + item); }
        Console.WriteLine("  統合台帳  " + (rebuild ? "作り直しが必要" : "そのまま"));
        if (Ask("書きますか（y/n）> ").ToLowerInvariant() != "y") { Console.WriteLine("書きませんでした。"); return; }

        string backup = settingsPath + ".bak-" + DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        File.Copy(settingsPath, backup, true);
        Console.WriteLine("控え: " + backup);

        List<Edit> order = new List<Edit>(edits);
        order.Sort(delegate(Edit a, Edit b)
        {
            if (a.Line != b.Line) { return b.Line.CompareTo(a.Line); }
            return b.Seq.CompareTo(a.Seq);
        });
        foreach (Edit e in order)
        {
            if (e.Kind == 0) { lines[e.Line - 1] = e.Text; }
            else { lines.Insert(e.Line, e.Text); }
        }
        File.WriteAllText(settingsPath, string.Join("\r\n", lines.ToArray()), new UTF8Encoding(false));
        Console.WriteLine("書きました。");

        Console.WriteLine();
        Console.WriteLine("設定を確かめます。");
        int code;
        try { code = Rdv3Headless.Run(appDir, settingsPath, dataDir, false, null, null); }
        catch (Exception error) { Console.WriteLine(error.Message); code = 3; }
        if (code == 0) { Console.WriteLine("設定は通りました。"); }
        else
        {
            Console.WriteLine("設定が通りませんでした。");
            if (Ask("控えに戻しますか（y/n）> ").ToLowerInvariant() == "y")
            {
                File.Copy(backup, settingsPath, true);
                Console.WriteLine("控えに戻しました。");
            }
        }
        Load();
    }

    // ---- the console --------------------------------------------------------
    private static string Ask(string prompt)
    {
        Console.Write(prompt);
        string answer = Console.ReadLine();
        if (answer == null) { Console.WriteLine(); throw new Stop(); }
        Console.WriteLine(answer);
        return answer.Trim();
    }

    private static string Short(string text)
    {
        text = text.Replace("\r", " ").Replace("\n", " ");
        return text.Length > 60 ? text.Substring(0, 60) + "…" : text;
    }

    // full-width characters take two columns in the window
    private static int Width(string text)
    {
        int width = 0;
        foreach (char c in text) { width += (c < 0x80 || c == 0xFF61 || (c >= 0xFF66 && c <= 0xFF9F)) ? 1 : 2; }
        return width;
    }

    private static string Pad(string text, int width)
    {
        int at = Width(text);
        return at >= width ? text : text + new string(' ', width - at);
    }

    private static string Right(string text, int width)
    {
        int at = Width(text);
        return at >= width ? text : new string(' ', width - at) + text;
    }

    // a value out of the data, shown on one line and only as far as it fits
    private static string OneLine(string text, int width)
    {
        text = text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");
        StringBuilder sb = new StringBuilder();
        int at = 0;
        foreach (char c in text)
        {
            int step = (c < 0x80 || c == 0xFF61 || (c >= 0xFF66 && c <= 0xFF9F)) ? 1 : 2;
            if (at + step > width) { sb.Append('…'); break; }
            sb.Append(c);
            at += step;
        }
        return sb.ToString();
    }
}
