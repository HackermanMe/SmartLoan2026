/*
 * =============================================================================
 * SERVICE D'EXPORT PRINCIPAL
 * =============================================================================
 * Service facade qui orchestre les exports vers les differents formats.
 * Delegue le travail aux services specialises:
 * - IExportExcelService pour Excel
 * - IExportPdfService pour PDF
 * - IExportWordService pour Word
 * - IDocumentCombineService pour le document combine
 * =============================================================================
 */

using EcoService.Models;
using EcoService.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EcoService.Services.Export
{
    /// <summary>
    /// Implementation du service d'export principal
    /// </summary>
    public class ExportService : IExportService
    {
        private readonly IExportExcelService _excelService;
        private readonly IExportPdfService _pdfService;
        private readonly IExportWordService _wordService;
        private readonly IDocumentCombineService _documentCombineService;

        public ExportService(
            IExportExcelService excelService,
            IExportPdfService pdfService,
            IExportWordService wordService,
            IDocumentCombineService documentCombineService)
        {
            _excelService = excelService;
            _pdfService = pdfService;
            _wordService = wordService;
            _documentCombineService = documentCombineService;
        }


        /// <summary>
        /// Exporte le tableau d'amortissement en Excel
        /// </summary>
        public byte[] ExportToExcel(AmortissementResult result, AmortissementInput input)
        {
            return _excelService.ExportToExcel(result, input);
        }

        /// <summary>
        /// Exporte le tableau d'amortissement en PDF
        /// </summary>
        public byte[] ExportToPdf(AmortissementResult result, AmortissementInput input)
        {
            return _pdfService.ExportToPdf(result, input);
        }

        /// <summary>
        /// Exporte le tableau d'amortissement en Word
        /// </summary>
        public byte[] ExportToWord(AmortissementResult result, AmortissementInput input)
        {
            return _wordService.ExportToWord(result, input);
        }

        /// <summary>
        /// Exporte un document complet combinant tous les elements
        /// </summary>
        public byte[] ExportDocumentComplet(AmortissementResult result, AmortissementInput input)
        {
            return _documentCombineService.GenererDocumentComplet(result, input);
        }
    }
}