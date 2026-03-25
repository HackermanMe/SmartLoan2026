using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class EcoCerCertificateTemplate
    {
        public int CerTempId { get; set; }

        public string? HeaderText { get; set; }

        public string? TitleText { get; set; }

        public string? BodyTextPart1 { get; set; }

        public string? BodyTextPart2 { get; set; }

        public string? BodyTextPart3 { get; set; }

        public string? BodyTextPart4 { get; set; }

        public string? BodyTextPart5 { get; set; }

        public string? DeliverDateText { get; set; }

        public string? FooterTextPart1 { get; set; }

        public string? FooterTextPart2 { get; set; }
    }
}