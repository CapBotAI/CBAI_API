using App.Commons.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace App.Commons.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;


    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        this._configuration = configuration;
        this._logger = logger;

    }
    public async Task<bool> SendEmailAsync(EmailModel emailModel)
    {
        if (emailModel.To == null || !emailModel.To.Any())
        {
            throw new ArgumentException("The recipient email address cannot be null or empty.");
        }

        var emailConfig = _configuration.GetSection("EmailConfiguration");
        var smtpServer = emailConfig["SmtpServer"];
        int port = Convert.ToInt32(emailConfig["Port"]);
        var from = emailConfig["From"];
        var userName = emailConfig["UserName"];
        var password = emailConfig["Password"];

        if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(from) ||
            string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException("SMTP configuration is not complete.");
        }

        var email = CreateEmailMessage(emailModel);
        using var client = new MailKit.Net.Smtp.SmtpClient
        {
            Timeout = 10000
        };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await client.ConnectAsync(smtpServer, port, MailKit.Security.SecureSocketOptions.SslOnConnect, cts.Token);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            await client.AuthenticateAsync(userName, password, cts.Token);
            await client.SendAsync(email, cts.Token);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex.StackTrace, "Error occurred while sending email.");
            return false; // không throw để tránh làm fail logic gọi
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true, CancellationToken.None);
            client.Dispose();
        }
    }



    #region PRIVATE
    /// <summary>
    /// This is used to create an email by Mailkit
    /// </summary>
    /// <param name="emailModel"></param>
    /// <returns></returns>
    private MimeMessage CreateEmailMessage(EmailModel emailModel)
    {
        var emailMessage = new MimeMessage();
        var displayName = _configuration["EmailConfiguration:DisplayName"] ?? "CapBot Team";
        emailMessage.From.Add(new MailboxAddress(displayName, _configuration["EmailConfiguration:From"]));
        emailMessage.To.AddRange(emailModel.To);
        emailMessage.Subject = emailModel.Subject;
        emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = emailModel.BodyHtml };

        // // Tạo nội dung email có cả plain text và HTML
        // var bodyBuilder = new BodyBuilder
        // {
        //     TextBody = emailModel.BodyPlainText, // Nội dung plain text
        //     HtmlBody = emailModel.BodyHtml // Nội dung HTML
        // };

        // emailMessage.Body = bodyBuilder.ToMessageBody();

        return emailMessage;
    }
    #endregion
}