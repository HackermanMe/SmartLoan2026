using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class EcoCerEmailSettings
    {
        public string SmtpServer { get; set; }

        public int Port { get; set; }

        public string SenderEmail { get; set; }

        public bool EnableSSL { get; set; }
    }
}