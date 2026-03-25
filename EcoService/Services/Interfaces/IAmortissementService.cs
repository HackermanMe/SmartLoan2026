using EcoService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoService.Services.Interfaces
{
    /// <summary>
    /// Interface principale du service d'amortissement
    /// </summary>
    public interface IAmortissementService
    {
        /// <summary>
        /// Calcule le tableau d'amortissement complet a partir des parametres
        /// </summary>
        AmortissementResult CalculerTableau(AmortissementInput input);

        /// <summary>
        /// Calcule uniquement la mensualite (pour affichage rapide)
        /// </summary>
        decimal CalculerMensualite(decimal montant, decimal tauxAnnuel, int nombreEcheances,
            Periodicite periodicite, decimal tauxTAF = 0, TypeTaux typeTaux = TypeTaux.Proportionnel);
    }
}
