using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class Demande
    {
        [Key]
        public int DemandeId { get; set; }
        public decimal Montant { get; set; }
        public string TypePret { get; set; }
        public float Taux { get; set; }
        public int NbreEcheances { get; set; }
        public string Status { get; set; } = "Pending";
        public float Quotity { get; set; }
        public int Matricule { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}