using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class Avis
    {
        [Required]
        public int ID { get; set; }
        [Required]
        [StringLength(14)]
        [Display(Name = "COMPTE")]
        public string COMPTE { get; set; }
        [Required]
        public string INSTITUTION { get; set; }
        [Required]
        [Display(Name = "ADRESSE EMAIL")]
        public string ADRESSEMAIL { get; set; }
        [Required]
        public string CC { get; set; }
        [Required]
        public string avisdebit { get; set; }
        public string aviscredit { get; set; }
        public string INPUTTER { get; set; }
        public string JOURJ { get; set; }
        public string CRJ { get; set; }
        public string DRJ { get; set; }
        public string REGION { get; set; }
    }
}