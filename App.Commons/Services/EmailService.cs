using App.Commons.Interfaces;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace App.Commons.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        this._configuration = configuration;
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
        var client = new MailKit.Net.Smtp.SmtpClient();
        try
        {
            await client.ConnectAsync(smtpServer, port, true);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            await client.AuthenticateAsync(userName, password);
            await client.SendAsync(email);
            return true;
        }
        catch (Exception)
        {
            throw;
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
        emailMessage.From.Add(new MailboxAddress("LERM System", _configuration["EmailConfiguration:From"]));
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