using EcoService.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EcoService.Controllers
{
    public class RHDemandeDePretController : Controller
    {
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

                    // Vérifier si selectedLoanIds n'est pas null
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
    }
}
