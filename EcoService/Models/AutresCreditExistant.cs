using System;

namespace EcoService.Models
{
    /// <summary>
    /// Représente un crédit existant dans une autre banque
    /// </summary>
    public class AutresCreditExistant
    {
        public int APretId { get; set; }
        public string TypeDeCredit { get; set; }
        public string NomBanque { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Montant { get; set; }
        public decimal EnCours { get; set; }
        public decimal Mensualites { get; set; }
    }
}
