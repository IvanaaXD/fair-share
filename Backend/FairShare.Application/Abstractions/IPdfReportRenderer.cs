using FairShare.Application.Common.Reports;

namespace FairShare.Application.Abstractions;

/// <summary>
/// Lays a report out as a PDF document. Implemented in Infrastructure (QuestPDF), so the
/// application layer does not depend on a PDF library.
/// </summary>
public interface IPdfReportRenderer
{
    byte[] RenderSpending(SpendingReport report, ReportText text);

    byte[] RenderGroupDebts(GroupDebtReport report, ReportText text);
}
