using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class SimulationViewModel
    {
        public decimal MontantEmprunte { get; set; }
        public decimal AnnualRate { get; set; }
        public int Months { get; set; }
        public decimal NetSalary { get; set; }
        public List<int> SelectedLoanIds { get; set; }
        public List<decimal> AutresPrets { get; set; }
    }

}