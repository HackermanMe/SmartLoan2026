using System;

namespace EcoService.Models
{
    /// <summary>
    /// Représente un prêt existant Ecobank d'un employé
    /// </summary>
    public class PretExistantEcobank
    {
        public int PretId { get; set; }
        public string ReferencePret { get; set; }
        public string TypeCredit { get; set; }
        public decimal Montant { get; set; }
        public decimal EnCours { get; set; }
        public decimal Taux { get; set; }
        public decimal Mensualites { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string NumeroCompte { get; set; }
    }
}
