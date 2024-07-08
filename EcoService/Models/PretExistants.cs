using System;

namespace EcoService.Models
{
    public class PretExistants
    {

        public int PretId { get; set; }

        public string ReferencePret { get; set; }

        public string NumeroCompte { get; set; }

        public int Montant { get; set; }

        public int EnCours { get; set; }

        public float Taux { get; set; }

        public string TypeCredit { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Mensualites { get; set; }
    }
}

