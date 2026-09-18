using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// A value error belongs to one record. Only this exception is caught at row
// boundaries; malformed definitions, files and persisted ledgers still stop.
public sealed class Rdv3RecordError : Exception
{
    public Rdv3RecordError(string message) : base(message) { }
}

public sealed class Rdv3InputCounts
{
    public int ShortRows, BlankRows, HeaderColumns, HeaderOffset;
    public int InvalidRows;
    public readonly List<int> SourceRows = new List<int>();
    public readonly List<string> RowWarnings = new List<string>();
    // What the reader absorbed on its own (a BOM that decided the encoding, a
    // tab-separated file, a header spelled with other width characters). Not
    // an exclusion: reported to the log so the absorption is never silent,
    // but never raised as an error dialog.
    public readonly List<string> Notes = new List<string>();
    // the workbook's date system: serial dates count from 1904-01-01 when set
    public bool Date1904;
    public readonly List<string> DuplicateHeaders = new List<string>();
    // ヘッダー行の空セル。使わない列なら落として続ける (件数だけ警告に出す)。
    public int BlankHeaders;

    public void Exclude(string path, int row, string reason)
    {
        InvalidRows++;
        RowWarnings.Add(Rdv3Text.Format(Rdv3Text.RecordExcluded,
            Rdv3Text.Format(Rdv3Text.SourceRow, System.IO.Path.GetFileName(path), row), reason));
    }

    public void Shape(string path, int row, int actual, int expected, bool blank)
    {
        if (blank) { BlankRows++; } else { ShortRows++; }
        RowWarnings.Add(Rdv3Text.Format(Rdv3Text.RecordExcluded,
            Rdv3Text.Format(Rdv3Text.SourceRow, System.IO.Path.GetFileName(path), row),
            Rdv3Text.Format(Rdv3Text.RecordColumns, expected, actual)));
    }

    public void Note(string path, string text)
    {
        string line = System.IO.Path.GetFileName(path) + ": " + text;
        if (!Notes.Contains(line)) { Notes.Add(line); }
    }

    public void AddWarnings(string path, List<string> warnings)
    {
        string file = System.IO.Path.GetFileName(path);
        if (HeaderOffset > 0)
        { warnings.Add(Rdv3Text.InputHeaderOffset.Replace("{file}", file)
            .Replace("{n}", HeaderOffset.ToString(CultureInfo.InvariantCulture))); }
        if (ShortRows > 0 || BlankRows > 0)
        { warnings.Add(Rdv3Text.InputShapeSkipped.Replace("{file}", file)
            .Replace("{short}", ShortRows.ToString(CultureInfo.InvariantCulture))
            .Replace("{blank}", BlankRows.ToString(CultureInfo.InvariantCulture))); }
        if (HeaderColumns > 0)
        { warnings.Add(Rdv3Text.InputHeadersSkipped.Replace("{file}", file)
            .Replace("{n}", HeaderColumns.ToString(CultureInfo.InvariantCulture))
            .Replace("{names}", string.Join(" / ", DuplicateHeaders.ToArray()))); }
        warnings.AddRange(RowWarnings);
    }
}

// Drop the entire unreferenced name group, never choose the first or the empty
// member of an ambiguous group. Keep source positions until row shape is checked.
public sealed class Rdv3InputColumns
{
    public string[] Head;
    public int SourceCount;
    private int[] kept;

