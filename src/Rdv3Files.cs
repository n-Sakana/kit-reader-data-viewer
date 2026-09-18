// File layout, local session ownership and non-destructive exports. C# 5.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public static class Rdv3Files
{
    public static string Full(string value, string root)
    {
        if (string.IsNullOrWhiteSpace(value)) { throw new IOException("file path is blank"); }
        return Path.GetFullPath(Path.IsPathRooted(value) ? value : Path.Combine(root, value));
    }

    public static bool Same(string a, string b)
    { return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase); }

    public static bool Exists(string path)
    {
        try { File.GetAttributes(path); return true; }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }

    private static bool Under(string path, string directory)
    {
        string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ProgramFile(string path, string appDir, string configPath)
    {
        if (Same(path, configPath) || Same(path, configPath + ".edit.lock")) { return true; }
        string[] dirs = { "src", "lib", "web", "docs", "tests" };
        for (int i = 0; i < dirs.Length; i++) { if (Under(path, Path.Combine(appDir, dirs[i]))) { return true; } }
        string[] files = { "ReaderDataViewer.cmd", "ReaderDataViewer.vbs", "README.md", "LICENSE",
            "THIRD-PARTY-NOTICES.md", "build.bat" };
        for (int i = 0; i < files.Length; i++) { if (Same(path, Path.Combine(appDir, files[i]))) { return true; } }
        return false;
    }

    private static List<string> InputPaths(Rdv3Data data, string dataDir)
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string note;
        for (int i = 0; i < data.Tables.Count; i++)
        { seen.Add(ResolveInput(data.Tables[i].File, data.Tables[i].FileMatch, dataDir, out note)); }
        // File-only job inputs (for example delete.csv) are inputs too.
        for (int j = 0; j < data.Jobs.Count; j++)
        {
            for (int i = 0; i < data.Jobs[j].Inputs.Count; i++)
            {
                Rdv3ProcessInputDef input = data.Jobs[j].Inputs[i];
                if (!string.IsNullOrEmpty(input.File)) { seen.Add(ResolveInput(input.File, input.FileMatch, dataDir, out note)); }
            }
        }
        return new List<string>(seen);
    }

    // extensionKey is a NameKey (upper-cased ASCII), so the constants are too.
    private static bool IsInputExtension(string extensionKey)
    {
        return extensionKey == ".CSV" || extensionKey == ".TXT" || extensionKey == ".TSV" || extensionKey == ".XLSX";
    }

    // The settings dialog names a file: say now, not at the next start, when
    // nothing in the data folder answers to that name. Returns null when it resolves.
    public static string CheckInputName(string file, string match, string dataDirSetting, string appDir)
    {
        string dataDir;
        try { dataDir = Full(dataDirSetting, appDir); }
        catch (Exception) { return Rdv3Text.ErrDataDir + dataDirSetting; }
        if (!Directory.Exists(dataDir)) { return Rdv3Text.ErrDataDir + dataDir; }
        string note;
        string path = ResolveInput(file, match, dataDir, out note);
        return Exists(path) ? null : MissingInputMessage(file, match, dataDir);
    }

    // Where an input file is. "exact": the name as written when that file
    // exists, else the one file whose name reads the same (width, spacing,
    // ASCII case). "prefix": the newest file whose name starts with the
    // configured stem and carries its extension. A departure from the name as
    // written comes back as a note; a name that matches nothing resolves to
    // the configured path, so the caller reports the missing file by name.
    public static string ResolveInput(string file, string match, string dataDir, out string note)
    {
        note = null;
        string configured = Full(file, dataDir);
        bool prefix = string.Equals(match, "prefix", StringComparison.OrdinalIgnoreCase);
        if (!prefix && Exists(configured)) { return configured; }
        string directory = Path.GetDirectoryName(configured);
        string name = Path.GetFileName(configured);
        if (directory == null || !Directory.Exists(directory)) { return configured; }
        string wantStem = Rdv3Input.NameKey(Path.GetFileNameWithoutExtension(name));
        string wantExt = Rdv3Input.NameKey(Path.GetExtension(name));
        if (wantStem.Length == 0) { return configured; }
        FileInfo best = null;
        int matched = 0;
        foreach (FileInfo candidate in new DirectoryInfo(directory).GetFiles())
        {
            string stem = Rdv3Input.NameKey(Path.GetFileNameWithoutExtension(candidate.Name));
            string extension = Rdv3Input.NameKey(candidate.Extension);
            bool extensionOk = (wantExt.Length == 0) ? IsInputExtension(extension) : extension == wantExt;
            bool stemOk = prefix ? stem.StartsWith(wantStem, StringComparison.Ordinal) : stem == wantStem;
            if (!extensionOk || !stemOk) { continue; }
            matched++;
            if (best == null || candidate.LastWriteTimeUtc > best.LastWriteTimeUtc
                || (candidate.LastWriteTimeUtc == best.LastWriteTimeUtc && string.CompareOrdinal(candidate.Name, best.Name) > 0))
            { best = candidate; }
        }
        if (best == null) { return configured; }
        if (!Same(best.FullName, configured) || matched > 1)
        {
            note = Rdv3Text.InputFileResolved.Replace("{configured}", file)
                .Replace("{mode}", prefix ? Rdv3Text.FileMatchPrefix : Rdv3Text.FileMatchExact)
                .Replace("{actual}", best.Name + (matched > 1 ? " (" + matched.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")" : ""));
        }
        return best.FullName;
    }

    public static string MissingInputMessage(string file, string match, string dataDir)
    {
        bool prefix = string.Equals(match, "prefix", StringComparison.OrdinalIgnoreCase);
        return Rdv3Text.InputFileNotFound.Replace("{configured}", file)
            .Replace("{mode}", prefix ? Rdv3Text.FileMatchPrefix : Rdv3Text.FileMatchExact)
            .Replace("{dir}", Path.GetDirectoryName(Full(file, dataDir)) ?? dataDir);
    }

    public static void ValidateLayout(Rdv3Config cfg, string appDir, string configPath)
    {
        string data = Full(cfg.DataDir, appDir);
        string ledger = Full(cfg.Ledger, appDir);
        string log = Full(cfg.Log, appDir);
        if (!string.Equals(Path.GetExtension(ledger), ".xlsx", StringComparison.OrdinalIgnoreCase))
        { throw new IOException("paths.ledger must have the .xlsx extension"); }
        if (Same(ledger, log) || Same(log, ledger + ".lock") || Same(log, ledger + ".version")
            || ProgramFile(ledger, appDir, configPath) || ProgramFile(log, appDir, configPath)
            || Rdv3OperationLog.IsOperationLog(log, ledger))
        { throw new IOException("paths collide with a protected application/ledger file"); }
        foreach (string source in InputPaths(cfg.Data, data))
        {
            if (Same(source, ledger) || Same(source, log) || Same(source, ledger + ".lock")
                || Same(source, ledger + ".version") || Rdv3OperationLog.IsOperationLog(source, ledger))
            { throw new IOException("input file collides with a ledger/log file: " + source); }
        }
    }

    public static string ExportPath(string value, string appDir, string dataDir, string ledgerPath,
                                     string logPath, string configPath, Rdv3Data data)
    { return NewOutputPath(value, appDir, dataDir, ledgerPath, logPath, configPath, data, ".csv"); }

    public static string ReportPath(string value, string appDir, string dataDir, string ledgerPath,
                                     string logPath, string configPath, Rdv3Data data)
    { return NewOutputPath(value, appDir, dataDir, ledgerPath, logPath, configPath, data, ".json"); }

    public static string MigrationPath(string value, string appDir, string dataDir, string ledgerPath,
                                       string logPath, string configPath, Rdv3Data data)
    { return NewOutputPath(value, appDir, dataDir, ledgerPath, logPath, configPath, data, ".xlsx"); }

    private static string NewOutputPath(string value, string appDir, string dataDir, string ledgerPath,
                                         string logPath, string configPath, Rdv3Data data, string extension)
    {
        string path = Full(value, appDir);
        if (!string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase))
        { throw new IOException(extension == ".csv" ? Rdv3Text.ExportCsvOnly : "-Output: use a new " + extension + " file"); }
        if (ProgramFile(path, appDir, configPath) || Same(path, ledgerPath) || Same(path, logPath)
            || Same(path, ledgerPath + ".lock") || Same(path, ledgerPath + ".version")
            || Rdv3OperationLog.IsOperationLog(path, ledgerPath))
        { throw new IOException(Rdv3Text.ExportProtected); }
        foreach (string source in InputPaths(data, dataDir))
        {
            if (Same(path, source))
            { throw new IOException(Rdv3Text.ExportProtected); }
        }
        if (Exists(path)) { throw new IOException(Rdv3Text.ExportExists); }
        return path;
    }

    public static string[] ExportHeaders(List<string> fields, Rdv3Data data, string stateColumn)
    {
        string[] headers = new string[fields.Count];
        Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < fields.Count; i++)
        {
            int column = fields[i] == "$work" ? -1 : data.IndexOf(fields[i]);
            if (column < 0 && fields[i] != "$work") { throw new InvalidDataException("unknown export field: " + fields[i]); }
            headers[i] = column < 0 ? stateColumn : data.Columns[column].Column;
            int count; counts.TryGetValue(headers[i], out count); counts[headers[i]] = count + 1;
        }
        HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < fields.Count; i++)
        {
            string name = counts[headers[i]] > 1
                ? (fields[i] == "$work" ? headers[i] + " ($work)" : fields[i]) : headers[i];
            string unique = name; int suffix = 2;
            while (!used.Add(unique)) { unique = name + " [" + (suffix++).ToString(CultureInfo.InvariantCulture) + "]"; }
            headers[i] = unique;
        }
        return headers;
    }

    public static string CsvCell(string value, bool excelSafe)
    {
        string text = value ?? "";
        string probe = text.TrimStart();
        if (excelSafe && probe.Length > 0 && "=+-@".IndexOf(probe[0]) >= 0) { text = "'" + text; }
        if (text.IndexOfAny(new char[] { ',', '"', '\r', '\n' }) < 0) { return text; }
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    public static void WriteNewText(string path, string text, bool utf8Bom = true)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(path));
        Directory.CreateDirectory(directory);
        string temp = Path.Combine(directory, ".rdv-export-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (FileStream stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(utf8Bom, true), 65536, true))
                { writer.Write(text); }
                stream.Flush(true);
            }
            // Move does not overwrite a file created by someone else meanwhile.
            File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) { File.Delete(temp); } }
    }

    public static string SessionKey(string ledger)
    {
        return Rdv3PendingStore.DigestOf(Path.GetFullPath(ledger).ToLowerInvariant())
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');
    }

    public static FileStream AcquireLocalSession(string pendingPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(pendingPath));
        // Keep the file, release only the handle. Deleting an unlocked pathname
        // introduces a race with a new owner. OS handle cleanup handles a crash.
        return new FileStream(pendingPath + ".session.lock", FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
    }

    public static string StorageContract(Rdv3Data data, Rdv3WorkState work)
    {
        // Old clients must reject the new ledger rather than drop its archive.
        return Rdv3PendingStore.DigestOf("RDV-STORAGE-2:" + LegacyStorageContract(data, work)
            + ":" + Rdv3BusinessDefinition.Bound(data));
    }

    public static string LegacyStorageContract(Rdv3Data data, Rdv3WorkState work)
    {
        List<string> entries = new List<string>();
        entries.Add("RDV-STORAGE-1");
        entries.Add(string.Join(",", Array.ConvertAll(data.IdentityCols, delegate(int c) { return c.ToString(CultureInfo.InvariantCulture); })));
        entries.Add(work.Column);
        entries.Add(work.Initial);
        for (int i = 0; i < data.Columns.Count; i++) { entries.Add(data.Columns[i].Ref); }
        List<string> states = new List<string>();
        for (int i = 0; i < work.States.Count; i++)
        { states.Add(Rdv3PendingStore.DigestOf(work.States[i].Id) + ":" + Rdv3PendingStore.DigestOf(work.States[i].Stored)); }
        states.Sort(StringComparer.Ordinal);
        entries.AddRange(states);
        StringBuilder canonical = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        { canonical.Append(entries[i].Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(entries[i]); }
        return Rdv3PendingStore.DigestOf(canonical.ToString());
    }

    public static string Stamp(string path)
    {
        if (!Exists(path)) { return "missing"; }
        FileInfo file = new FileInfo(path);
        return file.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture)
            + ":" + file.Length.ToString(CultureInfo.InvariantCulture);
    }
}
