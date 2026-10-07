using System.Net;

namespace FairShare.Infrastructure.Email;

/// <summary>
/// Јединствен HTML шаблон за све e-mail поруке, у бојама апликације FairShare.
/// Кориснички текст се увијек енкодује (HtmlEncode) да би се спријечио XSS у пошти.
/// </summary>
public static class EmailTemplateBuilder
{
    private const string PrimaryColor = "#1F4E79";
    private const string BackgroundColor = "#F4F6F9";

    public static string Build(string recipientName, string title, string body)
    {
        var safeName = WebUtility.HtmlEncode(recipientName);
        var safeTitle = WebUtility.HtmlEncode(title);
        var safeBody = WebUtility.HtmlEncode(body).Replace("\n", "<br>");

        return $"""
            <!DOCTYPE html>
            <html lang="sr">
            <head><meta charset="utf-8"><title>{safeTitle}</title></head>
            <body style="margin:0;padding:0;background:{BackgroundColor};font-family:Segoe UI,Arial,sans-serif;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{BackgroundColor};padding:24px 0;">
                <tr><td align="center">
                  <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;overflow:hidden;">
                    <tr><td style="background:{PrimaryColor};padding:20px 28px;color:#ffffff;font-size:22px;font-weight:bold;">FairShare</td></tr>
                    <tr><td style="padding:28px;color:#222222;font-size:15px;line-height:1.6;">
                      <p style="margin:0 0 12px 0;">Поштовани/а {safeName},</p>
                      <h2 style="margin:0 0 12px 0;font-size:18px;color:{PrimaryColor};">{safeTitle}</h2>
                      <p style="margin:0;">{safeBody}</p>
                    </td></tr>
                    <tr><td style="padding:16px 28px;background:#FAFBFC;color:#888888;font-size:12px;border-top:1px solid #E5E8EC;">
                      Ова порука је аутоматски генерисана из апликације FairShare. Молимо вас да не одговарате на њу.
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }
}