    public static Rdv3InputColumns Read(string[] head, string path, int row,
        HashSet<string> references, Rdv3InputCounts counts)
    {
        for (int i = 0; i < head.Length; i++) { head[i] = head[i].Trim(); }
        // A header spelled with other width characters, extra spaces or a
        // different case than the definition is the definition's column: read
        // it under the configured name. Only names the definition refers to
        // are renamed, and only when no header carries the exact name already.
        if (references != null)
        {
            HashSet<string> exact = new HashSet<string>(head, StringComparer.Ordinal);
            Dictionary<string, string> wanted = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string reference in references)
            {
                if (reference.Length == 0 || exact.Contains(reference)) { continue; }
                string key = Rdv3Input.NameKey(reference);
                if (key.Length > 0 && !wanted.ContainsKey(key)) { wanted.Add(key, reference); }
            }
            for (int i = 0; i < head.Length && wanted.Count > 0; i++)
            {
                if (head[i].Length == 0) { continue; }
                string alias;
                if (!wanted.TryGetValue(Rdv3Input.NameKey(head[i]), out alias)) { continue; }
                counts.Note(path, Rdv3Text.InputHeaderAlias.Replace("{actual}", head[i]).Replace("{name}", alias));
                head[i] = alias;
            }
        }
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> duplicates = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < head.Length; i++)
        {
            // 名前の無い列は設定から参照できないので、落として続ける。
            // Excel の表は区切りに空列を挟むことがあり、そこで止めると
            // 使っていない列のせいでファイル全体が読めなくなる。
            if (head[i].Length == 0)
            { counts.BlankHeaders++; continue; }
            if (!seen.Add(head[i]))
            {
                if (references == null || references.Contains(head[i]))
                { throw Rdv3Input.Error(path, row, (i + 1).ToString(CultureInfo.InvariantCulture),
                    Rdv3Text.InputExpectHeader, head[i], Rdv3Text.InputFixHeader); }
                if (duplicates.Add(head[i])) { counts.DuplicateHeaders.Add(head[i]); }
            }
        }
        List<int> positions = new List<int>();
        List<string> names = new List<string>();
        for (int i = 0; i < head.Length; i++)
        {
            if (head[i].Length == 0) { counts.HeaderColumns++; continue; }
            if (duplicates.Contains(head[i])) { counts.HeaderColumns++; continue; }
            positions.Add(i); names.Add(head[i]);
        }
        Rdv3InputColumns result = new Rdv3InputColumns();
        result.SourceCount = head.Length; result.Head = names.ToArray();
        result.kept = positions.ToArray();
        return result;
    }

    public string[] Project(string[] cells)
    {
        if (kept.Length == SourceCount) { return cells; }
        string[] result = new string[kept.Length];
        for (int i = 0; i < kept.Length; i++) { result[i] = cells[kept[i]]; }
        return result;
    }
}

