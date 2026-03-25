using EcoService.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Fluent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using NPOI.Util;
using System.IO.Compression;
using System.IO;
using DocumentFormat.OpenXml.Drawing;
using NPOI.SS.Formula.Functions;
using System.Text.RegularExpressions;

namespace EcoService.Controllers
{
    public class RHDemandeDePretController : Controller
    {
        // Logger NLog
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        // Instance unique de RHSqlQuery pour l'utilisation dans le contrôleur
        private readonly RHSqlQuery _sqlQuery = new RHSqlQuery();
        private int quotiteCessible = 50;

        // GET: RHDemandeDePret
        public ActionResult Index()
        {
            return View();
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult CalculateMonthlyPayment(decimal MontantEmprunte, decimal annualRate, int months, decimal netSalary, List<int> selectedLoanIds, List<decimal> autresPrets)
        {
            try
            {
                if (MontantEmprunte <= 0 || annualRate < 0 || months <= 0 || netSalary <= 0)
                {
                    var errorObject = new { error = "Veuillez saisir des valeurs valides" };
                    return Json(errorObject, JsonRequestBehavior.AllowGet);
                }

                decimal totalMonthlyPayments;
                int monthlyPayment;
                var monthlyRate = (annualRate + annualRate / 10) / 100 / 12;
                var totalMonths = months;

                if (monthlyRate != 0)
                {
                    monthlyPayment = (int)Math.Round((MontantEmprunte * monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), totalMonths)) /
                                                      ((decimal)Math.Pow((double)(1 + monthlyRate), totalMonths) - 1));
                    totalMonthlyPayments = monthlyPayment;

                    // Ajouter les mensualités des autres prêts
                    if (autresPrets != null)
                    {
                        foreach (var loan in autresPrets)
                        {
                            totalMonthlyPayments -= loan;
                        }
                    }

                    // Vérifier si les prets sélectionnées ne sont pas null
                    if (selectedLoanIds != null)
                    {
                        // Récupérer les prêts existants sélectionnés pour rachats
                        foreach (var loanId in selectedLoanIds)
                        {
                            RHSqlQuery pe = new RHSqlQuery();
                            SqlDataReader loanReader = pe.GetLoanById(loanId);

                            // Vérifier si loanReader n'est pas null
                            if (loanReader != null)
                            {
                                var loan = new Dictionary<string, object>();
                                while (loanReader.Read())
                                {
                                    for (var i = 0; i < loanReader.FieldCount; i++)
                                    {
                                        loan[loanReader.GetName(i)] = loanReader.GetValue(i);
                                    }

                                    // Vérifier si la clé "Mensualites" existe et si sa valeur n'est pas null
                                    if (loan.ContainsKey("Mensualites") && loan["Mensualites"] != null)
                                    {
                                        totalMonthlyPayments -= Convert.ToDecimal(loan["Mensualites"]);
                                    }
                                }
                                loanReader.Close(); // Fermer le reader après utilisation
                            }
                        }
                    }

                    decimal quotity = totalMonthlyPayments / netSalary * 100;

                    var customCulture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
                    customCulture.NumberFormat.NumberGroupSeparator = " ";

                    Logger.Info("Simulation faite pour le numéro de compte: ", Session["NumeroCompte"]);

                    return Json(new
                    {
                        monthlyPayment = monthlyPayment.ToString("N0", customCulture),
                        quotity = quotity.ToString("F", System.Globalization.CultureInfo.InvariantCulture),
                    });
                }
                else
                {
                    monthlyPayment = (int)Math.Round(MontantEmprunte / months);
                    totalMonthlyPayments = monthlyPayment;

                    decimal quotity = totalMonthlyPayments / netSalary * 100;

                    var customCulture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
                    customCulture.NumberFormat.NumberGroupSeparator = " ";

                    Logger.Info("Simulation faite pour le numéro de compte: ", Session["NumeroCompte"]);

                    return Json(new
                    {
                        monthlyPayment = monthlyPayment.ToString("N0", customCulture),
                        quotity = quotity.ToString("F", System.Globalization.CultureInfo.InvariantCulture)
                    });
                }
            }
            catch (Exception ex)
            {
                var errorObject = new { error = "Une erreur est survenue lors du calcul des mensualités. Veuillez réessayer." };
                Logger.Error(ex, "Erreur dans le calcul des mensualités");
                return Json(errorObject, JsonRequestBehavior.AllowGet);
            }
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult AjouterAutresPretsExistants(
            string TypePret, string NomBanque, string StartDate, string EndDate,
            string MontantStr, string MensualitesStr, string EncoursStr, string NumeroCompte
            )
        {
            // Convertir les chaînes de caractères en Décimal 
            decimal Montant = Convert.ToDecimal(MontantStr);
            decimal Mensualites = Convert.ToDecimal(MensualitesStr);
            decimal Encours = Convert.ToDecimal(EncoursStr);

            // Conversion des dates dans le format souhaité
            DateTime parsedStartDate = DateTime.Parse(StartDate);
            DateTime parsedEndDate = DateTime.Parse(EndDate);
            try
            {
                _sqlQuery.InsertAutresPretsExistants(TypePret, NomBanque, parsedStartDate, parsedEndDate, Montant, Mensualites, Encours, NumeroCompte);

                //Logger.
                Logger.Info("Pret ajouté pour le numero de compte : ", Session["NumeroCompte"]);
                return Json(new { success = true, message = "Pret ajouté avec succès.", redirectUrl = Url.Action("Create", "RHDemandeDePret") });
            }
            catch (Exception ex) 
            {
                Logger.Error(ex, "Error in SendSimulation");
                return Json(new { success = false, message = "Une erreur est survenue lors de l'ajout du prêt. Veuillez réessayer." });
            }
        }

