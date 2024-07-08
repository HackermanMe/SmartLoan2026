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

namespace Ecoservice.Controllers
{
    public class AccountController : Controller
    {
       // GET: Account
       //[AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Index(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.UserName, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
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
                    ModelState.AddModelError("", "The user name or password provided is incorrect");
                }
            }

            // if we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
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


                        string accountNamel = Session["accountName"]?.ToString() ?? "DEFAULT";

                        RHSqlQuery a = new RHSqlQuery();
                        SqlDataReader DbReader = a.AccountRole(accountNamel);

                        if (DbReader.Read())
                        {
                            // Vérification de la présence de la colonne IDGroup
                            if (DbReader.IsDBNull(DbReader.GetOrdinal("IDGroup")))
                            {
                                ModelState.AddModelError("", "IDGroup est null dans la base de données.");
                            }
                            else
                            {
                                int idgroup = DbReader.GetInt32(DbReader.GetOrdinal("IDGroup"));
                                Session["idGroup"] = idgroup;

                                FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);

                                if (idgroup == 100)
                                {
                                    return RedirectToAction("Index", "RHAdmin");
                                }
                                else
                                {
                                    return RedirectToAction("StaffSimulation", "RHStaff");
                                }
                            }
                        }
                        else
                        {
                            // Gérer le cas où aucune donnée n'est lue
                            ModelState.AddModelError("", "Erreur de rôle utilisateur.");
                        }

                    }
                    else
                    {
                        // user not found
                        Session["domainName"] = "";
                        Session["accountName"] = "";
                        Session["userFullName"] = "";
                    }                   

                    //return RedirectToAction("StaffSimulation", "RHStaff");
                }
                catch
                {
                    // If we got this far, something failed, redisplay form
                    ModelState.AddModelError("", "Le nom d'utilisateur ou le mot de passe fourni est incorrect.");
                    //return RedirectToAction("Index", "RHAdmin");
                }
                
            }
            
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Loginn(LoginViewModel model, string userName, string userPwd, string returnUrl)
        //public ActionResult Loginnn(LoginViewModel model, string returnUrl)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Utilisateur utilisateur = new Utilisateur { userName = "admin", userPwd = "admin" };
            
            if (model.UserName == utilisateur.userName && model.Password == utilisateur.userPwd)
            {

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
                    return RedirectToAction("Index", "RHAdmin");
                }

                // return RedirectToAction("Index", "RHAdmin");


                //return Redirect("/");
                //return  RedirectToAction("Liste", "Personne");
                // return Json(new { connexion = 1 });

            }
            ModelState.AddModelError("", "Le nom d'utilisateur ou le mot de passe fourni est incorrect.");
            return View(model);
            // return Redirect(returnUrl);


            //  return Json(new { connexion = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
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