// Normalize imported cells and lookup input, never persisted ledger/pending
// snapshots: changing those would invalidate their conflict baselines.
public static class Rdv3Input
{
    public static void ValidateEncoding(byte[] bytes, Encoding encoding, string path, string encodingSetting = "data.encoding",
                                        char delimiter = ',')
    {
        Encoding strict = (Encoding)encoding.Clone();
        strict.DecoderFallback = DecoderFallback.ExceptionFallback;
        try { strict.GetCharCount(bytes); }
        catch (DecoderFallbackException ex)
        {
            int stop = Math.Max(0, Math.Min(ex.Index, bytes.Length));
            string prefix = encoding.GetString(bytes, 0, stop);
            int row = 1, column = 1;
            bool quoted = false;
            for (int i = 0; i < prefix.Length; i++)
            {
                char c = prefix[i];
                if (c == '"') { quoted = !quoted; }
                else if (c == delimiter && !quoted) { column++; }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && i + 1 < prefix.Length && prefix[i + 1] == '\n') { i++; }
                    row++;
                    if (!quoted) { column = 1; }
                }
            }
            throw Error(path, row, column.ToString(CultureInfo.InvariantCulture), encoding.WebName,
                "bytes " + BitConverter.ToString(ex.BytesUnknown), Rdv3Text.InputFixUnknownEncoding.Replace("{setting}", encodingSetting)
                + EncodingHint(bytes, encodingSetting));
        }
    }

    // A byte pattern is evidence for a suggestion, never permission to decode
    // with a different encoding or silently change the configured table.
    private static string EncodingHint(byte[] bytes, string setting)
    {
        if (bytes.Length < 16 || bytes.Length % 2 != 0) { return ""; }
        if ((bytes[0] == 255 && bytes[1] == 254) || (bytes[0] == 254 && bytes[1] == 255)
            || (bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)) { return ""; }
        int length = Math.Min(bytes.Length, 8192), even = 0, odd = 0;
        for (int i = 0; i < length; i += 2)
        { if (bytes[i] == 0) { even++; } if (bytes[i + 1] == 0) { odd++; } }
        bool little = odd >= 4 && odd >= (even + 1) * 4;
        bool big = even >= 4 && even >= (odd + 1) * 4;
        if (!little && !big) { return ""; }
        try
        {
            Encoding candidate = new UnicodeEncoding(big, false, true);
            candidate.GetCharCount(bytes);
            // Do not cut a surrogate pair at the end of the sampled prefix.
            int last = big ? (bytes[length - 2] << 8) | bytes[length - 1] : (bytes[length - 1] << 8) | bytes[length - 2];
            if (last >= 0xd800 && last <= 0xdbff) { length -= 2; }
            string sample = candidate.GetString(bytes, 0, length);
            if (sample.IndexOf(',') < 0 || (sample.IndexOf('\n') < 0 && sample.IndexOf('\r') < 0)) { return ""; }
            foreach (char c in sample) { if (c < ' ' && c != '\r' && c != '\n' && c != '\t') { return ""; } }
            return " Possible " + (little ? "UTF-16LE" : "UTF-16BE")
                + " without BOM (alternating zero bytes and valid UTF-16 CSV text). Confirm the source encoding and set "
                + setting + " to \"" + (little ? "utf-16" : "utf-16BE") + "\". The encoding was not changed automatically.";
        }
        catch (DecoderFallbackException) { return ""; }
    }

    public static Rdv3DataError Error(string file, int row, string column, string expected, string actual, string fix)
    {
        return new Rdv3DataError(Rdv3Text.InputError.Replace("{file}", System.IO.Path.GetFileName(file))
            .Replace("{row}", row.ToString(CultureInfo.InvariantCulture)).Replace("{column}", column)
            .Replace("{expected}", expected).Replace("{actual}", Display(actual)).Replace("{fix}", fix));
    }

    public static string Display(string value)
    {
        StringBuilder result = new StringBuilder();
        foreach (char c in value ?? "")
        { if (char.IsControl(c)) { result.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture)); } else { result.Append(c); } }
        return result.ToString();
    }

    public static bool IsPadding(char c)
    { return c == ' ' || c == '　' || c == ' ' || c == '\t'; }

    // An imported cell: padding on either side is dropped and full-width
    // digits become ASCII digits. Letters keep their width and case here;
    // matching keys fold them separately (Fold, SearchKey).
    public static string Cell(string value)
    {
        if (string.IsNullOrEmpty(value)) { return value ?? ""; }
        int end = value.Length;
        while (end > 0 && IsPadding(value[end - 1])) { end--; }
        int start = 0;
        while (start < end && IsPadding(value[start])) { start++; }
        char[] changed = null;
        for (int i = start; i < end; i++)
        {
            char c = value[i];
            if (c < '０' || c > '９') { continue; }
            if (changed == null) { changed = value.Substring(start, end - start).ToCharArray(); }
            changed[i - start] = (char)('0' + c - '０');
        }
        if (changed != null) { return new string(changed); }
        return (start == 0 && end == value.Length) ? value : value.Substring(start, end - start);
    }

    // Representation folding for values that are compared or pattern-matched:
    // full-width ASCII letters, digits and punctuation become their ASCII
    // forms, the hyphen variants become '-', and padding is dropped. The
    // characters themselves (kanji, kana, case) are not changed.
    public static string Fold(string value)
    {
        string text = Cell(value);
        if (text.Length == 0) { return text; }
        char[] changed = null;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            char folded = c;
            if (c >= '！' && c <= '～') { folded = (char)(c - 0xfee0); }
            // The katakana long vowel mark (U+30FC) is a letter of a station
            // name, not a hyphen, and is deliberately left alone.
            else if (c == '‐' || c == '‑' || c == '‒' || c == '–' || c == '—'
                     || c == '―' || c == '−') { folded = '-'; }
            else if (c == '　') { folded = ' '; }
            if (folded == c) { continue; }
            if (changed == null) { changed = text.ToCharArray(); }
            changed[i] = folded;
        }
        return changed == null ? text : new string(changed);
    }

    // The typed, watched or indexed search key: folded and, for ASCII letters,
    // upper-cased, so a number typed in lower case or full width still finds
    // the record the systems wrote in upper-case ASCII.
    public static string SearchKey(string value)
    {
        string text = Fold(value);
        if (text.Length == 0) { return text; }
        char[] changed = null;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c < 'a' || c > 'z') { continue; }
            if (changed == null) { changed = text.ToCharArray(); }
            changed[i] = (char)(c - 32);
        }
        return changed == null ? text : new string(changed);
    }

    // The comparison key of a column name or a file name: folded, without any
    // white space, and case-insensitive for ASCII. Two names with the same
    // NameKey are the same name written differently.
    public static string NameKey(string value)
    {
        string text = SearchKey(value);
        if (text.Length == 0) { return text; }
        StringBuilder sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c) || c == ' ') { continue; }
            if (c == '_' || c == '＿') { c = '_'; }
            sb.Append(c);
        }
        return sb.ToString();
    }

    public static bool TryNumber(string text, out decimal value)
    {
        text = Cell(text).Trim().Replace('，', ',').Replace('．', '.')
            .Replace('－', '-').Replace('＋', '+').Replace('（', '(').Replace('）', ')');
        bool negative = text.Length >= 2 && text[0] == '(' && text[text.Length - 1] == ')';
        if (negative) { text = text.Substring(1, text.Length - 2).Trim(); }
        if (text.StartsWith("¥", StringComparison.Ordinal) || text.StartsWith("￥", StringComparison.Ordinal))
        { text = text.Substring(1).TrimStart(); }
        if (negative && (text.StartsWith("-", StringComparison.Ordinal) || text.StartsWith("+", StringComparison.Ordinal)))
        { value = 0; return false; }
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)) { return false; }
        if (negative) { value = -value; }
        return true;
    }
}
