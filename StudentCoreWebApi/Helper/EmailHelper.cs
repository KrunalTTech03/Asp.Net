using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public static class EmailHelper
{
    public static async Task<bool> SendEmailAsync(
        IConfiguration configuration,
        ILogger logger,
        string toEmail,
        string subject,
        string body)
    {
        try
        {
            logger.LogInformation("Preparing to send email to {Email}", toEmail);

            using var smtpClient = new SmtpClient
            {
                Host = configuration["EmailSettings:Host"],
                Port = int.Parse(configuration["EmailSettings:Port"]),
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    configuration["EmailSettings:Username"],
                    configuration["EmailSettings:Password"]
                )
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(configuration["EmailSettings:FromEmail"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            logger.LogInformation("Sending email to {Email}", toEmail);
            await smtpClient.SendMailAsync(mailMessage);
            logger.LogInformation("Email successfully sent to {Email}", toEmail);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {Email}", toEmail);
            return false;
        }
    }
}