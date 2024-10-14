using Ecoservice.Models;
using EcoService.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using System.Data;
using System.Data.SqlClient;
using System.DirectoryServices;
using System.Drawing;
using System.Linq;
using System.Security;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using NLog;

namespace Ecoservice.Controllers
{
    public class AccountController : Controller
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // GET: Account
        //[AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            Logger.Info("Accès à la page de connexion. URL de retour : {0}", returnUrl);
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Index(LoginViewModel model, string returnUrl)
        {
            Logger.Info("Tentative de connexion pour l'utilisateur {0}", model.UserName);

            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.UserName, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                    Logger.Info("Connexion réussie pour l'utilisateur {0}", model.UserName);
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("//\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    Logger.Warn("Échec de la connexion pour l'utilisateur {0}. Mot de passe ou nom d'utilisateur incorrect.", model.UserName);
                    ModelState.AddModelError("", "The user name or password provided is incorrect");
                }
            }
            else
            {
                Logger.Warn("Échec de la validation du modèle pour l'utilisateur {0}", model.UserName);
            }

            // if we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            Logger.Info("Tentative de connexion via AD pour l'utilisateur {0}", model.UserName);

            if (ModelState.IsValid)
            {
                try
                {
                    //Check user credentials
                    //  ActiveDirectory adVerifyUser = new ActiveDirectory(serverName, model.UserName, securePwd);
                    var domain = "ECOBANK.GROUP";

                    //var domain = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\').First();
                    DirectoryEntry entry = new DirectoryEntry($"LDAP://{domain}", model.UserName, model.Password, AuthenticationTypes.Secure);
                    //DirectoryEntry entry = new DirectoryEntry(LDAP://ECOBANKGROUP", model.UserName, model.Password, AuthenticationTypes.Secure);
                    DirectorySearcher searcher = new DirectorySearcher(entry);

                    //var accountName = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\').Last();
                    var accountName = model.UserName;
                    searcher.Filter = $"(sAMAccountName={accountName})";
                    searcher.PropertiesToLoad.Add("displayName");
                    var searchResult = searcher.FindOne();

                    if (searchResult != null && searchResult.Properties.Contains("displayName"))
                    {
                        var displayName = searchResult.Properties["displayName"][0];

                        Session["domainName"] = domain;
                        Session["accountName"] = accountName;
                        Session["userFullName"] = displayName;

                        Logger.Info("Utilisateur {0} connecté avec succès via AD. Nom complet : {1}", accountName, displayName);

                        string accountNamel = Session["accountName"]?.ToString() ?? "DEFAULT";

                        RHSqlQuery a = new RHSqlQuery();
                        SqlDataReader DbReader = a.AccountRole(accountNamel);

                        if (DbReader.Read())
                        {
                            // Vérification de la présence de la colonne IDGroup
                            if (DbReader.IsDBNull(DbReader.GetOrdinal("IDGroup")))
                            {
                                Logger.Warn("Groupe non attribué pour l'utilisateur {0}", accountName);
                                ModelState.AddModelError("", "Groupe non attribué.");
                            }
                            else
                            {
                                int idgroup = DbReader.GetInt32(DbReader.GetOrdinal("IDGroup"));
                                Session["idGroup"] = idgroup;

                                FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);

                                Logger.Info("Groupe {0} attribué à l'utilisateur {1}", idgroup, accountName);

                                switch (idgroup)
                                {
                                    case 2:
                                        return RedirectToAction("StaffSimulation", "RHStaff");
                                    case 100:
                                        return RedirectToAction("Index", "RHAdmin");
                                    case 101:
                                        return RedirectToAction("Index", "RHAdmin");
                                    case 102:
                                        return RedirectToAction("PendingRoleChanges", "RHAccounts");
                                    default:
                                        ModelState.AddModelError("", "Vous n'avez pas accès à la plateforme.");
                                        break;
                                }
                               
                            }
                        }
                        else
                        {
                            Logger.Error("Erreur de rôle utilisateur pour l'utilisateur {0}", accountName);
                            // Gérer le cas où aucune donnée n'est lue
                            ModelState.AddModelError("", "Erreur de rôle utilisateur.");
                        }

                    }
                    else
                    {
                        Logger.Warn("Utilisateur {0} non trouvé dans l'annuaire AD", accountName);
                        // user not found
                        Session["domainName"] = "";
                        Session["accountName"] = "";
                        Session["userFullName"] = "";
                    }                   

                    //return RedirectToAction("StaffSimulation", "RHStaff");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Erreur lors de la tentative de connexion pour l'utilisateur {0}", model.UserName);
                    // If we got this far, something failed, redisplay form
                    ModelState.AddModelError("", "Le nom d'utilisateur ou le mot de passe fourni est incorrect.");
                    //return RedirectToAction("Index", "RHAdmin");
                }
                
            }
            else
            {
                Logger.Warn("Échec de la validation du modèle pour l'utilisateur {0}", model.UserName);
            }
            
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Loginn(LoginViewModel model, string userName, string userPwd, string returnUrl)
        //public ActionResult Loginnn(LoginViewModel model, string returnUrl)
        {
            Logger.Info("Tentative de connexion avec utilisateur local {0}", model.UserName);

            if (!ModelState.IsValid)
            {
                Logger.Warn("Échec de la validation du modèle pour l'utilisateur {0}", model.UserName);
                return View(model);
            }

            Utilisateur utilisateur = new Utilisateur { userName = "admin", userPwd = "admin" };
            
            if (model.UserName == utilisateur.userName && model.Password == utilisateur.userPwd)
            {

                Logger.Info("Connexion réussie pour l'utilisateur local {0}", model.UserName);

                var idgroup = 100;
                Session["idGroup"] = idgroup;

                //if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                //    return Redirect(returnUrl);
                FormsAuthentication.SetAuthCookie(model.UserName.ToString(), false);
                if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/"))
                {
                    return Redirect(returnUrl);
                }
                else
                {

                    Session["domainName"] = "";
                    Session["accountName"] = "admin";
                    Session["userFullName"] = model.UserName;
                    //return RedirectToAction("Index", "RHAdmin");
                    if (idgroup == 100 || idgroup == 101)
                    {
                        return RedirectToAction("Index", "RHAdmin");
                    }
                    else if (idgroup == 2)
                    {
                        return RedirectToAction("StaffSimulation", "RHStaff");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Vous n'avez pas accès à la plateforme.");
                        return View(model);
                    }
                }

                // return RedirectToAction("Index", "RHAdmin");

                //return Redirect("/");
                //return  RedirectToAction("Liste", "Personne");
                // return Json(new { connexion = 1 });

            }
            else
            {
                Logger.Warn("Échec de la connexion pour l'utilisateur local {0}", model.UserName);
                ModelState.AddModelError("", "Le nom d'utilisateur ou le mot de passe fourni est incorrect.");
                return View(model);
                // return Redirect(returnUrl);

                //  return Json(new { connexion = 0 });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Logger.Info("Déconnexion de l'utilisateur {0}", User.Identity.Name);
            FormsAuthentication.SignOut();
            return Redirect("/");
        }

    }

    internal class Utilisateur
    {
        internal string userName;
        internal string userPwd;
    }
}