using EcoService.Models;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NLog;
using NLog.Fluent;
//using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace EcoService.Controllers
{
    public class RHStaffController : Controller
    {
        // Logger NLog
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        // Instance unique de RHSqlQuery pour l'utilisation dans le contrôleur
        private readonly RHSqlQuery _sqlQuery = new RHSqlQuery();
        private int quotiteCessible = 50;

        // GET: RHStaff              
        public ActionResult Index()
        {
          //return RedirectToAction("StaffSimulation", "RHStaff");
           return View();
        }
       
        public ActionResult StaffSimulation()
        {

            string loginStaff = (string)HttpContext.Session["accountName"];
           
            RHSqlQuery st = new RHSqlQuery();
            decimal totalMensualites = 0;

            // Récupérer les prêts Ecobank
            var pretss = new List<Dictionary<string, object>>();
            SqlDataReader staffreadere = st.AccountLogin(loginStaff);
            
            // récupérer les informations du staff
            var staff = new Dictionary<string, object>();
            
            while (staffreadere.Read())
            {
                for (int i = 0; i < staffreadere.FieldCount; i++)
                {
                    staff[staffreadere.GetName(i)] = staffreadere.GetValue(i);
                }
            }

            string NumeroCompte = Convert.ToString(staff["NumeroComptee"]);
            // Récupérer les prêts autres banques 
            List<Dictionary<string, object>> existingLoans = _sqlQuery.GetExistingLoansWithID(NumeroCompte);

            RHSqlQuery p = new RHSqlQuery();
            SqlDataReader prets = p.PretExistantsStaff(NumeroCompte);

            // Calculer le total des mensualités et récupérer CreatedAt
            DateTime? createdAt = null;
            DateTime tempDate;

            while (prets.Read())
            {
                var mensualites = prets.GetOrdinal(("Mensualites"));
                var Pret = new Dictionary<string, object>();

                for (int i = 0; i < prets.FieldCount; i++)
                {
                    Pret[prets.GetName(i)] = prets.GetValue(i);
                }

                if (Pret["CreatedAt"] != null && DateTime.TryParse(Pret["CreatedAt"].ToString(), out tempDate))
                {
                    createdAt = tempDate;
                }

                pretss.Add(Pret);
                totalMensualites += Convert.ToDecimal(Pret["Mensualites"]);
            }
        
            //Récupérer les informations du staff
            decimal salaireNet = Convert.ToDecimal(staff["SalaireNete"]);
                    
            // Calculer la quotité consommée et résiduelle
            var quotiteConsommee = (totalMensualites / salaireNet) * 100;
            var quotiteResiduelle = quotiteCessible - quotiteConsommee;


            ViewBag.CreatedAt = createdAt;
            // Préparer les données pour la vue
            var simulation = new Dictionary<string, object>
            {
                { "Prets", pretss },
                { "AutresPrets", existingLoans },
                { "Staff", staff },
                { "QuotiteResiduelle", quotiteResiduelle.ToString("0.00") },
                { "QuotiteConsommee", quotiteConsommee.ToString("F", System.Globalization.CultureInfo.InvariantCulture) },
                { "CreatedAt", createdAt }
            };

            return View(simulation);
        }

        public ActionResult Parametre(int id)
        {
            RHSqlQuery lr = new RHSqlQuery();
            SqlDataReader DbReader = lr.Rapport(id);

            while (DbReader.Read())
            {
                Session["id"] = id;
                Session["Act"] = DbReader.GetString(DbReader.GetOrdinal("action"));
                Session["Cont"] = DbReader.GetString(DbReader.GetOrdinal("controller"));
                Session["Nom"] = DbReader.GetString(DbReader.GetOrdinal("nom"));
            }
            return Json(new { redirect = 1 });

        }
    }    
}