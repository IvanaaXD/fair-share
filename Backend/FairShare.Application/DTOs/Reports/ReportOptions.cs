namespace FairShare.Application.DTOs.Reports;

/// <summary>What the user downloads: a formatted document or a table for Excel.</summary>
public enum ReportFormat
{
    Pdf,
    Csv
}

/// <summary>Language and script of the report - the same three the apps offer.</summary>
public enum ReportLanguage
{
    SrCyrl,
    SrLatn,
    En
}

public static class ReportLanguages
{
    /// <summary>
    /// Reads the language the apps send ("sr-Cyrl", "sr-Latn", "en"). Anything unknown falls
    /// back to Serbian Cyrillic, the default language of the application.
    /// </summary>
    public static ReportLanguage Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ReportLanguage.SrCyrl;

        var lang = value.Trim();
        if (lang.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            return ReportLanguage.En;
        if (lang.Contains("latn", StringComparison.OrdinalIgnoreCase))
            return ReportLanguage.SrLatn;

        return ReportLanguage.SrCyrl;
    }
}

/// <summary>A generated report, ready to be returned as a download.</summary>
public sealed record ReportFile(byte[] Content, string ContentType, string FileName);
