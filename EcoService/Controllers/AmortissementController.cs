/*
 * =============================================================================
 * CONTROLEUR POUR LE TABLEAU D'AMORTISSEMENT
 * =============================================================================
 * Ce controleur gere toutes les interactions liees au tableau d'amortissement.
 * Il suit le pattern MVC et expose des endpoints JSON pour jQuery.
 *
 * Endpoints:
 * - GET  /Amortissement              : Affiche la page principale
 * - POST /Amortissement/Calculer     : Calcule et retourne le tableau en JSON
 * - POST /Amortissement/ExportExcel  : Exporte en Excel
 * - POST /Amortissement/ExportPdf    : Exporte en PDF
 * - POST /Amortissement/ExportWord   : Exporte en Word
 * - POST /Amortissement/ExportComplet: Exporte le document complet combine
 * =============================================================================
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using EcoService.Models;
using EcoService.Services.Amortissement;
using EcoService.Services.Export;
using EcoService.Services.Interfaces;
using ActionResult = System.Web.Mvc.ActionResult;
using FromBodyAttribute = System.Web.Http.FromBodyAttribute;
//using Microsoft.AspNetCore.Mvc;

namespace EcoService.Controllers
{
    public class AmortissementController : Controller
    {
        // =====================================================================
        // INJECTION DE DEPENDANCES
        // =====================================================================
        private readonly IAmortissementService _amortissementService;
        private readonly IExportService _exportService;

        public AmortissementController()
        {
            // Instanciation des dépendances
            ICalculHelper calculHelper = new CalculHelper();
            ICapaciteEndettementService capaciteService = new CapaciteEndettementService();
            IAlerteService alerteService = new AlerteService();
            ITEGCalculator tEGCalculator = new TEGCalculator();

            IExportExcelService exportExcelService = new ExportExcelService();
            IExportPdfService exportPdfService = new ExportPdfService();
            IExportWordService exportWordService = new ExportWordService();
            IDocumentCombineService documentCombineService = new DocumentCombineService();


            //Instanciation du service principal
            _amortissementService = new AmortissementService(tEGCalculator,alerteService,capaciteService,calculHelper);

            //Export Service
            _exportService = new ExportService(exportExcelService,exportPdfService,exportWordService,documentCombineService);
        }

        /// Constructeur avec injection des services
        public AmortissementController(
            IAmortissementService amortissementService,
            IExportService exportService)
        {
            _amortissementService = amortissementService;
            _exportService = exportService;
        }

        /* =====================================================================
         * ACTION : PAGE PRINCIPALE
         * =====================================================================
         * Affiche le formulaire de saisie et le tableau d'amortissement.
         * GET /Amortissement
         * ===================================================================== */
        [System.Web.Mvc.HttpGet]
        public ActionResult Index()
        {
            // Modele vide avec valeurs par defaut
            var model = new AmortissementInput
            {
                DateDebut = DateTime.Today,
                TauxTAF = 10,
                Periodicite = Periodicite.Mensuel
            };

            return View(model);
        }

        /* =====================================================================
         * ACTION : CALCUL DU TABLEAU (API JSON)
         * =====================================================================
         * Recoit les parametres en JSON, calcule le tableau et le retourne.
         * POST /Amortissement/Calculer
         *
         * Cette methode est appelee via AJAX par jQuery.
         * ===================================================================== */
        [System.Web.Mvc.HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Calculer(AmortissementInput input)
        {
            try
            {
                // Validation du modele
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new { success = false, errors });
                }

                // Calcul du tableau d'amortissement
                var result = _amortissementService.CalculerTableau(input);

                return Json(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                // Erreur de validation metier
                return Json(new { success = false, errors = new[] { ex.Message } });
            }
            catch (Exception)
            {
                // Erreur inattendue (log en production)
                return Json(new
                {
                    success = false,
                    errors = new[] { "Une erreur est survenue lors du calcul." }
                });
            }
        }

        /* =====================================================================
         * ACTION : CALCUL RAPIDE DE LA MENSUALITE (API JSON)
         * =====================================================================
         * Calcule uniquement la mensualite pour affichage en temps reel.
         * POST /Amortissement/CalculerMensualite
         * ===================================================================== */
        [System.Web.Mvc.HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult CalculerMensualite(AmortissementInput input)
        {
            try
            {
                var mensualite = _amortissementService.CalculerMensualite(
                    input.MontantPret,
                    input.TauxAnnuel,
                    input.NombreEcheances - input.DiffereEcheances,
                    input.Periodicite,
                    input.TauxTAF
                );

                return Json(new { success = true, mensualite });
            }
            catch
            {
                return Json(new { success = false, mensualite = 0 });
            }
        }

        /* =====================================================================
         * ACTION : EXPORT EXCEL
         * =====================================================================
         * Genere et telecharge le tableau au format Excel (.xlsx)
         * POST /Amortissement/ExportExcel
         * ===================================================================== */
        [System.Web.Mvc.HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult ExportExcel(AmortissementInput input)
        {
            try
            {
                // Calcul du tableau
                var result = _amortissementService.CalculerTableau(input);

                // Generation du fichier Excel
                var fileContent = _exportService.ExportToExcel(result, input);
                // Nom du fichier avec date
                string fileName = $"Tableau_Amortissement_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(
                    fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Erreur lors de la generation du fichier Excel.");
                return new HttpStatusCodeResult(500, ex.ToString());
            }
        }

        /* =====================================================================
         * ACTION : EXPORT PDF
         * =====================================================================
         * Genere et telecharge le tableau au format PDF
         * POST /Amortissement/ExportPdf
         * ===================================================================== */
        [System.Web.Mvc.HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult ExportPdf(AmortissementInput input)
        {
            try
            {
                // Calcul du tableau
                var result = _amortissementService.CalculerTableau(input);

                // Generation du fichier PDF
                var fileContent = _exportService.ExportToPdf(result, input);

                // Nom du fichier avec date
                string fileName = $"Tableau_Amortissement_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(fileContent, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                // Log l'erreur complete pour le debug
                Console.WriteLine($"Erreur PDF: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");

                // Retourne l'erreur detaillee en developpement
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Erreur PDF: {ex.Message}");
            }
        }

        /* =====================================================================
         * ACTION : EXPORT WORD
         * =====================================================================
         * Genere et telecharge le tableau au format Word (.docx)
         * POST /Amortissement/ExportWord
         * ===================================================================== */
        [System.Web.Mvc.HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult ExportWord(  AmortissementInput input)
        {
            try
            {
                // Calcul du tableau
                var result = _amortissementService.CalculerTableau(input);

                // Generation du fichier Word
                var fileContent = _exportService.ExportToWord(result, input);

                // Nom du fichier avec date
                string fileName = $"Tableau_Amortissement_{DateTime.Now:yyyyMMdd_HHmmss}.docx";

                return File(
                    fileContent,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    fileName
                );
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Erreur lors de la generation du fichier Word.");
            }
        }

        /* =====================================================================
       * ACTION : EXPORT DOCUMENT COMPLET
       * =====================================================================
       * Genere et telecharge un document PDF complet contenant:
       * - Page de garde
       * - Resume du pret
       * - Tableau d'amortissement
       * - Contrat de pret
       * - Fiche d'approbation
       * POST /Amortissement/ExportComplet
       * ===================================================================== */
        [System.Web.Mvc.HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult ExportComplet(  AmortissementInput input)
        {
            try
            {
                // Calcul du tableau
                var result = _amortissementService.CalculerTableau(input);

                // Generation du document complet
                var fileContent = _exportService.ExportDocumentComplet(result, input);

                // Nom du fichier avec date
                string clientNom = result.Client?.Nom ?? result.NomClient ?? "Client";
                string fileName = $"Dossier_Credit_{clientNom}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(fileContent, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur Document Complet: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");

                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Erreur lors de la generation du document complet: {ex.Message}");
            }
        }

    }
}
