using EcoService.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace EcoService.Controllers
{
    public class RHDemandeDePretController : Controller
    {
        // Instance unique de RHSqlQuery pour l'utilisation dans le contrôleur
        private readonly RHSqlQuery _sqlQuery = new RHSqlQuery();

        // GET: RHDemandeDePret
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create()
        {
            var pretss = new List<Dictionary<string, object>>();
            string loginStaff = (string)HttpContext.Session["accountName"];

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

            var staffInfo = new Dictionary<string, object>
            {
                { "Prets", pretss },
                { "Staff", staff },
            };
            return View(staffInfo);
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
                // Log exception (ex) if necessary
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
                _sqlQuery.SendSimulation(Montant, TypeDePret, annualRate, months, quotity, netSalary, matricule);
                return Json(new { success = true, message = "La simulation a été envoyée avec succès.", redirectUrl = Url.Action("Create", "RHDemandeDePret") });
            }
            catch (Exception ex) 
            {
                // Log exception (ex) if necessary
                // Logger.LogError(ex, "Error in SendSimulation");
                return Json(new { success = false, message = "Une erreur est survenue lors de l'envoi de la demande. Veuillez réessayer." });
            }
        }

        //[HttpPost]
        //public ActionResult FillWordDocWSimulation()
        //{
        //    return ;
        //}
    }
}
