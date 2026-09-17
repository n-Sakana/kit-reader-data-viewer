@echo off
chcp 65001 > nul
setlocal DisableDelayedExpansion
set "CSV_MASK_ARGS="
set "CSV_MASK_SELF=%~f0"

:collect
if "%~1"=="" goto run
set "CSV_MASK_ARGS=%CSV_MASK_ARGS%%~1|"
shift
goto collect

:run
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "try{$l=[IO.File]::ReadAllLines($env:CSV_MASK_SELF,[Text.Encoding]::UTF8);$i=[Array]::IndexOf($l,'//__CSHARP__');$s=[string]::Join([Environment]::NewLine,$l[($i+1)..($l.Length-1)]);$c=$false;try{$h=[BitConverter]::ToString([Security.Cryptography.SHA1]::Create().ComputeHash([Text.Encoding]::UTF8.GetBytes($s))).Replace('-','');$d=Join-Path $env:LOCALAPPDATA 'csv-mask';$p=Join-Path $d ($h+'.dll');if(-not(Test-Path -LiteralPath $p)){[void](New-Item -ItemType Directory -Force $d);Get-ChildItem -LiteralPath $d -Filter *.dll|Remove-Item -Force -ErrorAction SilentlyContinue;Add-Type -TypeDefinition $s -Language CSharp -OutputAssembly $p -OutputType Library};Add-Type -LiteralPath $p;$c=$true}catch{};if(-not $c){Add-Type -TypeDefinition $s -Language CSharp}}catch{Write-Host ('ERROR: csv-mask could not start. '+$_.Exception.Message) -ForegroundColor Red;Read-Host 'Press Enter to close'|Out-Null;exit 2};exit [CsvMask]::Run($env:CSV_MASK_ARGS)"
exit /b %ERRORLEVEL%

