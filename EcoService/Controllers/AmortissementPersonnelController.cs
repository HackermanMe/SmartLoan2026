using EcoService.Models;
using EcoService.Services.Export;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace EcoService.Controllers
{
    [Authorize]
    public class AmortissementRHController : Controller
    {
        private readonly ExportExcelPersonnelService _excelService;
        private readonly BilletOrdreService _billetService;

        // Durées max par type de prêt (en mois)
        private static readonly Dictionary<string, int> MaxMoisParType = new Dictionary<string, int>
        {
            { "Néant",                                 0 },
            { "Prêt d'urgence",                        12 },
            { "Prêt personnel",                        60 },
            { "Prêt automobile (véhicule d'occasion)", 60 },
            { "Prêt automobile (véhicule neuf)",       60 },
            { "Achat de terrain",                      60 },
            { "Prêt de scolarisation",                 12 },
            { "Prêt immobilier",                       180 },
            { "Prêt aménagement habitat",              60 },
            { "Prêt aménagement gros oeuvres",         120 }
        };

        public AmortissementRHController()
        {
            _excelService = new ExportExcelPersonnelService();
            _billetService = new BilletOrdreService();
        }

        /// <summary>Vérifie que le nombre d'échéances ne dépasse pas le max du type de prêt</summary>
        private void ValiderEcheancesMax(EtatPretsPersonnelInput model)
        {
            int maxMois;
            if (!MaxMoisParType.TryGetValue(model.TypePret ?? "", out maxMois))
                maxMois = 180;

            if (maxMois == 0)
            {
                ModelState.AddModelError("TypePret",
                    "Veuillez sélectionner un type de prêt valide (pas \"Néant\").");
            }
            else if (model.NbreEcheances > maxMois)
            {
                ModelState.AddModelError("NbreEcheances",
                    $"Le nombre d'échéances ne peut pas dépasser {maxMois} mois pour le type \"{model.TypePret}\".");
            }
        }

        // GET /AmortissementRH
        [HttpGet]
        public ActionResult Index()
        {
            var model = new EtatPretsPersonnelInput
            {
                DateDebut = DateTime.Today,
                TauxTAF = 10,
                TypePret = "Prêt personnel",
                TypePretExistant = "Néant",
                TypeAutresCredits1 = "Néant",
                TypeAutresCredits2 = "Néant"
            };

            ViewBag.TypesPret = ExportExcelPersonnelService.TypesPret;
            return View(model);
        }

        // POST /AmortissementRH/Generer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Generer(EtatPretsPersonnelInput model)
        {
            ValiderEcheancesMax(model);

            if (!ModelState.IsValid)
            {
                ViewBag.TypesPret = ExportExcelPersonnelService.TypesPret;
                return View("Index", model);
            }

            try
            {
                var bytes = _excelService.Generer(model);
                string nom = string.IsNullOrWhiteSpace(model.NomDemandeur)
                    ? "Personnel"
                    : model.NomDemandeur.Replace(" ", "_");
                string fichier = $"EtatPrets_{nom}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fichier
                );
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erreur lors de la génération : {ex.Message}");
                ViewBag.TypesPret = ExportExcelPersonnelService.TypesPret;
                return View("Index", model);
            }
        }

        // POST /AmortissementRH/GenererBillet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenererBillet(EtatPretsPersonnelInput model)
        {
            ValiderEcheancesMax(model);

            if (!ModelState.IsValid)
            {
                ViewBag.TypesPret = ExportExcelPersonnelService.TypesPret;
                return View("Index", model);
            }

            try
            {
                // Mêmes calculs que pour l'Excel
                decimal tauxTAF = model.TauxTAF;
                decimal rMoisTTC = model.TauxAnnuel * (1m + tauxTAF / 100m) / 12m / 100m;
                int nEff = Math.Max(1, model.NbreEcheances - model.NbreDifferes);
                decimal mensualite = PMTHelper(model.Montant, rMoisTTC, nEff);
                DateTime dateFin = model.DateDebut.AddMonths(model.NbreEcheances);

                var bytes = _billetService.Generer(model, mensualite, dateFin);

                string nom = string.IsNullOrWhiteSpace(model.NomDemandeur)
                    ? "Personnel"
                    : model.NomDemandeur.Replace(" ", "_");
                string fichier = $"BilletOrdre_{nom}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    fichier
                );
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erreur lors de la génération du billet : {ex.Message}");
                ViewBag.TypesPret = ExportExcelPersonnelService.TypesPret;
                return View("Index", model);
            }
        }

        /// <summary>Calcul PMT identique à ExportExcelPersonnelService</summary>
        private static decimal PMTHelper(decimal montant, decimal rMois, int n)
        {
            if (rMois == 0m) return montant / n;
            double r = (double)rMois;
            double pmt = (double)montant * r / (1.0 - Math.Pow(1.0 + r, -n));
            return (decimal)pmt;
        }
    }
}
