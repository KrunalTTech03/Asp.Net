using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StudentCoreWebApi.Enums;

public class EmailServices
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailServices> _logger;

    public EmailServices(IConfiguration configuration, ILogger<EmailServices> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            _logger.LogInformation("Preparing to send email to {Email}", toEmail);

            var smtpClient = new SmtpClient
            {
                Host = _configuration["EmailSettings:Host"],
                Port = int.Parse(_configuration["EmailSettings:Port"]),
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    _configuration["EmailSettings:Username"],
                    _configuration["EmailSettings:Password"]
                )
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["EmailSettings:FromEmail"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            _logger.LogInformation("Sending email to {Email}", toEmail);
            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email successfully sent to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendTemplatedEmailAsync(
        EmailTemplateType templateType,
        string recipientEmail,
        string subject,
        Dictionary<string, string> placeholders)
    {
        try
        {
            string templateFileName = templateType switch
            {
                EmailTemplateType.UserCreated => "NewUserAdded.html",
                EmailTemplateType.UserRegistered => "UserRegistration.html",
                _ => throw new ArgumentOutOfRangeException(nameof(templateType), "Invalid email template type")
            };

            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", templateFileName);

            if (!File.Exists(templatePath))
            {
                _logger.LogError("Email template file not found: {Path}", templatePath);
                return false;
            }

            string emailTemplate = await File.ReadAllTextAsync(templatePath);

            foreach (var placeholder in placeholders)
            {
                emailTemplate = emailTemplate.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
            }

            return await SendEmailAsync(recipientEmail, subject, emailTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing or sending templated email to {Email}", recipientEmail);
            return false;
        }
    }
}