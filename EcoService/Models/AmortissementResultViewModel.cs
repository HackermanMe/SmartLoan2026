using System;
using System.Collections.Generic;

namespace EcoService.Models
{
    /// <summary>
    /// Modèle de données pour l'export du dossier complet Word
    /// </summary>
    public class AmortissementResultViewModel
    {
        // Informations du prêt
        public decimal MontantPret { get; set; }
        public decimal TauxAnnuel { get; set; }
        public decimal TauxTAF { get; set; }
        public decimal TauxTTC { get; set; }
        public int NombreEcheances { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime? DateDeblocage { get; set; }
        public string ObjetCredit { get; set; }

        // Mensualités et frais
        public decimal MensualiteHT { get; set; }
        public decimal MensualiteTTC { get; set; }
        public decimal AssuranceParEcheance { get; set; }
        public decimal FraisDossier { get; set; }

        // Totaux
        public decimal TotalInterets { get; set; }
        public decimal TotalTAF { get; set; }
        public decimal TotalAssurance { get; set; }
        public decimal CoutTotalCredit { get; set; }
        public decimal TEGCalcule { get; set; }

        // Capacité d'endettement
        public decimal SalaireMensuel { get; set; }
        public decimal TauxEndettement { get; set; }

        // Informations client
        public Dictionary<string, string> ClientInfo { get; set; }

        // Lignes du tableau d'amortissement
        public List<LigneAmortissement> LignesAmortissement { get; set; }

        public AmortissementResultViewModel()
        {
            ClientInfo = new Dictionary<string, string>();
            LignesAmortissement = new List<LigneAmortissement>();
        }
    }
}
