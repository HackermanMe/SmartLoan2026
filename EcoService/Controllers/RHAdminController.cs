using EcoService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.DirectoryServices;
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


namespace EcoService.Controllers
{
    public class RHAdminController : Controller
    {
        // Instance unique de RHSqlQuery pour l'utilisation dans le contrôleur
        private readonly RHSqlQuery _sqlQuery = new RHSqlQuery();

        // GET: RHAdmin
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

        // Action pour rechercher le personnel
        [HttpPost]
        public ActionResult Search(string searchTerm)
        {
            List<Dictionary<string, object>> staffList = _sqlQuery.SearchStaff(searchTerm);
            return Json(staffList);
        }

        // GET: RHAdmin/Simulation
        public ActionResult Simulation(int id)
        {
            decimal totalMensualites = 0;
            List<Dictionary<string, object>> pretss = new List<Dictionary<string, object>>();
            SqlDataReader prets = _sqlQuery.PretExistants(id);
            SqlDataReader staffreader = _sqlQuery.Account(id);

            // Calculer le total des mensualités
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

            var staff = new Dictionary<string, object>();
            while (staffreader.Read())
            {
                for (int i = 0; i < staffreader.FieldCount; i++)
                {
                    staff[staffreader.GetName(i)] = staffreader.GetValue(i);
                }
            }

            // Récupérer les informations du staff
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
            SqlDataReader DbReader = _sqlQuery.Rapport(id);

            while (DbReader.Read())
            {
                Session["id"] = id;
                Session["Act"] = DbReader.GetString(DbReader.GetOrdinal("action"));
                Session["Cont"] = DbReader.GetString(DbReader.GetOrdinal("controller"));
                Session["Nom"] = DbReader.GetString(DbReader.GetOrdinal("nom"));
            }

            return Json(new { redirect = 1 });
        }

        // GET: RHAdmin/Create
        public ActionResult Create()
        {
            List<RHRole> RHroles = _sqlQuery.Role();
            ViewBag.roles = new SelectList(RHroles, "Iduser", "NumeroCompte");
            return View();
        }

        // POST: RHAdmin/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                var user = new User
                {
                    Nom = collection["Name"],
                    Prenom = collection["Prenom"],
                    Email = collection["Email"],
                    IdRole = Int32.Parse(collection["role"]),
                    Status = collection["isActive"] == "checked" ? "OUI" : "NON"
                };

                _sqlQuery.InsertUser(user);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Gestion des erreurs ici
                return View();
            }
        }

        // GET: RHAdmin/CreateAvis
        public ActionResult CreateAvis()
        {
            List<RHRole> RHroles = _sqlQuery.Role();
            ViewBag.roles = new SelectList(RHroles, "Matricule", "NumeroCpte");
            return View();
        }

        public ActionResult UploadStaff()
        {
            return View();
        }

        public ActionResult UploadLoans()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ImportStaffs(HttpPostedFileBase file)
        {
            // Implémentation de l'importation du personnel depuis un fichier Excel
            if (file == null || file.ContentLength <= 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Fichier invalide");

            var uploadsFolder = Server.MapPath("~/Uploads/Staffs");

            //var uploadsFolder = "//10.8.14.100/SmartLoanList/";

            //string uploadsFolder = WebConfigurationManager.AppSettings["RHExportPath"].ToString();
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Ajouter un horodatage au nom du fichier pour garantir l'unicité
            var uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                file.SaveAs(filePath);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, $"Erreur lors de l'écriture du fichier : {ex.Message}");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Fichier Excel vide");

                    var rowCount = worksheet.Dimension.Rows;

                    // Supprimer toutes les lignes existantes de la table Staffs
                    RHSqlQuery lr = new RHSqlQuery();
                    lr.DeleteRHStaff();

                    // Insérer les nouvelles lignes à partir du fichier Excel
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var matricule = worksheet.Cells[row, 1].Value?.ToString();
                        var email = worksheet.Cells[row, 2].Value?.ToString();
                        var salaireNet = worksheet.Cells[row, 4].Value?.ToString();
                        var numeroCompte = worksheet.Cells[row, 3].Value?.ToString();
                        RHSqlQuery RHStaffs = new RHSqlQuery();
                        RHStaffs.InsertStaffs(matricule, email, salaireNet, numeroCompte);
                    }
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, $"Erreur lors de la lecture du fichier Excel : {ex.Message}");
            }
            return RedirectToAction("Index", "RHAdmin");
        }

        [HttpPost]
        public ActionResult ImportLoans(HttpPostedFileBase file)
        {
            // Implémentation de l'importation des prêts depuis un fichier Excel
            if (file == null || file.ContentLength <= 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Fichier invalide");

            var uploadsFolder = Server.MapPath("~/App_Data/uploads/");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                file.SaveAs(filePath);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, $"Erreur lors de l'écriture du fichier : {ex.Message}");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Fichier Excel vide");

                    var rowCount = worksheet.Dimension.Rows;

                    // Supprimer toutes les lignes existantes de la table PretsExistans
                    RHSqlQuery lr = new RHSqlQuery();
                    lr.DeleteRHLoans();

                    // Insérer les nouvelles lignes à partir du fichier Excel
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var numeroCompte = worksheet.Cells[row, 1].Value?.ToString();
                        var reference = worksheet.Cells[row, 2].Value?.ToString();
                        var type = worksheet.Cells[row, 3].Value?.ToString();
                        var montantEmprunte = worksheet.Cells[row, 4].Value?.ToString();
                        var enCours = worksheet.Cells[row, 5].Value?.ToString();
                        var taux = worksheet.Cells[row, 6].Value?.ToString();
                        var mensualites = worksheet.Cells[row, 7].Value?.ToString();
                        var dateDebutStr = worksheet.Cells[row, 8].Value?.ToString();
                        var finPretStr = worksheet.Cells[row, 9].Value?.ToString();

                        var tauxFloat = float.Parse(taux);

                        // Convertir les dates au format dmy
                        DateTime dateDebut, finPret;

                        if (!DateTime.TryParseExact(dateDebutStr, "dd/MM/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out dateDebut))
                        {
                            throw new Exception($"Format de date invalide pour DateDebut à la ligne {row}: {dateDebutStr}");
                        }
                        if (!DateTime.TryParseExact(finPretStr, "dd/MM/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out finPret))
                        {
                            throw new Exception($"Format de date invalide pour FinPret à la ligne {row}: {finPretStr}");
                        }

                        RHSqlQuery Prets = new RHSqlQuery();
                        Prets.InsertLoans(numeroCompte, reference, type, montantEmprunte, enCours, tauxFloat, mensualites, dateDebut, finPret);
                    }
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, $"Erreur lors de la lecture du fichier Excel : {ex.Message}");
            }
            
            return RedirectToAction("Index", "RHAdmin");
        }
    }
}

