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

    private static readonly string[] FontFiles = ["Onest-Regular.ttf", "Onest-Bold.ttf"];

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
            var resources = assembly.GetManifestResourceNames();

            foreach (var file in FontFiles)
            {
                // The resource name depends on how the file was embedded ("FairShare.Fonts.X.ttf"
                // with LogicalName, "FairShare.Infrastructure.Reports.Fonts.X.ttf" without it),
                // so it is found by the file name at the end.
                var resource = resources.FirstOrDefault(name => name.EndsWith(file, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException(
                        $"Font {file} is not embedded in {assembly.GetName().Name}. Add it to the .csproj as " +
                        "<EmbeddedResource Include=\"Reports\\Fonts\\*.ttf\" /> and rebuild.");

                FontManager.RegisterFontFromEmbeddedResource(assembly, resource);
            }

            _configured = true;
        }
    }
}
