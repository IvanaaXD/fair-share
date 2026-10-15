using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace FairShare.Infrastructure.Reports;

/// <summary>
/// One-time QuestPDF configuration: license and the font of the reports.
///
/// The font (Onest, the same as in the web app, SIL Open Font License) is embedded in this
/// assembly, so reports look the same on Windows and in a minimal Docker image that has no
/// fonts installed - and Cyrillic, č, ć, đ always render.
/// </summary>
public static class QuestPdfSetup
{
    public const string FontFamily = "Onest";

    // Names given with LogicalName in FairShare.Infrastructure.csproj.
    private static readonly string[] FontResources =
    [
        "FairShare.Fonts.Onest-Regular.ttf",
        "FairShare.Fonts.Onest-Bold.ttf"
    ];

    private static readonly object Lock = new();
    private static bool _configured;

    public static void Configure()
    {
        lock (Lock)
        {
            if (_configured)
                return;

            // Free Community license: individuals, education and companies under 1M USD revenue.
            QuestPDF.Settings.License = LicenseType.Community;

            // A description may contain an emoji the font does not have. It is drawn as an empty
            // box instead of failing the whole report.
            QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;

            var assembly = typeof(QuestPdfSetup).Assembly;
            foreach (var resource in FontResources)
                FontManager.RegisterFontFromEmbeddedResource(assembly, resource);

            _configured = true;
        }
    }
}
