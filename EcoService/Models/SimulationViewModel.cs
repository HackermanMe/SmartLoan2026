using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class SimulationViewModel
    {
        public string TypeDePret {  get; set; }
        public decimal Montant { get; set; }
        public decimal AnnualRate { get; set; }
        public decimal Quotity { get; set; }
        public int Months { get; set; }
        public decimal NetSalary { get; set; }
        public decimal Remboursement { get; set; }
        public int Matricule { get; set; }
        public int Mensualites { get; set; }
        public List<int> SelectedLoanIds { get; set; }
        public List<decimal> AutresPrets { get; set; }
    }

}