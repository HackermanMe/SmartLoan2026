using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class EcoCerUserDetails
    {
        [MaxLength(25)]
        public string? Login { get; set; }

        [MaxLength(100)]
        public string? Nom { get; set; }

        [MaxLength(100)]
        public string? Prenom { get; set; }

        [MaxLength(7)]
        public String? Sexe { get; set; }

        [MaxLength(10)]
        public String? Civilite { get; set; }

        [MaxLength(20)]
        public string? CategorieProfessionnelle { get; set; }

        //[DataType(DataType.Date)]
        public string? DateRecrutement { get; set; }
        //public DateTime? DateRecrutement { get; set; }
    }
}