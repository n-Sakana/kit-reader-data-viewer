// CSV fallback for quoting/multiline fields and non-byte-compatible encodings.
// The ordinary unquoted UTF-8/Shift-JIS/single-byte path remains in Rdv3Core.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public static class Rdv3Csv
{
    public static bool NeedsDecoded(byte[] bytes, Encoding encoding)
    {
        int cp = encoding.CodePage;
        if (!encoding.IsSingleByte && cp != 65001 && cp != 932) { return true; }
        if (bytes.Length >= 2 && ((bytes[0] == 255 && bytes[1] == 254) || (bytes[0] == 254 && bytes[1] == 255))) { return true; }
        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191 && cp != 65001) { return true; }
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == (byte)'"' || (bytes[i] == 13 && (i + 1 == bytes.Length || bytes[i + 1] != 10))) { return true; }
        }
        return false;
    }

    // The encoding a byte-order mark declares, or null without one.
    public static Encoding BomEncoding(byte[] bytes, out int skip)
    {
        skip = 0;
        if (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 254 && bytes[2] == 0 && bytes[3] == 0) { skip = 4; return Encoding.GetEncoding(12000); }
        if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 254 && bytes[3] == 255) { skip = 4; return Encoding.GetEncoding(12001); }
        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191) { skip = 3; return new UTF8Encoding(false); }
        if (bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254) { skip = 2; return new UnicodeEncoding(false, false); }
        if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255) { skip = 2; return new UnicodeEncoding(true, false); }
        return null;
    }

    private static bool DecodesStrictly(byte[] bytes, int skip, Encoding encoding)
    {
        Encoding strict = (Encoding)encoding.Clone();
        strict.DecoderFallback = DecoderFallback.ExceptionFallback;
        try { strict.GetCharCount(bytes, skip, bytes.Length - skip); return true; }
        catch (DecoderFallbackException) { return false; }
    }

    private static Encoding SafeEncoding(int codePage)
    {
        try { return Encoding.GetEncoding(codePage); }
        catch (Exception) { return null; }
    }

    // The encoding the file is read with. A byte-order mark decides; without
    // one the configured encoding is tried strictly, then UTF-8, then
    // Shift_JIS. A file that fits none is read with the configured encoding,
    // whose strict read then names the offending bytes. Any departure from
    // the configured value is noted so the absorption is never silent.
    public static Encoding ResolveEncoding(byte[] bytes, Encoding configured, Rdv3InputCounts counts, string path)
    {
        int skip;
        Encoding bom = BomEncoding(bytes, out skip);
        if (bom != null)
        {
            if (bom.CodePage == configured.CodePage) { return configured; }
            if (counts != null) { counts.Note(path, Rdv3Text.InputEncodingBom.Replace("{encoding}", bom.WebName)); }
            return bom;
        }
        if (DecodesStrictly(bytes, 0, configured)) { return configured; }
        Encoding[] candidates = { new UTF8Encoding(false), SafeEncoding(932) };
        for (int i = 0; i < candidates.Length; i++)
        {
            Encoding candidate = candidates[i];
            if (candidate == null || candidate.CodePage == configured.CodePage) { continue; }
            if (!DecodesStrictly(bytes, 0, candidate)) { continue; }
            if (counts != null)
            { counts.Note(path, Rdv3Text.InputEncodingFallback.Replace("{configured}", configured.WebName).Replace("{encoding}", candidate.WebName)); }
            return candidate;
        }
        return configured;
    }

    private static string HeaderLine(byte[] bytes, Encoding encoding, int headerRow)
    {
        int skip;
        BomEncoding(bytes, out skip);
        int length = Math.Min(bytes.Length - skip, 262144);
        if (length <= 0) { return null; }
        string text;
        try { text = encoding.GetString(bytes, skip, length); }
        catch (Exception) { return null; }
        string[] lines = text.Split('\n');
        if (headerRow < 1 || headerRow > lines.Length) { return null; }
        return lines[headerRow - 1].TrimEnd('\r');
    }

    private static int CountOutsideQuotes(string line, char c)
    {
        int n = 0;
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"') { quoted = !quoted; }
            else if (line[i] == c && !quoted) { n++; }
        }
        return n;
    }

    public static string DelimiterName(char delimiter)
    {
        if (delimiter == '\t') { return "tab"; }
        if (delimiter == ',') { return "comma"; }
        if (delimiter == ';') { return "semicolon"; }
        if (delimiter == '|') { return "pipe"; }
        return delimiter.ToString();
    }

    // The field separator: the configured one when the header line holds it,
    // otherwise the most frequent of tab / comma / semicolon / pipe there. A
    // departure from the configured value is noted, never silent.
    public static char ResolveDelimiter(byte[] bytes, Encoding encoding, char configured, int headerRow, Rdv3InputCounts counts, string path)
    {
        string header = HeaderLine(bytes, encoding, headerRow);
        if (header == null || CountOutsideQuotes(header, configured) > 0) { return configured; }
        char[] candidates = { '\t', ',', ';', '|' };
        char best = configured;
        int bestCount = 0;
        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i] == configured) { continue; }
            int n = CountOutsideQuotes(header, candidates[i]);
            if (n > bestCount) { best = candidates[i]; bestCount = n; }
        }
        if (bestCount == 0) { return configured; }
        if (counts != null)
        { counts.Note(path, Rdv3Text.InputDelimiterDetected.Replace("{configured}", DelimiterName(configured)).Replace("{delimiter}", DelimiterName(best))); }
        return best;
    }

    // headerRow: the physical line that holds the header (1 = the first line).
    // Report exports often put a title and a print date above the header;
    // those lines are skipped without being counted as short or blank rows.
    // delimiter: the field separator; a tab for an Excel "Unicode text" export.
    public static void Read(string path, Encoding encoding, bool headOnly,
                            out string[] head, out string[][] rows, out int[] rowNumbers, string encodingSetting = "data.encoding",
                            HashSet<string> references = null, Rdv3InputCounts counts = null, int headerRow = 1, char delimiter = ',')
    {
        Read(path, File.ReadAllBytes(path), encoding, headOnly, out head, out rows, out rowNumbers, encodingSetting, references, counts, headerRow, delimiter);
    }

    public static void Read(string path, byte[] bytes, Encoding encoding, bool headOnly,
                            out string[] head, out string[][] rows, out int[] rowNumbers, string encodingSetting = "data.encoding",
                            HashSet<string> references = null, Rdv3InputCounts counts = null, int headerRow = 1, char delimiter = ',')
    {
        head = null;
        if (counts == null) { counts = new Rdv3InputCounts(); }
        if (headerRow > 1) { counts.HeaderOffset = headerRow - 1; }
        Rdv3InputColumns columns = null;
        List<string[]> data = new List<string[]>();
        List<int> numbers = new List<int>();
        Encoding strict = (Encoding)encoding.Clone();
        strict.DecoderFallback = DecoderFallback.ExceptionFallback;
        using (MemoryStream stream = new MemoryStream(bytes, false))
        {
            byte[] bom = new byte[4];
            int n = stream.Read(bom, 0, bom.Length);
            int cp = 0, skip = 0;
            if (n >= 4 && bom[0] == 255 && bom[1] == 254 && bom[2] == 0 && bom[3] == 0) { cp = 12000; skip = 4; }
            else if (n >= 4 && bom[0] == 0 && bom[1] == 0 && bom[2] == 254 && bom[3] == 255) { cp = 12001; skip = 4; }
            else if (n >= 3 && bom[0] == 239 && bom[1] == 187 && bom[2] == 191) { cp = 65001; skip = 3; }
            else if (n >= 2 && bom[0] == 255 && bom[1] == 254) { cp = 1200; skip = 2; }
            else if (n >= 2 && bom[0] == 254 && bom[1] == 255) { cp = 1201; skip = 2; }
            if (cp != 0 && cp != encoding.CodePage)
            { throw Rdv3Input.Error(path, 1, "BOM", encoding.WebName, Encoding.GetEncoding(cp).WebName,
                Rdv3Text.InputFixEncoding.Replace("{encoding}", Encoding.GetEncoding(cp).WebName).Replace("{setting}", encodingSetting)); }
            stream.Position = skip;
            using (StreamReader reader = new StreamReader(stream, strict, false, 65536))
            {
                int physical = 1;
                while (true)
                {
                    int first = physical;
                    bool blank;
                    string[] cells;
                    try { cells = Record(reader, path, ref physical, out blank, delimiter); }
                    catch (DecoderFallbackException)
                    {
                        // Locate the invalid byte in the whole file; a decoder's
                        // buffered read may run ahead of this record.
                        Rdv3Input.ValidateEncoding(bytes, encoding, path, encodingSetting, delimiter);
                        throw;
                    }
                    if (cells == null) { break; }
                    if (first < headerRow) { continue; }
                    if (blank) { counts.Shape(path, first, 0, head == null ? 0 : columns.SourceCount, true); continue; }
                    if (head == null)
                    {
                        head = cells;
                        for (int i = 0; i < head.Length; i++)
                        {
                            head[i] = head[i].Trim();
                            for (int k = 0; k < head[i].Length; k++)
                            {
                                if (head[i][k] >= ' ') { continue; }
                                // A tab inside a header cell of a comma-split line is almost
                                // always a tab-separated file: say so instead of "duplicate header".
                                string fix = (head[i][k] == '\t' && delimiter != '\t' && head.Length == 1)
                                    ? Rdv3Text.InputFixTab : Rdv3Text.InputFixHeader;
                                throw Failure(path, first, i + 1, Rdv3Text.InputExpectHeader, head[i], fix);
                            }
                        }
                        columns = Rdv3InputColumns.Read(head, path, first, references, counts);
                        head = columns.Head;
                        if (headOnly) { break; }
                    }
                    else
                    {
                        if (cells.Length < columns.SourceCount) { counts.Shape(path, first, cells.Length, columns.SourceCount, false); continue; }
                        if (cells.Length > columns.SourceCount)
                        { throw Failure(path, first, columns.SourceCount + 1,
                            Rdv3Text.InputColumnCount.Replace("{n}", columns.SourceCount.ToString(CultureInfo.InvariantCulture)),
                            Rdv3Text.InputColumnCount.Replace("{n}", cells.Length.ToString(CultureInfo.InvariantCulture)), Rdv3Text.InputFixCsv); }
                        data.Add(columns.Project(cells));
                        numbers.Add(first);
                    }
                }
            }
        }
        if (head == null) { throw Failure(path, 1, 1, Rdv3Text.InputExpectHeader, "", Rdv3Text.InputFixHeader); }
        rows = data.ToArray();
        rowNumbers = numbers.ToArray();
    }

    private static string[] Record(TextReader reader, string path, ref int line, out bool blank, char delimiter)
    {
        blank = true;
        List<string> cells = new List<string>();
        StringBuilder field = new StringBuilder();
        bool quoted = false, afterQuote = false, any = false;
        int recordLine = line;
        while (true)
        {
            int read = reader.Read();
            if (read < 0)
            {
                if (quoted) { throw Failure(path, recordLine, cells.Count + 1, Rdv3Text.InputExpectQuote, "EOF", Rdv3Text.InputFixCsv); }
                if (!any) { return null; }
                cells.Add(field.ToString());
                return cells.ToArray();
            }
            char ch = (char)read;
            any = true;
            if (quoted)
            {
                if (ch == '"')
                {
                    if (reader.Peek() == '"') { reader.Read(); field.Append('"'); }
                    else { quoted = false; afterQuote = true; }
                }
                else
                {
                    field.Append(ch);
                    if (ch == '\r')
                    {
                        if (reader.Peek() == '\n') { reader.Read(); field.Append('\n'); }
                        line++;
                    }
                    else if (ch == '\n') { line++; }
                }
                continue;
            }
            if (ch == '\r' || ch == '\n')
            {
                if (ch == '\r' && reader.Peek() == '\n') { reader.Read(); }
                line++;
                cells.Add(field.ToString());
                return cells.ToArray();
            }
            blank = false;
            if (ch == delimiter) { cells.Add(field.ToString()); field.Length = 0; afterQuote = false; continue; }
            if (afterQuote && Rdv3Input.IsPadding(ch)) { continue; }
            if (afterQuote) { throw Failure(path, recordLine, cells.Count + 1, Rdv3Text.InputExpectDelimiter, ch.ToString(), Rdv3Text.InputFixCsv); }
            if (ch == '"')
            {
                if (field.Length != 0) { throw Failure(path, recordLine, cells.Count + 1, Rdv3Text.InputExpectQuote, field.ToString() + ch, Rdv3Text.InputFixCsv); }
                quoted = true;
            }
            else { field.Append(ch); }
        }
    }

    private static Rdv3DataError Failure(string path, int line, int column, string expected, string actual, string fix)
    { return Rdv3Input.Error(path, line, column.ToString(CultureInfo.InvariantCulture), expected, actual, fix); }
}
