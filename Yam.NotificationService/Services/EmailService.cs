﻿using Neo4jClient;
using System.Net.Mail;
using System.Net;
using Yam.NotificationService.Core.Interfaces;

namespace Yam.NotificationService.Services
{
    public class EmailService(ILogger logger) : IMailService
    {
        //private readonly IGraphClient _graphClient = graphClient;
        private readonly ILogger _logger = logger;

        public async Task SendVerificationMail(string email, string verificationCode)
        {
            
            
                #region SendingTheMail
                var smtpClient = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("yam.socialplatform@gmail.com", "xpjd zaiy vakp nutn"),
                    EnableSsl = true // This ensures a secure connection
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("yam.socialplatform@gmail.com"),
                    Subject = "Email Verification",
                    Body = $"Your verification code is {verificationCode}.",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                #endregion
            
        }
    }
}
