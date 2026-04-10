using EcoService.Models;
using EcoService.Services.Export;
using System;
using System.Collections.Generic;
using System.Linq;
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

            try
            {
                string loginStaff = Session["accountName"] as string;
                if (!string.IsNullOrEmpty(loginStaff))
                {
                    EcoService.Models.RHSqlQuery st = new EcoService.Models.RHSqlQuery();
                    using (System.Data.SqlClient.SqlDataReader staffreadere = st.AccountLogin(loginStaff))
                    {
                        var staffInfo = new Dictionary<string, object>();
                        while (staffreadere.Read())
                        {
                            for (int i = 0; i < staffreadere.FieldCount; i++)
                            {
                                staffInfo[staffreadere.GetName(i)] = staffreadere.GetValue(i);
                            }
                        }

                        // AccountLogin retourne les colonnes avec alias "Nomm", "Prenomm", "NumeroComptee", "SalaireNete"
                        if (staffInfo.ContainsKey("Nomm"))
                        {
                            model.NomDemandeur = Convert.ToString(staffInfo["Nomm"]);
                            if (staffInfo.ContainsKey("Prenomm"))
                                model.NomDemandeur += " " + Convert.ToString(staffInfo["Prenomm"]);
                        }
                        else if (staffInfo.ContainsKey("Nom"))
                        {
                            model.NomDemandeur = Convert.ToString(staffInfo["Nom"]);
                            if (staffInfo.ContainsKey("Prenom"))
                                model.NomDemandeur += " " + Convert.ToString(staffInfo["Prenom"]);
                        }

                        if (staffInfo.ContainsKey("NumeroComptee"))
                            model.NumeroCompte = Convert.ToString(staffInfo["NumeroComptee"]);
                        else if (staffInfo.ContainsKey("NumeroCompte"))
                            model.NumeroCompte = Convert.ToString(staffInfo["NumeroCompte"]);

                        if (staffInfo.ContainsKey("SalaireNete"))
                            model.SalaireNetActuel = Convert.ToDecimal(staffInfo["SalaireNete"]);
                        else if (staffInfo.ContainsKey("SalaireNet"))
                            model.SalaireNetActuel = Convert.ToDecimal(staffInfo["SalaireNet"]);

                        if (!string.IsNullOrEmpty(model.NumeroCompte))
                        {
                            // Charger tous les prêts existants actifs Ecobank
                            model.PretsExistantsEcobank = st.GetPretsExistantsActifs(model.NumeroCompte);

                            // Remplir les champs de compatibilité (premier prêt uniquement)
                            using (System.Data.SqlClient.SqlDataReader pretsReader = st.PretExistantsStaff(model.NumeroCompte))
                            {
                                int pretCount = 0;
                                while (pretsReader.Read())
                                {
                                    if (pretCount == 0)
                                    {
                                        try { model.TypePretExistant = Convert.ToString(pretsReader["TypeCredit"]); } catch {}
                                        try { model.MontantPretExistant = Convert.ToDecimal(pretsReader["Montant"]); } catch {}
                                        try { model.MensualitesPretExistant = Convert.ToDecimal(pretsReader["Mensualites"]); } catch {}
                                    }
                                    pretCount++;
                                }
                            }

                            // Charger TOUS les autres crédits existants
                            var existingLoans = st.GetExistingLoans(model.NumeroCompte);
                            if (existingLoans != null && existingLoans.Count > 0)
                            {
                                // Convertir en liste d'objets typés
                                model.AutresCreditsExistants = existingLoans.ConvertAll(loan => new AutresCreditExistant
                                {
                                    TypeDeCredit = loan.ContainsKey("TypeDeCredit") ? Convert.ToString(loan["TypeDeCredit"]) : "",
                                    NomBanque = loan.ContainsKey("NomBanque") ? Convert.ToString(loan["NomBanque"]) : "",
                                    StartDate = loan.ContainsKey("StartDate") && loan["StartDate"] != DBNull.Value ? Convert.ToDateTime(loan["StartDate"]) : (DateTime?)null,
                                    EndDate = loan.ContainsKey("EndDate") && loan["EndDate"] != DBNull.Value ? Convert.ToDateTime(loan["EndDate"]) : (DateTime?)null,
                                    Montant = loan.ContainsKey("Montant") ? Convert.ToDecimal(loan["Montant"]) : 0,
                                    EnCours = loan.ContainsKey("EnCours") ? Convert.ToDecimal(loan["EnCours"]) : 0,
                                    Mensualites = loan.ContainsKey("Mensualites") ? Convert.ToDecimal(loan["Mensualites"]) : 0
                                });

                                // Calculer la somme TOTALE de toutes les mensualités pour compatibilité avec Excel
                                decimal totalMensualitesAutres = model.AutresCreditsExistants.Sum(c => c.Mensualites);
                                model.MensualitesAutresCredits1 = totalMensualitesAutres;
                                model.MensualitesAutresCredits2 = 0; // Plus utilisé, on met tout dans Credits1

                                // Remplir les types pour les 2 premiers (pour compatibilité visuelle)
                                if (existingLoans.Count > 0)
                                {
                                   try { model.TypeAutresCredits1 = Convert.ToString(existingLoans[0]["TypeDeCredit"]); } catch {}
                                   try { model.MontantAutresCredits1 = Convert.ToDecimal(existingLoans[0]["Montant"]); } catch {}
                                }

                                if (existingLoans.Count > 1)
                                {
                                   try { model.TypeAutresCredits2 = Convert.ToString(existingLoans[1]["TypeDeCredit"]); } catch {}
                                   try { model.MontantAutresCredits2 = Convert.ToDecimal(existingLoans[1]["Montant"]); } catch {}
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Continue if DB fetching fails
            }

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
                // Recharger les prêts existants Ecobank et autres crédits pour l'export Excel
                if (!string.IsNullOrEmpty(model.NumeroCompte))
                {
                    EcoService.Models.RHSqlQuery st = new EcoService.Models.RHSqlQuery();
                    model.PretsExistantsEcobank = st.GetPretsExistantsActifs(model.NumeroCompte);

                    // Recharger tous les autres crédits
                    var existingLoans = st.GetExistingLoans(model.NumeroCompte);
                    if (existingLoans != null && existingLoans.Count > 0)
                    {
                        model.AutresCreditsExistants = existingLoans.ConvertAll(loan => new AutresCreditExistant
                        {
                            TypeDeCredit = loan.ContainsKey("TypeDeCredit") ? Convert.ToString(loan["TypeDeCredit"]) : "",
                            NomBanque = loan.ContainsKey("NomBanque") ? Convert.ToString(loan["NomBanque"]) : "",
                            StartDate = loan.ContainsKey("StartDate") && loan["StartDate"] != DBNull.Value ? Convert.ToDateTime(loan["StartDate"]) : (DateTime?)null,
                            EndDate = loan.ContainsKey("EndDate") && loan["EndDate"] != DBNull.Value ? Convert.ToDateTime(loan["EndDate"]) : (DateTime?)null,
                            Montant = loan.ContainsKey("Montant") ? Convert.ToDecimal(loan["Montant"]) : 0,
                            EnCours = loan.ContainsKey("EnCours") ? Convert.ToDecimal(loan["EnCours"]) : 0,
                            Mensualites = loan.ContainsKey("Mensualites") ? Convert.ToDecimal(loan["Mensualites"]) : 0
                        });

                        // Calculer la somme TOTALE pour compatibilité avec le service Excel
                        decimal totalMensualitesAutres = model.AutresCreditsExistants.Sum(c => c.Mensualites);
                        model.MensualitesAutresCredits1 = totalMensualitesAutres;
                        model.MensualitesAutresCredits2 = 0;
                    }
                }

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
                // Recharger les prêts existants Ecobank et autres crédits si nécessaire
                if (!string.IsNullOrEmpty(model.NumeroCompte))
                {
                    EcoService.Models.RHSqlQuery st = new EcoService.Models.RHSqlQuery();
                    model.PretsExistantsEcobank = st.GetPretsExistantsActifs(model.NumeroCompte);

                    // Recharger tous les autres crédits
                    var existingLoans = st.GetExistingLoans(model.NumeroCompte);
                    if (existingLoans != null && existingLoans.Count > 0)
                    {
                        model.AutresCreditsExistants = existingLoans.ConvertAll(loan => new AutresCreditExistant
                        {
                            TypeDeCredit = loan.ContainsKey("TypeDeCredit") ? Convert.ToString(loan["TypeDeCredit"]) : "",
                            NomBanque = loan.ContainsKey("NomBanque") ? Convert.ToString(loan["NomBanque"]) : "",
                            StartDate = loan.ContainsKey("StartDate") && loan["StartDate"] != DBNull.Value ? Convert.ToDateTime(loan["StartDate"]) : (DateTime?)null,
                            EndDate = loan.ContainsKey("EndDate") && loan["EndDate"] != DBNull.Value ? Convert.ToDateTime(loan["EndDate"]) : (DateTime?)null,
                            Montant = loan.ContainsKey("Montant") ? Convert.ToDecimal(loan["Montant"]) : 0,
                            EnCours = loan.ContainsKey("EnCours") ? Convert.ToDecimal(loan["EnCours"]) : 0,
                            Mensualites = loan.ContainsKey("Mensualites") ? Convert.ToDecimal(loan["Mensualites"]) : 0
                        });

                        // Calculer la somme TOTALE pour compatibilité avec le service de génération
                        decimal totalMensualitesAutres = model.AutresCreditsExistants.Sum(c => c.Mensualites);
                        model.MensualitesAutresCredits1 = totalMensualitesAutres;
                        model.MensualitesAutresCredits2 = 0;
                    }
                }

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
