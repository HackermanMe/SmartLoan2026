using EcoService.Models;
using Microsoft.AspNetCore.Http;
using NLog;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Linq;
using System.Web.Mvc;
using System.Net;
using System.Web.Security;
using System.Web.UI.WebControls;

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
            // Vérifier si l'utilisateur a les droits pour voir cette page
            if (!UserHasRole(new List<int> { 100, 101 })) // RH et Contrôleur
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

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

        // GET: Create
        public ActionResult Create()
        {
            // Charger la liste des rôles pour le dropdown
            ViewBag.Roles = _sqlQuery.GetAllRoles();
            return View();
        }

        // Action pour traiter la création d'un utilisateur
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UserCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Créer l'utilisateur dans la table RHAccounts
                    int newUserId = _sqlQuery.CreateUser(model.Matricule, model.Nom, model.Prenom, model.Email, model.NumeroCompte, model.RoleId);
                    int proposedBy = GetCurrentUserId();

                    // Ajouter un enregistrement dans PendingRoleChanges pour la validation du rôle
                    _sqlQuery.AddPendingRoleChange(model.Matricule, proposedBy, model.RoleId);

                    TempData["SuccessMessage"] = "L'utilisateur a été créé avec succès. Le rôle est en attente de validation.";
                    return RedirectToAction("PendingRoleChanges");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Erreur lors de la création de l'utilisateur : " + ex.Message;
                }
            }

            // Recharger la liste des rôles en cas d'erreur
            ViewBag.Roles = _sqlQuery.GetAllRoles();
            return View(model);
        }


        // Action pour proposer le changement de rôle de l'utilisateur
        [HttpGet]
        public ActionResult ProposeChangeRole(int role, int matricule)
        {
            try
            {
                int proposedBy = GetCurrentUserId(); // Méthode pour obtenir l'ID de l'utilisateur actuel

                bool isProposed = _sqlQuery.ProposeUserRoleChange(matricule, role, proposedBy);

                if (isProposed)
                {
                    TempData["SuccessMessage"] = "La modification de rôle a été proposée. En attente de validation.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Échec de la proposition de changement de rôle.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la proposition de rôle : {ex.Message}";
                Logger.Error(ex, "Erreur lors de la proposition de changement de rôle.");
                return RedirectToAction("Index");
            }
        }

        // Action pour afficher les changements de rôle en attente
        public ActionResult PendingRoleChanges()
        {
            // Vérifier si l'utilisateur a les droits pour voir cette page
            if (!UserHasRole(new List<int> { 102, 101 })) // RH et Contrôleur
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            List<RoleChangePendingModel> pendingChanges = _sqlQuery.GetPendingRoleChanges();

            return View(pendingChanges);
        }

        // Action pour valider ou rejeter un changement de rôle
        [System.Web.Mvc.HttpPost]
        public ActionResult ValidateChangeRole(int changeId, bool isApproved)
        {
            try
            {
                bool isValidated = _sqlQuery.ValidateUserRoleChange(changeId, isApproved);

                if (isValidated)
                {
                    //bool isChange = _sqlQuery.UpdateUserRole(newRole, matricule);
                    TempData["SuccessMessage"] = isApproved ? "Le changement de rôle a été approuvé." : "Le changement de rôle a été rejeté.";
                    return RedirectToAction("PendingRoleChanges");
                }
                else
                {
                    TempData["ErrorMessage"] = "Échec de la validation du changement de rôle.";
                    return RedirectToAction("PendingRoleChanges");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la validation du rôle : {ex.Message}";
                Logger.Error(ex, "Erreur lors de la validation du changement de rôle.");
                return RedirectToAction("PendingRoleChanges");
            }
        }

        // Méthode auxiliaire pour vérifier les rôles de l'utilisateur actuel
        private bool UserHasRole(List<int> allowedRoles)
        {
            int userRole = Convert.ToInt32(Session["idGroup"]);
            return allowedRoles.Contains(userRole);
        }

        // Méthode auxiliaire pour obtenir l'ID de l'utilisateur actuel
        private int GetCurrentUserId()
        {
            string loginStaff = (string)HttpContext.Session["accountName"];
            return Convert.ToInt32(Session["IDUser"]); // Assurez-vous que "UserId" est stocké dans la session
        }
    }
}
