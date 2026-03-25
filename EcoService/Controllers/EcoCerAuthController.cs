using EcoService.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Web.Mvc;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Web.Security;

namespace EcoService.Controllers
{
    public class EcoCerAuthController : Controller
    {
        private readonly EcoCerDbUtility _db;
        private readonly EcoCerLogger _logger;

        public EcoCerAuthController(EcoCerLogger logger, EcoCerDbUtility db)
        {
            _db = db;
            _logger = logger;
        }
        public EcoCerAuthController()
        {
            // Constructeur sans paramètres
        }

        //public IActionResult Index()
        //{
        //    return (IActionResult)View();
        //}
        public System.Web.Mvc.ActionResult Index()
        {
            return View();
        }

        //public IActionResult Login()
        //{
        //    ClaimsPrincipal claimUser = (ClaimsPrincipal)HttpContext.User;

        //    /*if (claimUser.Identity.IsAuthenticated)
        //    {
        //        return RedirectToAction("Index", "Home");

        //    }*/

        //    return (IActionResult)View();
        //}

        public System.Web.Mvc.ActionResult Login()
        {
            ClaimsPrincipal claimUser = (ClaimsPrincipal)HttpContext.User;

            /*if (claimUser.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");

            }*/

            return View();
        }

        [System.Web.Mvc.HttpPost]
        public async Task<IActionResult> Loginn(EcoCerUserLoginModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = _db.GetUser(model.Login, model.Password);

                    if (user != null)
                    {
                        List<Claim> claims = new List<Claim>()
                    {
                        new Claim(ClaimTypes.Name, user.Login.ToString()),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    };

                        ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        AuthenticationProperties properties = new AuthenticationProperties()
                        {
                            AllowRefresh = true,
                            IsPersistent = false,
                        };
                        FormsAuthentication.SetAuthCookie(user.Login.ToString(), true);

                        //await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        //    new ClaimsPrincipal(claimsIdentity), properties);

                        return (IActionResult)RedirectToAction("Home", "EcoCerHome");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Nom d'utilisateur ou mot de passe incorrect");
                        return (IActionResult)View(model);
                    }
                }

                return (IActionResult)View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError("An unexpected error occured in user authentication login method:", ex);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return (IActionResult)RedirectToAction(nameof(Login));
            }

        }

        //public async Task<ActionResult> Login(EcoCerUserLoginModel model)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            var user = _db.GetUser(model.Login, model.Password);

        //            if (user == null)
        //            {
        //                ModelState.AddModelError(string.Empty, "Nom d'utilisateur ou mot de passe incorrect");
        //                return View(model);
        //            }

        //            var claims = new List<Claim>()
        //    {
        //        new Claim(ClaimTypes.Name, user.Login),
        //        new Claim(ClaimTypes.Role, user.Role),
        //        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        //    };

        //            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        //            var authProperties = new AuthenticationProperties
        //            {
        //                IsPersistent = false,
        //                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15)
        //            };

        //            await HttpContext.SignInAsync(
        //                CookieAuthenticationDefaults.AuthenticationScheme,
        //                new ClaimsPrincipal(claimsIdentity),
        //                authProperties);

        //            if (user.Role == "Admin")
        //            {
        //                //return View("/Views/EcoCerAdmin/LandingPage.cshtml");
        //                return RedirectToAction("ManageCertificates", "EcoCerAdmin");
        //            }
        //            else
        //            {
        //                return RedirectToAction("Home", "EcoCerHome");
        //            }
        //        }

        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("An unexpected error occured in user authentication login method:", ex);
        //        TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
        //        return RedirectToAction(nameof(Login));
        //    }

        //}

        public async Task<IActionResult> LogOut()
        {
            //await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            FormsAuthentication.SignOut();
            return (IActionResult)RedirectToAction(nameof(Login));
        }


    }
}