        [System.Web.Mvc.HttpPost]
        public JsonResult SupprimerAutresPretsExistants(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "ID non fourni ou invalide." });
            }

            if (!int.TryParse(id, out int parsedid))
            {
                return Json(new { success = false, message = "L'ID fourni n'est pas valide." });
            }

            try
            {
                bool isDeleted = _sqlQuery.SupprimerPretExistant(parsedid);
                if (isDeleted)
                {
                    return Json(new { success = true, message = "Prêt supprimé avec succès." });
                }
                else
                {
                    return Json(new { success = false, message = "Échec de la suppression du prêt." });
                }
            }
            catch (Exception ex)
            {
                // Log de l'erreur si nécessaire
                Logger.Error(ex, "Erreur lors de la suppression des prêts autres banques");
                return Json(new { success = false, message = "Une erreur est survenue lors de la suppression du prêt." });
            }
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult SendSimulation(decimal Montant, string TypeDePret, decimal annualRate, int months, decimal quotity, decimal netSalary, int matricule)
        {
            if (Montant <= 0) return Json(new { success = false, message = "Le montant doit être supérieur à zéro." });
            //if (annualRate <= 0) return Json(new { success = false, message = "Le taux annuel doit être supérieur à zéro." });
            if (months <= 0) return Json(new { success = false, message = "La durée doit être supérieure à zéro mois." });
            if (netSalary <= 0) return Json(new { success = false, message = "Le salaire net doit être supérieur à zéro." });

            try
            {
                // Enregistrement des données dans la base de données
                _sqlQuery.SendSimulation(Montant, TypeDePret, annualRate, months, netSalary, matricule);

                decimal remboursementAdmis = netSalary * quotiteCessible / 100;

                // Stocker les données dans une session pour les passer à la prochaine action
                HttpContext.Session["SimulationData"] = new SimulationViewModel
                {
                    Montant = Montant,
                    TypeDePret = TypeDePret,
                    AnnualRate = annualRate,
                    Months = months,
                    Quotity = quotity,
                    NetSalary = netSalary,
                    Matricule = matricule,
                    Remboursement = remboursementAdmis
                };

                Logger.Info("Simulation envoyée pour le numero de compte : ", Session["NumeroCompte"]);
                return Json(new { success = true, message = "La simulation a été envoyée avec succès.", redirectUrl = Url.Action("Create", "RHDemandeDePret") });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in SendSimulation");
                return Json(new { success = false, message = "Une erreur est survenue lors de l'envoi de la demande. Veuillez réessayer." });
            }
        }

        public ActionResult Create()
        {
            var pretss = new List<Dictionary<string, object>>();
            string loginStaff = (string)HttpContext.Session["accountName"];
            var simulationData = HttpContext.Session["SimulationData"] as SimulationViewModel;

            RHSqlQuery st = new RHSqlQuery();
            SqlDataReader staffreadere = st.AccountLogin(loginStaff);

            var staff = new Dictionary<string, object>();

            while (staffreadere.Read())
            {
                for (int i = 0; i < staffreadere.FieldCount; i++)
                {
                    staff[staffreadere.GetName(i)] = staffreadere.GetValue(i);
                }
            }

            string NumeroCompte = Convert.ToString(staff["NumeroComptee"]);

            RHSqlQuery p = new RHSqlQuery();
            SqlDataReader prets = p.PretExistantsStaff(NumeroCompte);

            //Calculer le total des mensualités
            while (prets.Read())
            {
                var mensualites = prets.GetOrdinal(("Mensualites"));

                var Pret = new Dictionary<string, object>();

                for (int i = 0; i < prets.FieldCount; i++)
                {
                    Pret[prets.GetName(i)] = prets.GetValue(i);
                }

                pretss.Add(Pret);
            }

            if (simulationData == null)
            {
                return RedirectToAction("SendSimulation");
            }

            var staffInfo = new Dictionary<string, object>
            {
                { "Prets", pretss },
                { "Staff", staff },
                { "SimulationData", simulationData },
            };

            return View(staffInfo);
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult GenerateDocument(
             decimal Montant, string nom,
             string TypeDePret, string numeroCompte,
             decimal annualRate, string dateNaissance,
             int months, string dateDebut,
             decimal quotity, string ranking,
             decimal netSalary, string grade,
             int matricule, string notes, List<Dictionary<string, string>> prets)
        {

            // Récupérer les prêts autres banques 
            List<Dictionary<string, object>> existingLoans = _sqlQuery.GetExistingLoans(numeroCompte);
            string nomDocument = DateTime.UtcNow.ToString("-dd-MM-yyyy HH:mm", CultureInfo.CreateSpecificCulture("fr-FR"));
            
        // Initialisation des sommes
        decimal sommeEncours = 0;
            decimal sommeMensualites = 0;

            // Calculer la somme des encours et mensualités pour les prêts courants s'il y en a
            if (prets != null && prets.Count > 0)
            {
                sommeEncours = prets
                    .Where(pret => pret.ContainsKey("EnCours") && pret["EnCours"] != null)
                    .Sum(pret => Convert.ToDecimal(pret["EnCours"]));

                sommeMensualites = prets
                    .Where(pret => pret.ContainsKey("Mensualites") && pret["Mensualites"] != null)
                    .Sum(pret => Convert.ToDecimal(pret["Mensualites"]));
            }

            // Ajouter la somme des encours et mensualités pour les prêts existants dans d'autres banques
            if (existingLoans != null && existingLoans.Count > 0)
            {
                sommeEncours += existingLoans
                    .Where(pretsExistants => pretsExistants.ContainsKey("EnCours") && pretsExistants["EnCours"] != null)
                    .Sum(pretsExistants => Convert.ToDecimal(pretsExistants["EnCours"]));

                sommeMensualites += existingLoans
                    .Where(pretsExistants => pretsExistants.ContainsKey("Mensualites") && pretsExistants["Mensualites"] != null)
                    .Sum(pretsExistants => Convert.ToDecimal(pretsExistants["Mensualites"]));
            }

            // Vérification si la liste des prêts est null ou vide
            List<Dictionary<string, string>> filteredPrets = (prets != null && prets.Any())
                ? prets.Select(pret => new Dictionary<string, string>
                {
                    { "ReferencePret", pret.ContainsKey("ReferencePret") ? pret["ReferencePret"] : string.Empty },
                    { "StartDate", pret.ContainsKey("StartDate") ? pret["StartDate"].ToString().Split('T')[0] : string.Empty },
                    { "Montant", pret.ContainsKey("Montant") ? pret["Montant"] : "0" },
                    { "EnCours", pret.ContainsKey("EnCours") ? pret["EnCours"] : "0" },
                    { "Mensualites", pret.ContainsKey("Mensualites") ? pret["Mensualites"] : "0" },
                    { "EndDate", pret.ContainsKey("EndDate") ? pret["EndDate"].ToString().Split('T')[0] : string.Empty }
                }).ToList()
                : new List<Dictionary<string, string>>(); // Initialisation à une liste vide si prets est null ou vide

            // Conversion des éléments de prêts en chaînes de caractères 
            List<List<string>> stringListPretEco = filteredPrets
                .Select(dict => dict.Values.Select(value => value.ToString()).ToList())
                .ToList();

            // Conversion des éléments de prêts existants (autres banques) en chaînes de caractères 
            List<List<string>> stringListAutresPrets = existingLoans != null && existingLoans.Any()
                ? existingLoans.Select(dict => dict.Values.Select(value => value.ToString()).ToList()).ToList()
                : new List<List<string>>(); // Initialisation à une liste vide si existingLoans est null ou vide

            // Décodage des valeurs URL encodées
            nom = HttpUtility.UrlDecode(nom);
            TypeDePret = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(TypeDePret));
            numeroCompte = HttpUtility.UrlDecode(numeroCompte);
            dateNaissance = HttpUtility.UrlDecode(dateNaissance);
            dateDebut = HttpUtility.UrlDecode(dateDebut);
            notes = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(notes));
            //prets = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(prets));

            // Conversion des dates dans le format souhaité
            DateTime parsedDateNaissance = DateTime.Parse(dateNaissance);
            DateTime parsedDateDebut = DateTime.Parse(dateDebut);
            DateTime date = DateTime.UtcNow;

            string formattedDateNaissance = parsedDateNaissance.ToString("dd MMMM yyyy", CultureInfo.CreateSpecificCulture("fr-FR"));
            string formattedDateDebut = parsedDateDebut.ToString("dd MMMM yyyy", CultureInfo.CreateSpecificCulture("fr-FR"));
            string formattedDate = date.ToString("dd MMMM yyyy", CultureInfo.CreateSpecificCulture("fr-FR")); 

            if (string.IsNullOrEmpty(nom) || string.IsNullOrEmpty(numeroCompte) || string.IsNullOrEmpty(dateNaissance))
            {
                return Json(new { success = false, message = "Les champs obligatoires sont manquants." });
            }


            // Compléter les informations de la demande
            _sqlQuery.UpdateSimulation(matricule, nom, numeroCompte, parsedDateNaissance);

            try
            {
                var remboursementAdmis = netSalary * quotiteCessible / 100;

                var fieldValues = new Dictionary<string, string>
                {
                    { "Nom", nom },
                    { "DateNaissance", formattedDateNaissance },
                    { "DateEntree", formattedDateDebut },
                    { "Date", formattedDate },
                    { "Classe", ranking },
                    { "Grade", grade },
                    { "Matricule", matricule.ToString("N0") },
                    { "Montant", Montant.ToString("N0") },
                    { "TypeDePret", TypeDePret },
                    { "Months", months.ToString("N0") },
                    { "Taux", annualRate.ToString("N0") },
                    { "SalaireNet", netSalary.ToString("N0") },
                    { "Remboursement", remboursementAdmis.ToString("N0") },
                    { "NumeroCompte", numeroCompte },
                    { "TotalEnCours", sommeEncours.ToString("N0") },
                    { "TotalMensualites", sommeMensualites.ToString("N0") },
                    { "Notes", notes }
                };

                //string LoanFormtemplatePath = Server.MapPath("~/Content/Files/LoanTemplate.docx"); // Template Fiche de demande de prêt
                //string ConsentementtemplatePath = Server.MapPath("~/Content/Files/Consentement.docx"); // Template Formulaire Consentement
                //string PrimeAssurancetemplatePath = Server.MapPath("~/Content/Files/Prime_assurance.docx"); // Template Prime d'assurance
                //string ScolarLoanFormtemplatePath = Server.MapPath("~/Content/Files/Credit_scolarisation.docx"); // Template Formulaire Crédit de scolarisation

                string LoanFormtemplatePath = @"\\10.8.14.65\SmartLoanList\Content\Files\LoanTemplate.docx";
                string ConsentementtemplatePath = @"\\10.8.14.65\SmartLoanList\Content\Files\Consentement.docx";
                string PrimeAssurancetemplatePath = @"\\10.8.14.65\SmartLoanList\Content\Files\Prime_assurance.docx";
                string ScolarLoanFormtemplatePath = @"\\10.8.14.65\SmartLoanList\Content\Files\Credit_scolarisation.docx";

                List<byte[]> documentBytesList = new List<byte[]>();
                if (!System.IO.File.Exists(LoanFormtemplatePath))
                {
                    return Json(new { success = false, message = "Le modèle de document est introuvable." });
                } 

                if (TypeDePret == "Prêt scolaire")
                {
                    // génération du fichier de demande pret
                    var documentTableService = new WordTableDocumentService();
                    var LoanFormDocumentBytes = documentTableService.GenerateDocument(LoanFormtemplatePath, fieldValues, stringListPretEco, stringListAutresPrets);
                    // Ajout de fichier de demaned prêt à la liste des documents
                    documentBytesList.Add(LoanFormDocumentBytes);

                    // génération du fichier de consentement
                    var ConsentementdocumentService = new WordDocumentService();
                    var ConsentementdocumentBytes = ConsentementdocumentService.GenerateDocument(ConsentementtemplatePath, fieldValues);
                    // Ajout de fichier de demaned prêt à la liste des documents
                    documentBytesList.Add(ConsentementdocumentBytes);

                    // génération du fichier de Prime d'assurance
                    var PrimeAssurancedocumentService = new WordDocumentService();
                    var PrimeAssurancedocumentBytes = PrimeAssurancedocumentService.GenerateDocument(PrimeAssurancetemplatePath, fieldValues);
                    // Ajout de fichier de demaned prêt à la liste des documents
                    documentBytesList.Add(PrimeAssurancedocumentBytes);

                    // génération du fichier de Credit scolarisation
                    var ScolarLoandocumentService = new WordDocumentService();
                    var ScolarLoandocumentBytes = ScolarLoandocumentService.GenerateDocument(ScolarLoanFormtemplatePath, fieldValues);
                    // Ajout de fichier de demaned prêt à la liste des documents
                    documentBytesList.Add(ScolarLoandocumentBytes);

                    // Création d'un fichier ZIP
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                        {
                            for (int i = 0; i < documentBytesList.Count; i++)
                            {
                                var zipEntry = zip.CreateEntry($"Document_{i + 1}.docx", CompressionLevel.Fastest);
                                using (var zipStream = zipEntry.Open())
                                {
                                    zipStream.Write(documentBytesList[i], 0, documentBytesList[i].Length);
                                }
                            }
                        }

                        return File(memoryStream.ToArray(), "application/zip", $"DocumentsPrets{nomDocument}.zip");
                    }

                    //return File(documentBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "FICHE_DEMANDE_PRET_Filled.docx");
                }
                else
                {
                    // génération du fichier de demande pret
                    var documentTableService = new WordTableDocumentService();
                    var LoanFormDocumentBytes = documentTableService.GenerateDocument(LoanFormtemplatePath, fieldValues, stringListPretEco, stringListAutresPrets);
                    // Ajout de fichier de demaned prêt à la liste des documents
                    documentBytesList.Add(LoanFormDocumentBytes);

                    // génération du fichier de consentement
                    var ConsentementdocumentService = new WordDocumentService();
                    var ConsentementdocumentBytes = ConsentementdocumentService.GenerateDocument(ConsentementtemplatePath, fieldValues);
                    // Ajout de fichier de demaned prêt à la liste des documents
                    documentBytesList.Add(ConsentementdocumentBytes);

                    // génération du fichier de Prime d'assurance
                    var PrimeAssurancedocumentService = new WordDocumentService();
                    var PrimeAssurancedocumentBytes = PrimeAssurancedocumentService.GenerateDocument(PrimeAssurancetemplatePath, fieldValues);
                    // Ajout de fichier de demaned prêt à la liste des documents
                    documentBytesList.Add(PrimeAssurancedocumentBytes);

                    // Création d'un fichier ZIP
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                        {
                            for (int i = 0; i < documentBytesList.Count; i++)
                            {
                                var zipEntry = zip.CreateEntry($"Document_{i + 1}.docx", CompressionLevel.Fastest);
                                using (var zipStream = zipEntry.Open())
                                {
                                    zipStream.Write(documentBytesList[i], 0, documentBytesList[i].Length);
                                }
                            }
                        }

                        return File(memoryStream.ToArray(), "application/zip", $"DocumentsPrets{nomDocument}.zip");
                    }

                    //return File(documentBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "FICHE_DEMANDE_PRET_Filled.docx");
                }
            }
            catch (Exception ex)
            {
                // Log the exception 
                Logger.Error("Erreur lors de la génération du document : ", ex);
                return Json(new { success = false, message = "Erreur lors de la génération du document : " + ex.Message });
            }
        }

    }
}