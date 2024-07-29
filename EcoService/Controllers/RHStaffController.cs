using EcoService.Models;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace EcoService.Controllers
{
    public class RHStaffController : Controller
    {
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
            var pretss = new List<Dictionary<string, object>>();

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

                totalMensualites += Convert.ToDecimal(Pret["Mensualites"]);

            }

        
            //Récupérer les informations du staff
            decimal salaireNet = Convert.ToDecimal(staff["SalaireNete"]);
                    
            // Calculer la quotité consommée et résiduelle
            var quotiteConsommee = (totalMensualites / salaireNet) * 100;
            var quotiteResiduelle = 40 - quotiteConsommee;

            // Préparer les données pour la vue
            var simulation = new Dictionary<string, object>
            {

                { "Prets", pretss },
                { "Staff", staff },
                { "QuotiteResiduelle", quotiteResiduelle.ToString("0.00") },
                { "QuotiteConsommee", quotiteConsommee.ToString("F", System.Globalization.CultureInfo.InvariantCulture) }
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