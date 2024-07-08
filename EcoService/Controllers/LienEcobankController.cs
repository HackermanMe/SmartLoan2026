using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EcoService.Models;
using System.Data.Odbc;
using System.IO;
using System.Globalization;
using System.Web.UI.WebControls;
using NLog;
//using System.Data.OracleClient;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Text;
using NPOI.HPSF;
using System.Web.Configuration;

namespace EcoService.Controllers
{
    public class LienEcobankController : Controller
    {
        [HttpPost]
        public ActionResult Index()


        {       


            return PartialView();


        }
    }
}