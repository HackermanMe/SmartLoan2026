using EcoService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Globalization;
using System.Web.UI.WebControls;
using NLog;
using System.Text;
using System.Web.Configuration;
using System.Data.Entity.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Web.Security;
namespace EcoService.Controllers
{
    public class RHAccountsController : Controller
    {
        // Logger NLog
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        // Instance unique de RHSqlQuery pour l'utilisation dans le contrôleur
        private readonly RHSqlQuery _sqlQuery = new RHSqlQuery();

        // GET: RHAccounts
        public ActionResult Index()
        {
            string accountName = Session["accountName"]?.ToString() ?? "DEFAULT";
            SqlDataReader DbReader = _sqlQuery.Accounts(accountName);
            var dataTable = new DataTable();

            if (DbReader.HasRows)
            {
                dataTable.Load(DbReader);
            }
            else
            {
                SqlDataReader DbReader2 = _sqlQuery.Accounts("DEFAULT");
                dataTable.Load(DbReader2);
            }

            return View(dataTable);
        }

        //Action pour changer le rôle de l'utilisateur
        [HttpGet]
        public ActionResult ChangeRole(int role, int matricule)
        {
            try
            {
                Task<bool> task = _sqlQuery.UpdateUserRole(matricule, role);

                if (task.Result)
                {
                    TempData["SuccessMessage"] = "Le rôle de l'utilisateur a été mis à jour avec succès.";
                    return RedirectToAction("Index"); // Redirection vers une autre vue si nécessaire
                }
                else
                {
                    TempData["ErrorMessage"] = "Échec de la mise à jour du rôle.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de l'attribution du rôle : {ex.Message}";
                return RedirectToAction("Index");
            }
        }

    }
}