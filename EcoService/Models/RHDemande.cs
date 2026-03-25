using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class RHDemande
    {
        public decimal Montant { get; set; }
        public string TypePret { get; set; }
        public string NomComplet { get; set; }
        public string NumeroCompte { get; set; }
        public DateTime DateNaissance { get; set; }
        public double Taux { get; set; }
        public int NbreEcheances { get; set; }
        public double Quotity { get; set; }
        public int Matricule { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal SalaireNet { get; set; }
    }
}