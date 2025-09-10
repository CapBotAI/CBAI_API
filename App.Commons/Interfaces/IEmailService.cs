using System;

namespace App.Commons.Interfaces;

public interface IEmailService
{
    public Task<bool> SendEmailAsync(EmailModel emailModel);
}