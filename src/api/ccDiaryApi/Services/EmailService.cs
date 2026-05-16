// <copyright file="EmailService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using MailKit.Net.Smtp;
    using MailKit.Security;
    using MimeKit;

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendInvitationAsync(string toEmail, string toName, string inviteRedeemUrl)
        {
            var host = _configuration["Smtp:Host"]
                ?? throw new InvalidOperationException("Smtp:Host is not configured.");
            var portStr = _configuration["Smtp:Port"];
            var username = _configuration["Smtp:Username"]
                ?? throw new InvalidOperationException("Smtp:Username is not configured.");
            var password = _configuration["Smtp:Password"]
                ?? throw new InvalidOperationException("Smtp:Password is not configured.");
            var from = _configuration["Smtp:From"]
                ?? throw new InvalidOperationException("Smtp:From is not configured.");
            var fromName = _configuration["Smtp:FromName"] ?? "ccDiary";

            if (!int.TryParse(portStr, out var port))
            {
                port = 587;
            }

            var html = BuildHtml(toName, inviteRedeemUrl, DateTime.UtcNow.Year);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, from));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = "You're invited to join ccDiary";

            var body = new BodyBuilder { HtmlBody = html };
            message.Body = body.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Invitation email sent to {Email}", toEmail);
        }

        private static string BuildHtml(string displayName, string inviteRedeemUrl, int year)
        {
            return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>You're invited to join ccDiary</title>
                </head>
                <body style="margin:0;padding:0;background-color:#f6f6fb;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f6f6fb;padding:40px 16px;">
                    <tr>
                      <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background-color:#ffffff;border-radius:8px;overflow:hidden;border:1px solid #ededf5;">

                          <!-- Header -->
                          <tr>
                            <td style="padding:28px 40px;border-bottom:1px solid #ededf5;">
                              <span style="font-size:22px;font-weight:700;color:#1d1d2e;letter-spacing:-0.3px;text-decoration:none;">cc</span><span style="font-size:22px;font-weight:700;color:#e05520;letter-spacing:-0.3px;">Diary</span>
                            </td>
                          </tr>

                          <!-- Body -->
                          <tr>
                            <td style="padding:40px 40px 32px;">
                              <h1 style="margin:0 0 16px;font-size:24px;color:#1d1d2e;font-weight:600;letter-spacing:-0.3px;">Welcome, {displayName}!</h1>
                              <p style="margin:0 0 16px;font-size:15px;line-height:1.6;color:#4a4a5a;">
                                Great news &#8212; your request to join <strong style="color:#1d1d2e;">ccDiary</strong> has been approved.
                                We&#8217;re delighted to have you on board.
                              </p>
                              <p style="margin:0 0 32px;font-size:15px;line-height:1.6;color:#4a4a5a;">
                                ccDiary is your personal space to capture moments, reflect on memories, and keep your thoughts safe.
                                Click the button below to accept your invitation and get started.
                              </p>

                              <!-- CTA Button -->
                              <table cellpadding="0" cellspacing="0">
                                <tr>
                                  <td style="background-color:#e05520;border-radius:6px;">
                                    <a href="{inviteRedeemUrl}"
                                       style="display:inline-block;padding:13px 28px;color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;letter-spacing:0.1px;">
                                      Accept Invitation
                                    </a>
                                  </td>
                                </tr>
                              </table>

                              <p style="margin:28px 0 0;font-size:12px;color:#9ca3af;line-height:1.6;">
                                If the button above doesn&#8217;t work, copy and paste this link into your browser:<br>
                                <a href="{inviteRedeemUrl}" style="color:#e05520;word-break:break-all;">{inviteRedeemUrl}</a>
                              </p>
                            </td>
                          </tr>

                          <!-- Footer -->
                          <tr>
                            <td style="padding:20px 40px;background-color:#f6f6fb;border-top:1px solid #ededf5;">
                              <p style="margin:0 0 4px;font-size:12px;color:#9ca3af;">
                                If you didn&#8217;t request access to ccDiary, you can safely ignore this email.
                              </p>
                              <p style="margin:0;font-size:12px;color:#c4c4d0;">
                                &#169; {year} ccDiary
                              </p>
                            </td>
                          </tr>

                        </table>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>
                """;
        }
    }
}
