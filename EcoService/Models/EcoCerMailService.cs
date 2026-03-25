using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Web.Configuration;

namespace EcoService.Models
{
    public class EcoCerMailService
    {
        private readonly IConfiguration? _configuration;

        //private string _configuration;

        private readonly string? smtpServer;

        private readonly int smtpPort;

        private readonly string? fromEmail;

        private readonly bool enableSsl;

        private readonly EcoCerLogger _logger;

        public EcoCerMailService()
        {
            _logger = new EcoCerLogger();

            //Recuperation des parametres de connexion mail*    smtpServer = WebConfigurationManager.AppSettings["smtpServer"].ToString();
            smtpServer = WebConfigurationManager.AppSettings["SmtpServer"].ToString();
            smtpPort = Convert.ToInt32(WebConfigurationManager.AppSettings["Port"]);
            fromEmail = WebConfigurationManager.AppSettings["SenderEmail"].ToString();
            enableSsl = Convert.ToBoolean(WebConfigurationManager.AppSettings["EnableSSL"]);

        }

        //public EcoCerMailService(IConfiguration configuration)
        public EcoCerMailService(IConfiguration configuration)
        {
            /*_configuration = configuration;
            smtpServer = _configuration["EmailSettings:SmtpServer"];
            smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
            fromEmail = _configuration["EmailSettings:SenderEmail"];
            enableSsl = bool.Parse(_configuration["EmailSettings:EnableSSL"]);*/

            //_connectionString = WebConfigurationManager.AppSettings["RHExportPath"].ToString()
            _logger = new EcoCerLogger();

        }

        public void SendEmail(string toEmail, string mailSubject, string mailBody)
        {
            try
            {
                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = enableSsl;
                    /*client.UseDefaultCredentials = true;*/

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail),
                        Subject = mailSubject,
                        Body = mailBody,
                        IsBodyHtml = true,

                    };

                    mailMessage.To.Add(toEmail);

                    client.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in SendEmail method: ", ex);
                throw;
            }
        }
    }
}
