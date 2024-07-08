using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class Releve
    {
        public int ID { get; set; }
        [Required]
        public string COMPTE { get; set; }
        [Required]
        public string ADRESSE { get; set; }
        [Required]
        public string CC { get; set; }
        public string JOURJ { get; set; }
        public string INSTITUTION { get; set; }
        [Required]
        public string SEND { get; set; }
        public string INPUTTER { get; set; }
        [Required]
        public bool HEBO { get; set; }
        [Required]
        public bool MENS { get; set; }
        public string CRJ { get; set; }
        public string DRJ { get; set; }
        public string REGION { get; set; }


    }
}