//__CSHARP__
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public static class CsvMask
{
    // ここに書いた見出しの列は置き換えない（判定や集計に使う、個人を指さない列）。
    // 見出しの前後の空白と全角半角の違いは無視して比べる。
    private static readonly string[] KeepColumns =
    {
        "【共通】ステータス", "【共通】取引結果", "【共通】結果コード", "【共通】金額",
        "決済ステータス", "決済金額", "請求方法", "支払い方法", "受付状態"
    };

    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Digit = "0123456789";
    private const string FullUpper = "ＡＢＣＤＥＦＧＨＩＪＫＬＭＮＯＰＱＲＳＴＵＶＷＸＹＺ";
    private const string FullLower = "ａｂｃｄｅｆｇｈｉｊｋｌｍｎｏｐｑｒｓｔｕｖｗｘｙｚ";
    private const string FullDigit = "０１２３４５６７８９";
    private const string Hiragana = "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん";
    private const string Katakana = "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン";
    private const string HalfKana = "ｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜｦﾝ";
    private const string Kanji =
        "一二三四五六七八九十百千万円口目耳手足力男女子人名年月日火水木金土山川田石竹糸貝車雨草虫犬" +
        "花先生学校本文字正早村町森林空気天王玉音赤青白立休見入出上下左右中大小東西南北春夏秋冬朝昼" +
        "夜時分間週今前後午内外国京市場所道店家室門戸里野原池海岸谷岩星雲風雪光色形点線角米麦茶肉魚" +
        "鳥馬牛羊毛羽頭顔首体心声歌絵紙刀弓矢船汽電話語読書記計算数番組会社工作活用切引止歩走来帰行";

    private static readonly Regex Shaped = new Regex(
        "(?<dt>(?<![0-9])(?:19|20)[0-9]{2}(?<sep>[/\\-.])[0-9]{1,2}\\k<sep>[0-9]{1,2}(?![0-9]))" +
        "|(?<d8>(?<![0-9])(?:19|20)[0-9]{6}(?![0-9]))" +
        "|(?<tm>(?<![0-9:])[0-9]{1,2}:[0-9]{2}(?::[0-9]{2})?(?![0-9:]))",
        RegexOptions.CultureInvariant);

    private sealed class InputInfo
    {
        public string Path;
        public Encoding Encoding;
        public int BomLength;
        public char Delimiter;
        public string[] Header;
        public bool[] Keep;
    }

    private sealed class Counts
    {
        public long Rows, Masked, Kept, Unchanged;
    }

    public static int Run(string packedArguments)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            List<InputInfo> inputs = ReadInputs(packedArguments);
            ShowPlan(inputs);

            Console.WriteLine();
            Console.WriteLine("合言葉を入れると、別の日に同じ合言葉で実行しても同じ値は同じ値に置き換わります。");
            Console.WriteLine("空のまま Enter なら、今回まとめて入れたファイルの間だけでそろえます。");
            Console.Write("合言葉（省略可）: ");
            string phrase = Console.ReadLine();
            if (phrase == null) return Cancel();

            Console.Write("この内容で置き換えたファイルを作ります。よろしいですか (y/N、0で中止): ");
            string answer = Console.ReadLine();
            if (answer == null || !string.Equals(Normalize(answer), "y", StringComparison.OrdinalIgnoreCase))
                return Cancel();

            byte[] key = MakeKey(phrase);
            string outputDirectory = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(inputs[0].Path), "masked_" + DateTime.Now.ToString("yyyyMMdd"));
            Directory.CreateDirectory(outputDirectory);

            Console.WriteLine();
            foreach (InputInfo input in inputs)
            {
                string outputPath = System.IO.Path.Combine(outputDirectory, System.IO.Path.GetFileName(input.Path));
                Counts counts = MaskFile(input, key, outputPath);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK: " + outputPath);
                Console.ResetColor();
                Console.WriteLine("  データ" + counts.Rows + "行 / 置き換えたセル " + counts.Masked +
                    " / そのまま残したセル " + counts.Kept);
                if (counts.Unchanged > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  注意: 置き換えの対象なのに元と同じ値になったセルが " + counts.Unchanged + " 件あります。目で確かめてください。");
                    Console.ResetColor();
                }
            }
            Console.WriteLine();
            Console.WriteLine("ファイル名は元のままです。名前に固有の番号が入っている場合は、渡す前に付け替えてください。");
            Pause();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("エラー: " + ex.Message);
            Console.ResetColor();
            Pause();
            return 1;
        }
    }

    private static int Cancel()
    {
        Console.WriteLine("中止しました。何も保存していません。");
        Pause();
        return 0;
    }

    private static void Pause()
    {
        Console.Write("Enterで終了");
        Console.ReadLine();
    }

    private static string Normalize(string value)
    {
        return value.Normalize(NormalizationForm.FormKC).Trim();
    }

    private static string NameKey(string value)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in Normalize(value))
            if (!char.IsWhiteSpace(c)) sb.Append(char.ToUpperInvariant(c));
        return sb.ToString();
    }

    private static byte[] MakeKey(string phrase)
    {
        string text = Normalize(phrase ?? string.Empty);
        if (text.Length == 0)
        {
            byte[] random = new byte[32];
            using (RandomNumberGenerator generator = RandomNumberGenerator.Create()) generator.GetBytes(random);
            return random;
        }
        using (SHA256 sha = SHA256.Create())
            return sha.ComputeHash(Encoding.UTF8.GetBytes("csv-mask:" + text));
    }

    // ---- 入力の読み取り -------------------------------------------------

    private static List<InputInfo> ReadInputs(string packedArguments)
    {
        string[] paths = string.IsNullOrEmpty(packedArguments)
            ? new string[0]
            : packedArguments.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        if (paths.Length == 0)
            throw new InvalidOperationException("対象ファイルがありません。CSV / TSV / TXT をこのBATへドラッグ＆ドロップしてください。つながりを保つには、関係するファイルをまとめて入れます。");

        HashSet<string> keep = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in KeepColumns) keep.Add(NameKey(name));

        List<InputInfo> inputs = new List<InputInfo>();
        foreach (string rawPath in paths)
        {
            string path = System.IO.Path.GetFullPath(rawPath);
            if (!File.Exists(path)) throw new FileNotFoundException("ファイルが見つかりません。", path);
            string extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".csv" && extension != ".tsv" && extension != ".txt")
                throw new InvalidOperationException("対象外の形式です（Excel は CSV にしてから入れてください）: " + System.IO.Path.GetFileName(path));

            byte[] bytes = File.ReadAllBytes(path);
            InputInfo info = new InputInfo();
            info.Path = path;
            info.Encoding = DetectEncoding(bytes, out info.BomLength);
            string text = info.Encoding.GetString(bytes, info.BomLength, bytes.Length - info.BomLength);
            info.Delimiter = DetectDelimiter(text);
            info.Header = ReadHeader(text, info.Delimiter);
            info.Keep = new bool[info.Header.Length];
            for (int i = 0; i < info.Header.Length; i++) info.Keep[i] = keep.Contains(NameKey(info.Header[i]));
            inputs.Add(info);
        }
        return inputs;
    }

    private static Encoding DetectEncoding(byte[] bytes, out int bomLength)
    {
        bomLength = 0;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) { bomLength = 3; return new UTF8Encoding(false, true); }
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) { bomLength = 2; return new UnicodeEncoding(false, false, true); }
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) { bomLength = 2; return new UnicodeEncoding(true, false, true); }
        try
        {
            UTF8Encoding utf8 = new UTF8Encoding(false, true);
            utf8.GetCharCount(bytes);
            return utf8;
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(932);
        }
    }

    private static char DetectDelimiter(string text)
    {
        foreach (string line in text.Split('\n'))
        {
            if (line.Trim().Length == 0) continue;
            int tabs = 0, commas = 0;
            foreach (char c in line) { if (c == '\t') tabs++; else if (c == ',') commas++; }
            if (tabs == 0 && commas == 0) return '\0';
            return tabs >= commas ? '\t' : ',';
        }
        throw new InvalidOperationException("中身が空のファイルがあります。");
    }

    private static string[] ReadHeader(string text, char delimiter)
    {
        List<string> fields = new List<string>();
        Walk(text, delimiter, delegate(int row, int column, string content, bool quoted) {
            if (row == 0) fields.Add(content);
            return content;
        }, null, 1);
        return fields.ToArray();
    }

    // ---- 置き換え -------------------------------------------------------

    private delegate string FieldHandler(int row, int column, string content, bool quoted);

    // 文字を順に追い、区切り・引用符・改行はそのまま書き出す。セルの中身だけ handler へ渡す。
    // row は空行を数えない（最初の空でない行が 0 = 見出し）。maxRows > 0 ならその行数で打ち切る。
    private static void Walk(string text, char delimiter, FieldHandler handler, StringBuilder output, int maxRows)
    {
        StringBuilder field = new StringBuilder();
        bool inQuotes = false, quoted = false, rowHasContent = false;
        int row = 0, column = 0;
        int i = 0;
        while (i <= text.Length)
        {
            bool end = i == text.Length;
            char c = end ? '\0' : text[i];

            if (!end && inQuotes)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i += 2; continue; }
                if (c == '"') { inQuotes = false; i++; continue; }
                field.Append(c); i++; continue;
            }
            if (!end && c == '"' && field.Length == 0 && !quoted) { inQuotes = true; quoted = true; rowHasContent = true; i++; continue; }

            bool isDelimiter = !end && delimiter != '\0' && c == delimiter;
            bool isBreak = !end && (c == '\r' || c == '\n');
            if (!end && !isDelimiter && !isBreak) { field.Append(c); rowHasContent = true; i++; continue; }

            // セルの終わり
            if (rowHasContent || isDelimiter || field.Length > 0)
            {
                string content = field.ToString();
                string result = handler(row, column, content, quoted);
                if (output != null)
                {
                    if (quoted) output.Append('"').Append(result.Replace("\"", "\"\"")).Append('"');
                    else output.Append(result);
                }
                rowHasContent = true;
            }
            field.Length = 0;
            quoted = false;

            if (end) break;
            if (isDelimiter) { if (output != null) output.Append(c); column++; i++; continue; }

            // 改行。CRLF はまとめて書く。
            if (output != null) output.Append(c);
            i++;
            if (c == '\r' && i < text.Length && text[i] == '\n') { if (output != null) output.Append('\n'); i++; }
            if (rowHasContent)
            {
                row++;
                if (maxRows > 0 && row >= maxRows) return;
            }
            rowHasContent = false;
            column = 0;
        }
    }

    private static Counts MaskFile(InputInfo input, byte[] key, string outputPath)
    {
        byte[] bytes = File.ReadAllBytes(input.Path);
        string text = input.Encoding.GetString(bytes, input.BomLength, bytes.Length - input.BomLength);
        StringBuilder output = new StringBuilder(text.Length + 16);
        Counts counts = new Counts();
        int lastRow = 0;

        using (HMACSHA256 hmac = new HMACSHA256(key))
        {
            Walk(text, input.Delimiter, delegate(int row, int column, string content, bool quoted) {
                if (row == 0) return content;
                lastRow = row;
                if (column < input.Keep.Length && input.Keep[column]) { counts.Kept++; return content; }
                string masked = MaskValue(content, hmac);
                if (HasMaskable(content))
                {
                    counts.Masked++;
                    if (masked == content) counts.Unchanged++;
                }
                return masked;
            }, output, 0);
        }
        counts.Rows = lastRow;

        string temporaryPath = outputPath + ".tmp";
        try
        {
            using (FileStream stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write))
            {
                stream.Write(bytes, 0, input.BomLength); // BOM は元のまま
                byte[] body = input.Encoding.GetBytes(output.ToString());
                stream.Write(body, 0, body.Length);
            }
            if (File.Exists(outputPath)) File.Delete(outputPath);
            File.Move(temporaryPath, outputPath);
        }
        catch
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            throw;
        }
        return counts;
    }

    private static bool HasMaskable(string value)
    {
        foreach (char c in value) if (ClassOf(c) != null) return true;
        return false;
    }

    // 日付と時刻は、形と桁を保ったまま実在する別の日付・時刻へ。残りは文字の種類ごとのかたまりで置き換える。
    private static string MaskValue(string value, HMACSHA256 hmac)
    {
        if (value.Length == 0) return value;
        StringBuilder result = new StringBuilder(value.Length);
        int position = 0;
        foreach (Match match in Shaped.Matches(value))
        {
            string replaced = MaskShaped(match, hmac);
            if (replaced == null) continue; // 日付として成り立たない 8 桁は、ふつうの数字として扱う
            result.Append(MaskRuns(value.Substring(position, match.Index - position), hmac));
            result.Append(replaced);
            position = match.Index + match.Length;
        }
        result.Append(MaskRuns(value.Substring(position), hmac));
        return result.ToString();
    }

    private static string MaskShaped(Match match, HMACSHA256 hmac)
    {
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes("shape:" + match.Value));
        if (match.Groups["tm"].Success)
        {
            string[] parts = match.Value.Split(':');
            int hourValue, minuteValue;
            if (!int.TryParse(parts[0], out hourValue) || hourValue > 23) return null;
            if (!int.TryParse(parts[1], out minuteValue) || minuteValue > 59) return null;
            string hour = parts[0].Length == 1 ? (hash[0] % 10).ToString() : (hash[0] % 24).ToString("00");
            string time = hour + ":" + (hash[1] % 60).ToString("00");
            if (parts.Length == 3) time += ":" + (hash[2] % 60).ToString("00");
            return time;
        }

        string yearText, monthText, dayText, separator;
        if (match.Groups["dt"].Success)
        {
            separator = match.Groups["sep"].Value;
            string[] parts = match.Value.Split(separator[0]);
            yearText = parts[0]; monthText = parts[1]; dayText = parts[2];
        }
        else
        {
            separator = string.Empty;
            yearText = match.Value.Substring(0, 4); monthText = match.Value.Substring(4, 2); dayText = match.Value.Substring(6, 2);
        }
        DateTime parsed;
        if (!DateTime.TryParseExact(yearText + "-" + monthText.PadLeft(2, '0') + "-" + dayText.PadLeft(2, '0'),
                "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            return null;

        // 年はそのまま、月日だけ変える。桁数が変わらないよう、1 桁で書かれた月日は 1〜9 から選ぶ。
        string month = monthText.Length == 1 ? (1 + hash[0] % 9).ToString() : (1 + hash[0] % 12).ToString("00");
        string day = dayText.Length == 1 ? (1 + hash[1] % 9).ToString() : (1 + hash[1] % 28).ToString("00");
        if (month == monthText && day == dayText) day = dayText.Length == 1 ? (1 + (hash[1] + 1) % 9).ToString() : (1 + (hash[1] + 1) % 28).ToString("00");
        return yearText + separator + month + separator + day;
    }

    private static string ClassOf(char c)
    {
        if (c >= 'A' && c <= 'Z') return Upper;
        if (c >= 'a' && c <= 'z') return Lower;
        if (c >= '0' && c <= '9') return Digit;
        if (c >= 'Ａ' && c <= 'Ｚ') return FullUpper;
        if (c >= 'ａ' && c <= 'ｚ') return FullLower;
        if (c >= '０' && c <= '９') return FullDigit;
        if (c >= 'ぁ' && c <= 'ゖ') return Hiragana;
        if (c >= 'ァ' && c <= 'ヺ') return Katakana;      // 長音「ー」と中点は記号として残す
        if (c >= 'ｦ' && c <= 'ﾝ' && c != 'ｰ') return HalfKana;
        if (c >= '一' && c <= '鿿') return Kanji;
        if (c >= '㐀' && c <= '䶿') return Kanji;
        return null;
    }

    private static string MaskRuns(string text, HMACSHA256 hmac)
    {
        if (text.Length == 0) return text;
        StringBuilder result = new StringBuilder(text.Length);
        int start = 0;
        while (start < text.Length)
        {
            string alphabet = ClassOf(text[start]);
            int end = start + 1;
            while (end < text.Length && (object)ClassOf(text[end]) == (object)alphabet) end++;
            string run = text.Substring(start, end - start);
            result.Append(alphabet == null ? run : MaskRun(run, alphabet, hmac));
            start = end;
        }
        return result.ToString();
    }

    // 同じかたまりは、どのファイルのどの列でも同じ結果になる（識別番号などのつながりを保つ）。
    private static string MaskRun(string run, string alphabet, HMACSHA256 hmac)
    {
        char[] result = new char[run.Length];
        byte[] block = null;
        int used = 0, counter = 0;
        for (int i = 0; i < run.Length; i++)
        {
            if (block == null || used == block.Length)
            {
                block = hmac.ComputeHash(Encoding.UTF8.GetBytes(alphabet[0] + ":" + counter + ":" + run));
                counter++;
                used = 0;
            }
            result[i] = alphabet[block[used++] % alphabet.Length];
        }
        string masked = new string(result);
        if (masked == run)
        {
            int index = alphabet.IndexOf(result[0]);
            result[0] = alphabet[(index + 1) % alphabet.Length];
            masked = new string(result);
        }
        return masked;
    }

    // ---- 表示 -----------------------------------------------------------

    private static void ShowPlan(List<InputInfo> inputs)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("対象: " + inputs.Count + "件（文字の種類と長さ、区切り、文字コードはそのまま）");
        Console.ResetColor();
        foreach (InputInfo input in inputs)
        {
            string delimiter = input.Delimiter == '\t' ? "タブ区切り" : input.Delimiter == ',' ? "カンマ区切り" : "区切りなし";
            Console.WriteLine("  - " + System.IO.Path.GetFileName(input.Path) + " [" + delimiter + " / " +
                input.Encoding.WebName + "] " + input.Header.Length + "列");
            List<string> kept = new List<string>();
            for (int i = 0; i < input.Header.Length; i++) if (input.Keep[i]) kept.Add(input.Header[i].Trim());
            Console.WriteLine("      そのまま残す列: " + (kept.Count == 0 ? "なし" : string.Join(" / ", kept.ToArray())));
            Console.WriteLine("      それ以外の " + (input.Header.Length - kept.Count) + " 列は置き換えます（見出しの行はそのまま）。");
        }
        Console.WriteLine();
        Console.WriteLine("残す列を変えたいときは、このBATをメモ帳で開き、KeepColumns の一覧を書き換えます。");
    }
}
