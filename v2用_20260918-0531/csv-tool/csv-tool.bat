@echo off
chcp 65001 > nul
setlocal DisableDelayedExpansion
set "CSV_TOOL_ARGS="
set "CSV_TOOL_SELF=%~f0"

:collect
if "%~1"=="" goto run
set "CSV_TOOL_ARGS=%CSV_TOOL_ARGS%%~1|"
shift
goto collect

:run
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "try{$l=[IO.File]::ReadAllLines($env:CSV_TOOL_SELF,[Text.Encoding]::ASCII);$i=[Array]::IndexOf($l,'//__CSHARP__');$s=[string]::Join([Environment]::NewLine,$l[($i+1)..($l.Length-1)]);$c=$false;try{$h=[BitConverter]::ToString([Security.Cryptography.SHA1]::Create().ComputeHash([Text.Encoding]::ASCII.GetBytes($s))).Replace('-','');$d=Join-Path $env:LOCALAPPDATA 'csv-tool';$p=Join-Path $d ($h+'.dll');if(-not(Test-Path -LiteralPath $p)){[void](New-Item -ItemType Directory -Force $d);Get-ChildItem -LiteralPath $d -Filter *.dll|Remove-Item -Force -ErrorAction SilentlyContinue;Add-Type -TypeDefinition $s -Language CSharp -OutputAssembly $p -OutputType Library};Add-Type -LiteralPath $p;$c=$true}catch{};if(-not $c){Add-Type -TypeDefinition $s -Language CSharp}}catch{Write-Host ('ERROR: csv-tool could not start. '+$_.Exception.Message) -ForegroundColor Red;Read-Host 'Press Enter to close'|Out-Null;exit 2};exit [CsvTool]::Run($env:CSV_TOOL_ARGS)"
exit /b %ERRORLEVEL%

