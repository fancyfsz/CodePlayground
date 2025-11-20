using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

public static class CatalogScanner
{
    public class CatalogCheckResult
    {
        public string FilePath;
        public bool IsValid;
        public string Reason;

        public long FileLength;
        public int HeaderSize;
        public int EntryCount;
        public int ProviderCount;

        public bool PossiblyTruncated;
        public bool PossiblyShifted;
        public bool PossiblyVersionMismatch;

        public string StructureSummary;
        public string BackupPath;

        public string FileName => Path.GetFileName(FilePath);
    }

    // =========================================================
    // 单文件检查
    // =========================================================
    public static CatalogCheckResult CheckCatalog(string path, bool autoRemoveCorrupt = false)
    {
        var result = new CatalogCheckResult { FilePath = path };

        if (!File.Exists(path))
        {
            result.IsValid = false;
            result.Reason = "File not found.";
            return result;
        }

        byte[] bytes = File.ReadAllBytes(path);
        result.FileLength = bytes.LongLength;

        try
        {
            using var ms = new MemoryStream(bytes);
            using var br = new BinaryReader(ms);

            // -------- HEADER SIZE --------
            if (ms.Length < 4)
                return Fail(result, path, autoRemoveCorrupt, "Too small (<4 bytes). Truncated.", truncated:true);

            result.HeaderSize = br.ReadInt32();

            if (result.HeaderSize <= 0 || result.HeaderSize > 50_000_000)
                return Fail(result, path, autoRemoveCorrupt,
                    $"HeaderSize abnormal: {result.HeaderSize}", shifted:true);

            if (result.HeaderSize > ms.Length)
                return Fail(result, path, autoRemoveCorrupt,
                    "Header block exceeds file length (truncated or overwritten).",
                    truncated:true);

            ms.Seek(result.HeaderSize, SeekOrigin.Begin);

            // -------- ENTRIES --------
            if (ms.Position + 4 > ms.Length)
                return Fail(result, path, autoRemoveCorrupt, 
                    "Cannot read entry count (truncated).",
                    truncated:true);

            result.EntryCount = br.ReadInt32();

            bool entrySuspicious = result.EntryCount < 0 || result.EntryCount > 10_000_000;

            // -------- PROVIDERS --------
            if (ms.Position + 4 > ms.Length)
                return Fail(result, path, autoRemoveCorrupt,
                    "Cannot read provider count (truncated).",
                    truncated:true);

            result.ProviderCount = br.ReadInt32();
            bool providerSuspicious = result.ProviderCount < 0 || result.ProviderCount > 10_000_000;

            // Validate provider block
            long providerBlockEnd = ms.Position + result.ProviderCount * 4L;
            if (providerBlockEnd > ms.Length)
                return Fail(result, path, autoRemoveCorrupt,
                    "Provider block extends past file end (corrupted or partially overwritten).",
                    truncated:true);

            // -------- Version Mismatch --------
            if (DetectVersionMismatch(bytes, result.HeaderSize))
                result.PossiblyVersionMismatch = true;

            // -------- Shifted --------
            if (DetectShifted(bytes, result.HeaderSize, result.EntryCount, result.ProviderCount))
                result.PossiblyShifted = true;

            result.StructureSummary =
$@"FileSize: {result.FileLength:N0} bytes
HeaderSize: {result.HeaderSize:N0}
Entries: {result.EntryCount:N0} {(entrySuspicious ? "(Suspicious)" : "")}
Providers: {result.ProviderCount:N0} {(providerSuspicious ? "(Suspicious)" : "")}
VersionMismatch: {result.PossiblyVersionMismatch}
ShiftedStructure: {result.PossiblyShifted}
";

            result.IsValid = true;
            result.Reason = "Catalog appears structurally valid.";
            return result;
        }
        catch (Exception e)
        {
            return Fail(result, path, autoRemoveCorrupt, "Exception: " + e.Message, shifted:true);
        }
    }

    private static bool DetectVersionMismatch(byte[] data, int headerSize)
    {
        if (headerSize < 64 || headerSize > 5_000_000) return true;
        int jsonChars = 0;
        for (int i = 0; i < Math.Min(200, headerSize); i++)
        {
            byte b = data[i];
            if (b >= 32 && b <= 126) jsonChars++;
        }
        if (jsonChars < 10 && headerSize > 500_000) return true;
        return false;
    }

    private static bool DetectShifted(byte[] data, int headerSize, int entries, int providers)
    {
        if (headerSize % 4 != 0) return true;
        if ((entries > 50_000_000 || providers > 50_000_000) && entries > 0 && providers > 0) return true;
        return false;
    }

    private static CatalogCheckResult Fail(
        CatalogCheckResult result,
        string path,
        bool autoRemove,
        string reason,
        bool truncated = false,
        bool shifted = false)
    {
        result.IsValid = false;
        result.Reason = reason;
        result.PossiblyTruncated = truncated;
        result.PossiblyShifted = shifted;

        if (autoRemove)
        {
            string backup = path + ".corrupt.bak";
            if (File.Exists(backup)) File.Delete(backup);
            File.Move(path, backup);
            result.BackupPath = backup;
        }

        return result;
    }

    // =========================================================
    // 批量扫描目录
    // =========================================================
    public static List<CatalogCheckResult> ScanDirectory(string root, bool autoRemoveCorrupt = false)
    {
        var results = new List<CatalogCheckResult>();
        if (!Directory.Exists(root)) return results;

        var files = Directory.GetFiles(root, "*.bin", SearchOption.AllDirectories);
        foreach (var f in files)
        {
            if (!Path.GetFileName(f).ToLower().Contains("catalog")) continue;
            results.Add(CheckCatalog(f, autoRemoveCorrupt));
        }

        return results;
    }

    // =========================================================
    // 生成 HTML 报告
    // =========================================================
    public static void GenerateHtmlReport(List<CatalogCheckResult> results, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'><title>Catalog Scan Report</title>");
        sb.AppendLine("<style>body{font-family:Arial;} table{border-collapse:collapse;width:100%;} th,td{border:1px solid #aaa;padding:4px;} th{background:#eee;} .invalid{background:#fdd;} .valid{background:#dfd;}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h2>Catalog Scan Report - {DateTime.Now}</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>File</th><th>Status</th><th>Reason</th><th>Header</th><th>Entries</th><th>Providers</th><th>VersionMismatch</th><th>Shifted</th></tr>");

        foreach (var r in results)
        {
            string cls = r.IsValid ? "valid" : "invalid";
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{r.FileName}</td>");
            sb.AppendLine($"<td class='{cls}'>{(r.IsValid ? "VALID" : "INVALID")}</td>");
            sb.AppendLine($"<td>{r.Reason}</td>");
            sb.AppendLine($"<td>{r.HeaderSize}</td>");
            sb.AppendLine($"<td>{r.EntryCount}</td>");
            sb.AppendLine($"<td>{r.ProviderCount}</td>");
            sb.AppendLine($"<td>{r.PossiblyVersionMismatch}</td>");
            sb.AppendLine($"<td>{r.PossiblyShifted}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table></body></html>");

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }
}
