using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using EcoService.Models;
using Microsoft.Extensions.Logging;
//using Microsoft.AspNetCore.Mvc;


namespace EcoService.Controllers
{
    public class EcoCerHomeController : Controller
    {
        private readonly ILogger<EcoCerHomeController> _logger;

        // Constructeur par défaut requis par ASP.NET MVC
        public EcoCerHomeController() { }

        public EcoCerHomeController(ILogger<EcoCerHomeController> logger)
        {
            _logger = logger;
        }

        public ActionResult Home()
        {
            return View();
        }
        //public IActionResult Home()
        //{
        //    return (IActionResult)View();
        //}
        /*public async Task<IActionResult> LogOut()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

			return RedirectToAction("Login", "EcoCerAuth");
		}*/

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult Error()
        {
            //return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            var requestId = Guid.NewGuid().ToString();  // Générer un ID unique manuellement
            return View(new ErrorViewModel { RequestId = requestId });

        }
    }
}