//__CSHARP__
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class CsvTool
{
    private const int BufferSize = 64 * 1024;
    private static readonly char[] QuoteTriggers = { ',', '"', '\r', '\n' };
    private static readonly string[] OutputNames =
    {
        "01_\u53D6\u5F15\u30C7\u30FC\u30BF",
        "02_\u6C7A\u6E08\u30C7\u30FC\u30BF",
        "03_\u53D7\u4ED8\u30C7\u30FC\u30BF"
    };

    private sealed class InputInfo
    {
        public string Path { get; private set; }
        public Encoding Encoding { get; private set; }
        public char Delimiter { get; private set; }
        public string[] Header { get; private set; }

        public InputInfo(string path, Encoding encoding, char delimiter, string[] header)
        {
            Path = path;
            Encoding = encoding;
            Delimiter = delimiter;
            Header = header;
        }
    }

    public static int Run(string packedArguments)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            List<InputInfo> inputs = ParseAndValidateInputs(packedArguments);
            ShowInputs(inputs);

            string date = DateTime.Now.ToString("yyyyMMdd");
            string outputBaseName = SelectOutputName(date);
            if (outputBaseName == null) return Cancel();

            string outputPath = ResolveOutputPath(inputs, outputBaseName, date);
            if (outputPath == null) return Cancel();

            long duplicateCount;
            long rowCount = MergeAndWrite(inputs, outputPath, out duplicateCount);
            ShowCompleted(inputs.Count, rowCount, duplicateCount, outputPath);
            Pause();
            return 0;
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    private static List<InputInfo> ParseAndValidateInputs(string packedArguments)
    {
        string[] paths = SplitArguments(packedArguments);
        if (paths.Length == 0)
            throw new InvalidOperationException(
                "\u5BFE\u8C61\u30D5\u30A1\u30A4\u30EB\u304C\u3042\u308A\u307E\u305B\u3093\u3002CSV / TSV / TXT\u3092\u3053\u306EBAT\u3078\u30C9\u30E9\u30C3\u30B0\uFF06\u30C9\u30ED\u30C3\u30D7\u3057\u3066\u304F\u3060\u3055\u3044\u3002");

        var inputs = new List<InputInfo>();
        string[] referenceHeader = null;

        foreach (string rawPath in paths)
        {
            string path = Path.GetFullPath(rawPath);
            ValidateInputPath(path);

            Encoding encoding = DetectEncoding(path);
            char delimiter = DetectDelimiter(path, encoding);
            string[] header = ReadHeader(path, encoding, delimiter);

            if (referenceHeader == null) referenceHeader = header;
            else ValidateHeader(referenceHeader, header, path);

            inputs.Add(new InputInfo(path, encoding, delimiter, header));
        }
        return inputs;
    }

    private static void ValidateInputPath(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("\u30D5\u30A1\u30A4\u30EB\u304C\u898B\u3064\u304B\u308A\u307E\u305B\u3093\u3002", path);

        string extension = Path.GetExtension(path);
        if (!extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".tsv", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("\u5BFE\u8C61\u5916\u306E\u5F62\u5F0F\u3067\u3059: " + Path.GetFileName(path));
    }

    private static string[] SplitArguments(string packedArguments)
    {
        if (string.IsNullOrEmpty(packedArguments)) return new string[0];

        // Windows\u306E\u30D5\u30A1\u30A4\u30EB\u540D\u306B\u306F\u4F7F\u7528\u3067\u304D\u306A\u3044 | \u3092\u3001BAT\u304B\u3089\u8907\u6570\u30D1\u30B9\u3092\u6E21\u3059\u533A\u5207\u308A\u306B\u4F7F\u3046\u3002
        return packedArguments.Split(
            new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static string[] ReadHeader(string path, Encoding encoding, char delimiter)
    {
        using (var reader = new CsvReader(path, encoding, delimiter))
        {
            string[] header = reader.ReadRow();
            while (header != null && IsBlankRow(header)) header = reader.ReadRow();
            if (header == null)
                throw new InvalidOperationException(Path.GetFileName(path) + " \u306F\u7A7A\u30D5\u30A1\u30A4\u30EB\u3067\u3059\u3002");
            return header;
        }
    }

    private static void ShowInputs(List<InputInfo> inputs)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\u5BFE\u8C61: " + inputs.Count + "\u4EF6" +
            (inputs.Count > 1 ? " \u2192 \u30DE\u30FC\u30B8\u3057\u307E\u3059" : string.Empty));
        Console.ResetColor();

        foreach (InputInfo input in inputs)
        {
            string conversion = input.Delimiter == '\t' ? "TSV\u2192CSV" : "CSV\u305D\u306E\u307E\u307E";
            Console.WriteLine("  - " + Path.GetFileName(input.Path) +
                " [" + conversion + " / " + input.Encoding.WebName + "] " +
                input.Header.Length + "\u5217");
        }
    }

    private static string SelectOutputName(string date)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\u4FDD\u5B58\u540D\u3092\u9078\u3093\u3067\u304F\u3060\u3055\u3044:");
        Console.ResetColor();

        for (int i = 0; i < OutputNames.Length; i++)
            Console.WriteLine("  " + (i + 1) + ") " + OutputNames[i] + "_" + date + ".csv");
        Console.WriteLine("  0) \u4E2D\u6B62\u3059\u308B\uFF08\u4F55\u3082\u4FDD\u5B58\u3057\u307E\u305B\u3093\uFF09");

        while (true)
        {
            Console.Write("\u756A\u53F7 (1-" + OutputNames.Length + "\u30010\u3067\u4E2D\u6B62): ");
            string key = Console.ReadLine();
            if (key == null) return null; // \u5165\u529B\u304C\u9589\u3058\u3066\u3044\u308B\u3002\u5F85\u3061\u7D9A\u3051\u3066\u3082\u756A\u53F7\u306F\u6765\u306A\u3044\u3002

            string answer = Normalize(key);
            if (answer == "0" || answer.Equals("q", StringComparison.OrdinalIgnoreCase))
                return null;

            int number;
            if (int.TryParse(answer, out number) &&
                number >= 1 && number <= OutputNames.Length)
                return OutputNames[number - 1];
        }
    }

    private static string ResolveOutputPath(
        List<InputInfo> inputs, string baseName, string date)
    {
        string outputDirectory = Path.Combine(
            Path.GetDirectoryName(inputs[0].Path), "output_" + date);
        string outputPath = Path.Combine(
            outputDirectory, baseName + "_" + date + ".csv");

        foreach (InputInfo input in inputs)
            if (string.Equals(input.Path, outputPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "\u4FDD\u5B58\u5148\u3068\u540C\u3058\u30D5\u30A1\u30A4\u30EB\u304C\u5165\u529B\u306B\u542B\u307E\u308C\u3066\u3044\u307E\u3059: " + Path.GetFileName(outputPath));

        Directory.CreateDirectory(outputDirectory);
        if (!File.Exists(outputPath)) return outputPath;

        Console.Write("\u65E2\u306B\u5B58\u5728\u3057\u307E\u3059\u3002\u4E0A\u66F8\u304D\u3057\u307E\u3059\u304B? (y/N): ");
        string answer = Console.ReadLine();
        if (answer != null &&
            string.Equals(Normalize(answer), "y", StringComparison.OrdinalIgnoreCase))
            return outputPath;

        return null;
    }

    private static int Cancel()
    {
        Console.WriteLine("\u4E2D\u6B62\u3057\u307E\u3057\u305F\u3002");
        Pause();
        return 0;
    }

    // \u5168\u89D2\u306E\u300C\uFF11\u300D\u300C\uFF59\u300D\u3084\u524D\u5F8C\u306E\u7A7A\u767D\u3067\u5165\u529B\u3092\u306F\u3058\u304B\u306A\u3044\u3002
    private static string Normalize(string value)
    {
        return value.Normalize(NormalizationForm.FormKC).Trim();
    }

    private static long MergeAndWrite(
        List<InputInfo> inputs, string outputPath, out long duplicateCount)
    {
        // \u30A8\u30E9\u30FC\u6642\u306B\u65E2\u5B58\u51FA\u529B\u3092\u58CA\u3055\u306A\u3044\u3088\u3046\u3001\u4E00\u6642\u30D5\u30A1\u30A4\u30EB\u30781\u30D1\u30B9\u3067\u66F8\u3044\u3066\u304B\u3089\u7F6E\u63DB\u3059\u308B\u3002
        string temporaryPath = outputPath + ".tmp";
        long totalRows = 0;
        duplicateCount = 0;
        // \u5168\u5217\u304C\u5B8C\u5168\u306B\u540C\u3058\u884C\u306F\u3001\u30D5\u30A1\u30A4\u30EB\u3092\u307E\u305F\u3044\u3067\u3082\u6700\u521D\u306E1\u884C\u3060\u3051\u6B8B\u3059\u3002
        var seenRows = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            using (var writer = new StreamWriter(
                temporaryPath, false, new UTF8Encoding(true), BufferSize))
            {
                WriteCsvRow(writer, inputs[0].Header);

                foreach (InputInfo input in inputs)
                    totalRows += AppendDataRows(input, writer, seenRows, ref duplicateCount);
            }

            if (File.Exists(outputPath)) File.Delete(outputPath);
            File.Move(temporaryPath, outputPath);
            return totalRows;
        }
        catch
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            throw;
        }
    }

    private static long AppendDataRows(
        InputInfo input, TextWriter writer,
        HashSet<string> seenRows, ref long duplicateCount)
    {
        long rowNumber = 0;
        using (var reader = new CsvReader(
            input.Path, input.Encoding, input.Delimiter))
        {
            // \u30D8\u30C3\u30C0\u30FC\u306F\u51FA\u529B\u6E08\u307F\u306A\u306E\u3067\u8AAD\u307F\u98DB\u3070\u3059\u3002
            string[] skipped = reader.ReadRow();
            while (skipped != null && IsBlankRow(skipped)) skipped = reader.ReadRow();

            string[] row;
            long lineNumber = 0;
            while ((row = reader.ReadRow()) != null)
            {
                lineNumber++;
                if (IsBlankRow(row)) continue; // \u672B\u5C3E\u3084\u9014\u4E2D\u306E\u7A7A\u884C\u306F\u30C7\u30FC\u30BF\u3067\u306F\u306A\u3044\u3002

                ValidateColumnCount(input, row, lineNumber);
                if (!seenRows.Add(string.Join("\u001F", row)))
                {
                    duplicateCount++;
                    continue;
                }

                rowNumber++;
                WriteCsvRow(writer, row);
            }
        }
        return rowNumber;
    }

    private static bool IsBlankRow(string[] row)
    {
        return row.Length == 1 && row[0].Trim().Length == 0;
    }

    private static void ValidateColumnCount(
        InputInfo input, string[] row, long rowNumber)
    {
        if (row.Length == input.Header.Length) return;

        throw new InvalidOperationException(
            Path.GetFileName(input.Path) + " \u306E\u5217\u6570\u304C\u4E00\u81F4\u3057\u307E\u305B\u3093\u3002\u30C7\u30FC\u30BF\u884C " +
            rowNumber + ": " + row.Length + "\u5217\u3001\u30D8\u30C3\u30C0\u30FC: " +
            input.Header.Length + "\u5217");
    }

    private static void ValidateHeader(
        string[] expected, string[] actual, string path)
    {
        if (expected.Length != actual.Length)
            throw new InvalidOperationException(
                "\u5217\u6570\u304C\u4E00\u81F4\u3057\u306A\u3044\u305F\u3081\u30DE\u30FC\u30B8\u3067\u304D\u307E\u305B\u3093: " + Path.GetFileName(path));

        for (int i = 0; i < expected.Length; i++)
        {
            if (!string.Equals(Normalize(expected[i]), Normalize(actual[i]), StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "\u30D8\u30C3\u30C0\u30FC\u304C\u4E00\u81F4\u3057\u306A\u3044\u305F\u3081\u30DE\u30FC\u30B8\u3067\u304D\u307E\u305B\u3093: " +
                    Path.GetFileName(path) + " \u306E" + (i + 1) + "\u5217\u76EE");
        }
    }

    private static Encoding DetectEncoding(string path)
    {
        byte[] prefix = new byte[3];
        int length;
        using (FileStream stream = File.OpenRead(path))
            length = stream.Read(prefix, 0, prefix.Length);

        if (StartsWith(prefix, length, new byte[] { 0xEF, 0xBB, 0xBF }))
            return new UTF8Encoding(true, true);
        if (StartsWith(prefix, length, new byte[] { 0xFF, 0xFE }))
            return new UnicodeEncoding(false, true, true);
        if (StartsWith(prefix, length, new byte[] { 0xFE, 0xFF }))
            return new UnicodeEncoding(true, true, true);

        try
        {
            var utf8 = new UTF8Encoding(false, true);
            using (var reader = new StreamReader(
                path, utf8, false, BufferSize))
            {
                char[] buffer = new char[BufferSize];
                while (reader.Read(buffer, 0, buffer.Length) > 0) { }
            }
            return utf8;
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(932);
        }
    }

    private static bool StartsWith(
        byte[] source, int sourceLength, byte[] prefix)
    {
        if (sourceLength < prefix.Length) return false;
        for (int i = 0; i < prefix.Length; i++)
            if (source[i] != prefix[i]) return false;
        return true;
    }

    private static char DetectDelimiter(string path, Encoding encoding)
    {
        using (var reader = new StreamReader(
            path, encoding, true, BufferSize))
        {
            string firstLine = reader.ReadLine();
            while (firstLine != null && firstLine.Trim().Length == 0)
                firstLine = reader.ReadLine();
            if (firstLine == null)
                throw new InvalidOperationException(
                    Path.GetFileName(path) + " \u306E\u5148\u982D\u884C\u304C\u7A7A\u3067\u3059\u3002");

            int tabs = Count(firstLine, '\t');
            int commas = Count(firstLine, ',');
            if (tabs == 0 && commas == 0)
                throw new InvalidOperationException(
                    Path.GetFileName(path) + " \u306E\u533A\u5207\u308A\u6587\u5B57\u3092\u5224\u5B9A\u3067\u304D\u307E\u305B\u3093\u3002");

            // \u5143\u30C4\u30FC\u30EB\u4E92\u63DB: \u540C\u6570\u306E\u5834\u5408\u306F\u30BF\u30D6\u3092\u512A\u5148\u3059\u308B\u3002
            return tabs >= commas ? '\t' : ',';
        }
    }

    private static int Count(string value, char target)
    {
        int count = 0;
        foreach (char c in value) if (c == target) count++;
        return count;
    }

    private static void WriteCsvRow(TextWriter writer, string[] fields)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0) writer.Write(',');
            string value = fields[i] ?? string.Empty;

            if (value.IndexOfAny(QuoteTriggers) < 0)
            {
                writer.Write(value);
                continue;
            }

            writer.Write('"');
            writer.Write(value.Replace("\"", "\"\""));
            writer.Write('"');
        }
        writer.WriteLine();
    }

    private static void ShowCompleted(
        int fileCount, long rowCount, long duplicateCount, string outputPath)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("OK: " + outputPath);
        Console.WriteLine(fileCount + "\u30D5\u30A1\u30A4\u30EB / \u30C7\u30FC\u30BF" + rowCount +
            "\u884C / UTF-8 BOM\u4ED8\u304DCSV");
        if (duplicateCount > 0)
            Console.WriteLine("\u5B8C\u5168\u306B\u540C\u3058\u884C\u3092 " + duplicateCount + "\u884C \u9664\u304D\u307E\u3057\u305F\u3002");
        Console.ResetColor();
    }

    private static int Fail(string message)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\u30A8\u30E9\u30FC: " + message);
        Console.ResetColor();
        Pause();
        return 1;
    }

    private static void Pause()
    {
        Console.Write("Enter\u3067\u7D42\u4E86");
        Console.ReadLine();
    }

    private sealed class CsvReader : IDisposable
    {
        private readonly StreamReader _reader;
        private readonly char _delimiter;
        private bool _finished;

        public CsvReader(string path, Encoding encoding, char delimiter)
        {
            _reader = new StreamReader(path, encoding, true, BufferSize);
            _delimiter = delimiter;
        }

        public string[] ReadRow()
        {
            if (_finished) return null;

            var fields = new List<string>();
            var field = new StringBuilder();
            bool insideQuotes = false;
            bool hasData = false;

            while (true)
            {
                int value = _reader.Read();
                if (value < 0)
                    return FinishAtEndOfFile(fields, field, insideQuotes, hasData);

                hasData = true;
                char current = (char)value;

                if (insideQuotes)
                {
                    ReadQuotedCharacter(field, ref insideQuotes, current);
                    continue;
                }

                // RFC 4180\u4E92\u63DB\u306E\u5272\u308A\u5207\u308A: \u5F15\u7528\u7B26\u306F\u30D5\u30A3\u30FC\u30EB\u30C9\u5148\u982D\u3060\u3051\u958B\u59CB\u8A18\u53F7\u3068\u3059\u308B\u3002
                if (current == '"' && field.Length == 0)
                    insideQuotes = true;
                else if (current == _delimiter)
                    FinishField(fields, field);
                else if (current == '\r' || current == '\n')
                    return FinishRecord(fields, field, current);
                else
                    field.Append(current);
            }
        }

        private void ReadQuotedCharacter(
            StringBuilder field, ref bool insideQuotes, char current)
        {
            if (current != '"')
            {
                field.Append(current);
                return;
            }

            if (_reader.Peek() == '"')
            {
                _reader.Read();
                field.Append('"');
            }
            else
            {
                insideQuotes = false;
            }
        }

        private string[] FinishAtEndOfFile(
            List<string> fields, StringBuilder field,
            bool insideQuotes, bool hasData)
        {
            _finished = true;
            if (insideQuotes)
                throw new InvalidOperationException("\u5F15\u7528\u7B26\u304C\u9589\u3058\u3089\u308C\u3066\u3044\u306A\u3044CSV\u3067\u3059\u3002");
            if (!hasData && field.Length == 0 && fields.Count == 0) return null;

            FinishField(fields, field);
            return fields.ToArray();
        }

        private string[] FinishRecord(
            List<string> fields, StringBuilder field, char lineBreak)
        {
            if (lineBreak == '\r' && _reader.Peek() == '\n') _reader.Read();
            FinishField(fields, field);
            return fields.ToArray();
        }

        private static void FinishField(
            List<string> fields, StringBuilder field)
        {
            fields.Add(field.ToString());
            field.Length = 0;
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}
