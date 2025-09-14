using System;
using MimeKit;

namespace App.Commons;

public class EmailModel
{
    public List<MailboxAddress> To { get; set; }
    public string Subject { get; set; }
    public string? BodyPlainText { get; set; }
    public string? BodyHtml { get; set; }
    public EmailModel(IEnumerable<string> to, string subject, string body)
    {
        To = new List<MailboxAddress>();
        To.AddRange(to.Select(x => new MailboxAddress("email", x)));
        Subject = subject;
        BodyPlainText = body;
        BodyHtml = body;
    }
}