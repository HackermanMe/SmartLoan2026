/*
 * =============================================================================
 * INTERFACE DU SERVICE D'EXPORT
 * =============================================================================
 * Definit les methodes d'export disponibles pour le tableau d'amortissement.
 * =============================================================================
 */

using EcoService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoService.Services.Interfaces
{
    /// <summary>
    /// Interface principale pour l'export des documents
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Exporte le tableau d'amortissement en Excel
        /// </summary>
        byte[] ExportToExcel(AmortissementResult result, AmortissementInput input);

        /// <summary>
        /// Exporte le tableau d'amortissement en PDF
        /// </summary>
        byte[] ExportToPdf(AmortissementResult result, AmortissementInput input);

        /// <summary>
        /// Exporte le tableau d'amortissement en Word
        /// </summary>
        byte[] ExportToWord(AmortissementResult result, AmortissementInput input);

        /// <summary>
        /// Exporte un document complet combinant tableau, contrat et fiche d'approbation
        /// </summary>
        byte[] ExportDocumentComplet(AmortissementResult result, AmortissementInput input);
    }
}
