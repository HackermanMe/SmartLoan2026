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

namespace EcoService.Controllers
{
    public class RHDemandeDePretController : Controller
    {
        // Logger NLog
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        // Instance unique de RHSqlQuery pour l'utilisation dans le contrôleur
        private readonly RHSqlQuery _sqlQuery = new RHSqlQuery();

        // GET: RHDemandeDePret
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
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
                    customCulture.NumberFormat.NumberGroupSeparator = "  ";

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

        [HttpPost]
        public ActionResult SendSimulation(decimal Montant, string TypeDePret, decimal annualRate, int months, decimal quotity, decimal netSalary, int matricule)
        {
            if (Montant <= 0) return Json(new { success = false, message = "Le montant doit être supérieur à zéro." });
            if (annualRate <= 0) return Json(new { success = false, message = "Le taux annuel doit être supérieur à zéro." });
            if (months <= 0) return Json(new { success = false, message = "La durée doit être supérieure à zéro mois." });
            if (netSalary <= 0) return Json(new { success = false, message = "Le salaire net doit être supérieur à zéro." });

            try
            {
                // Enregistrement des données dans la base de données
                _sqlQuery.SendSimulation(Montant, TypeDePret, annualRate, months, quotity, netSalary, matricule);

                decimal remboursementAdmis = netSalary * 40 / 100;

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

                //Logger.
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

        [HttpPost]
        public ActionResult GenerateDocument(
             decimal Montant, string nom,
             string TypeDePret, string numeroCompte,
             decimal annualRate, string dateNaissance,
             int months, string dateDebut,
             decimal quotity, string ranking,
             decimal netSalary, string grade,
             int matricule, string notes)
        {
            if (string.IsNullOrEmpty(nom) || string.IsNullOrEmpty(numeroCompte) || string.IsNullOrEmpty(dateNaissance))
            {
                return Json(new { success = false, message = "Les champs obligatoires sont manquants." });
            }

            try
            {
                var remboursementAdmis = netSalary * 40 / 100;

                var fieldValues = new Dictionary<string, string>
                {
                    { "Nom", nom },
                    { "DateNaissance", dateNaissance },
                    { "DateEntree", dateDebut },
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
                    { "Notes", notes }
                };

                string templatePath = Server.MapPath("~/Templates/LoanTemplate.docx");
                if (!System.IO.File.Exists(templatePath))
                {
                    return Json(new { success = false, message = "Le modèle de document est introuvable." });
                }

                var documentService = new WordDocumentService();
                var documentBytes = documentService.GenerateDocument(templatePath, fieldValues);

                return File(documentBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "FICHE_DEMANDE_PRET_Filled.docx");
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
