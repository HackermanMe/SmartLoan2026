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
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.EMMA;
using System.Web.Security;


namespace EcoService.Controllers
{
    public class RHAdminController : Controller
    {
        // Logger NLog
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        // Instance unique de RHSqlQuery pour l'utilisation dans le contrôleur
        private readonly RHSqlQuery _sqlQuery = new RHSqlQuery();
        public int quotiteCessible = 50;
        // GET: RHAdmin
        public ActionResult Index()
        {
            string accountName = Session["accountName"]?.ToString() ?? "DEFAULT";
            DataTable dataTable = new DataTable();

            try
            {
                // --- Lecture des informations du compte ---
                using (SqlDataReader DbReader = _sqlQuery.Accounts(accountName))
                {
                    if (DbReader.HasRows)
                        dataTable.Load(DbReader);
                    else
                    {
                        using (SqlDataReader DbReader2 = _sqlQuery.Accounts("DEFAULT"))
                        {
                            dataTable.Load(DbReader2);
                        }
                    }
                }

                // --- Recherche du rôle dans la base de données ---
                using (SqlDataReader DbReader1 = _sqlQuery.AccountRole(accountName))
                {
                    if (DbReader1.Read())
                    {
                        int ordinal = DbReader1.GetOrdinal("IDGroup");

                        if (DbReader1.IsDBNull(ordinal))
                        {
                            Logger.Warn("Groupe non attribué pour l'utilisateur {0}", accountName);
                            ModelState.AddModelError("", "Groupe non attribué.");
                        }
                        else
                        {
                            // Lecture adaptée au type SQL BIGINT
                            long idgroup = DbReader1.GetInt64(ordinal);
                            Session["idGroup"] = Convert.ToInt32(idgroup); // garde la compatibilité avec int
                            Logger.Info("Utilisateur {0} associé au groupe {1}", accountName, idgroup);
                        }
                    }
                    else
                    {
                        Logger.Error("Aucune donnée de rôle trouvée pour l'utilisateur {0}", accountName);
                        ModelState.AddModelError("", "Erreur de rôle utilisateur.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Erreur inattendue dans RHAdminController.Index pour l'utilisateur {0}", accountName);
                ModelState.AddModelError("", "Une erreur inattendue est survenue. Veuillez réessayer.");
            }

            // --- Retour de la vue ---
            return View(dataTable);
        }

        //public ActionResult Index()
        //{
        //    string accountName = Session["accountName"]?.ToString() ?? "DEFAULT";
        //    SqlDataReader DbReader = _sqlQuery.Accounts(accountName);

        //    // Rechercher le rôle dans la base de données 
        //    RHSqlQuery a = new RHSqlQuery();
        //    SqlDataReader DbReader1 = a.AccountRole(accountName);

        //    // Initialisation de la variable du groupe de l'utilisateur
        //    if (DbReader1.Read())
        //    {
        //        // Vérification de la présence de la colonne IDGroup
        //        if (DbReader1.IsDBNull(DbReader1.GetOrdinal("IDGroup")))
        //        {
        //            Logger.Warn("Groupe non attribué pour l'utilisateur {0}", accountName);
        //            ModelState.AddModelError("", "Groupe non attribué.");
        //        }
        //        else
        //        {
        //            int idgroup = DbReader1.GetInt32(DbReader1.GetOrdinal("IDGroup"));
        //            Session["idGroup"] = idgroup;                   
        //        }
        //    }
        //    else
        //    {
        //        Logger.Error("Erreur de rôle utilisateur pour l'utilisateur {0}", accountName);
        //        // Gérer le cas où aucune donnée n'est lue
        //        ModelState.AddModelError("", "Erreur de rôle utilisateur.");
        //    }

        //    // Affichage des comptes du personnel à la vue par le dataTable
        //    var dataTable = new DataTable();

        //    if (DbReader.HasRows)
        //    {
        //        dataTable.Load(DbReader);
        //    }
        //    else
        //    {
        //        SqlDataReader DbReader2 = _sqlQuery.Accounts("DEFAULT");
        //        dataTable.Load(DbReader2);
        //    }

        //    return View(dataTable);
        //}

        public ActionResult AccountsAdmin()
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

        public ActionResult ListDemandes()
        {
            List<RHDemande> listDemandes = _sqlQuery.GetDemandesPret();
            return View(listDemandes);
        }

        // Action pour télécharger le rapport des demandes en fichier Excel
        public ActionResult ExportToExcel()
        {
            List<RHDemande> loanRequests = _sqlQuery.GetDemandesPret();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Rapport des Prêts");

                // En-têtes des colonnes
                worksheet.Cells[1, 1].Value = "Nom & Prénoms";
                worksheet.Cells[1, 2].Value = "Date de naissance";
                worksheet.Cells[1, 3].Value = "Numéro de compte";
                worksheet.Cells[1, 4].Value = "Montant Crédit (Chiffre)";
                worksheet.Cells[1, 5].Value = "Montant Mensualité (Chiffre)";
                worksheet.Cells[1, 6].Value = "Nombre d'échéance (Chiffre)";
                worksheet.Cells[1, 7].Value = "Date début de remboursement";
                worksheet.Cells[1, 8].Value = "Taux";
                worksheet.Cells[1, 9].Value = "Type de crédit";

                // Remplir les lignes avec les données des demandes
                int row = 2;
                foreach (var request in loanRequests)
                {
                    worksheet.Cells[row, 1].Value = request.NomComplet; // Tu devrais récupérer le nom et prénom depuis une autre table liée
                    worksheet.Cells[row, 2].Value = request.DateNaissance; // Valeur brute de la date
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "dd/MM/yyyy"; // Formater la cellule comme une date lisible
                    worksheet.Cells[row, 3].Value = request.NumeroCompte;
                    worksheet.Cells[row, 4].Value = request.Montant;
                    worksheet.Cells[row, 5].Value = request.Montant / request.NbreEcheances; // Calcul de la mensualité
                    worksheet.Cells[row, 6].Value = request.NbreEcheances;
                    worksheet.Cells[row, 7].Value = request.CreatedAt; // Date de création
                    worksheet.Cells[row, 8].Value = request.Taux;
                    worksheet.Cells[row, 9].Value = request.TypePret;

                    row++;
                }

                // Générer le fichier Excel
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string fileName = $"RapportPrets{DateTime.Now:yyyyMMddHHmmssfff}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        //Action pour changer le rôle de l'utilisateur
        [System.Web.Mvc.HttpPost]
        public ActionResult ChangeRole(int role, int matricule)
        {
            try
            {
                bool isModified = _sqlQuery.UpdateUserRole(matricule, role);
                if (isModified)
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

        // Action pour rechercher les comptes
        [System.Web.Mvc.HttpPost]
        public ActionResult SearchAccounts(string searchTerm)
        {
            List<Dictionary<string, object>> accountsList = _sqlQuery.SearchAccounts(searchTerm);
            return Json(accountsList);
        }

        // Action pour rechercher le personnel
        [System.Web.Mvc.HttpPost]
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
            var quotiteResiduelle = quotiteCessible - quotiteConsommee;

            ViewBag.CreatedAt = createdAt;
            // Récupérer les prêts autres banques
            string NumeroCompte = Convert.ToString(staff["NumeroComptee"]);
            List<Dictionary<string, object>> existingLoans = _sqlQuery.GetExistingLoansWithID(NumeroCompte);
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
                        
        public ActionResult UploadStaff()
        {
            return View();
        }

        public ActionResult UploadLoans()
        {
            return View();
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult ImportStaffs(HttpPostedFileBase file)
        {
            // Implémentation de l'importation du personnel depuis un fichier Excel
            if (file == null || file.ContentLength <= 0)
            {
                TempData["ErrorMessage"] = "Fichier invalide ou vide.";
                return RedirectToAction("UploadStaff");
            }

            //var uploadsFolder = Server.MapPath("~/Uploads/Staffs");

            var uploadsFolder = "//10.8.14.65/SmartLoanList/";


            //string uploadsFolder = WebConfigurationManager.AppSettings["RHExportPath"].ToString();

            // Vérification de l'existence du répertoire de téléchargement
            if (!Directory.Exists(uploadsFolder))
            {
                try
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erreur lors de la création du répertoire de téléchargement : {ex.Message}";
                    return RedirectToAction("UploadStaff");
                }
            }

            // Ajouter un horodatage au nom du fichier pour garantir l'unicité
            var uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Enrégistrement du fichier
            try
            {
                file.SaveAs(filePath);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de l'écriture du fichier : {ex.Message}";
                return RedirectToAction("UploadStaff");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        TempData["ErrorMessage"] = "Le fichier Excel est vide ou ne contient pas de feuille de calcul valide.";
                        return RedirectToAction("UploadStaffs");
                    }

                    var rowCount = worksheet.Dimension.Rows;

                    // Supprimer toutes les lignes existantes de la table Staffs
                    try
                    {
                        RHSqlQuery lr = new RHSqlQuery();
                        lr.DeleteRHStaff();
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Impossible de vider la table des Staffs de la base de données. Erreur: : {ex.Message}";
                        return RedirectToAction("UploadStaff");
                    }

                    // Insérer les nouvelles lignes à partir du fichier Excel
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var matricule = worksheet.Cells[row, 1].Value?.ToString();
                            var email = worksheet.Cells[row, 2].Value?.ToString();
                            var salaireNet = worksheet.Cells[row, 4].Value?.ToString();
                            var numeroCompte = worksheet.Cells[row, 3].Value?.ToString();
                            RHSqlQuery RHStaffs = new RHSqlQuery();
                            RHStaffs.InsertStaffs(matricule, email, salaireNet, numeroCompte);
                        }
                        catch (Exception ex)
                        {
                            TempData["ErrorMessage"] = $"Erreur lors de l'insertion des données dans la base de données : {ex.Message}";
                            return RedirectToAction("UploadStaff");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la lecture du fichier Excel : {ex.Message}";
                return RedirectToAction("UploadStaff");
            }
            TempData["SuccessMessage"] = "Importation des informations des staffs réussie.";
            return RedirectToAction("UploadStaff");
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult ImportLoans(HttpPostedFileBase file)
        {
            // Vérification de la validité du fichier
            if (file == null || file.ContentLength <= 0)
            {
                TempData["ErrorMessage"] = "Fichier invalide ou vide.";
                return RedirectToAction("UploadLoans");
            }

            //var uploadsFolder = Server.MapPath("~/App_Data/uploads/");
            var uploadsFolder = "//10.8.14.65/SmartLoanList/";

            // Vérification de l'existence du répertoire de téléchargement
            if (!Directory.Exists(uploadsFolder))
            {
                try
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erreur lors de la création du répertoire de téléchargement : {ex.Message}";
                    return RedirectToAction("UploadLoans");
                }
            }

            var uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                file.SaveAs(filePath);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de l'écriture du fichier : {ex.Message}";
                return RedirectToAction("UploadLoans");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        TempData["ErrorMessage"] = "Le fichier Excel est vide ou ne contient pas de feuille de calcul valide.";
                        return RedirectToAction("UploadLoans");
                    }

                    var rowCount = worksheet.Dimension.Rows;

                    // Supprimer toutes les lignes existantes de la table PretsExistants
                    try
                    {
                        RHSqlQuery lr = new RHSqlQuery();
                        lr.DeleteRHLoans();
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Impossible de vider la table des prêts de la base de données. Erreur: : {ex.Message}";
                        return RedirectToAction("UploadLoans");
                    }

                    // Insérer les nouvelles lignes à partir du fichier Excel
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
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

                            if (!float.TryParse(taux, out float tauxFloat))
                            {
                                throw new Exception($"Valeur de taux invalide à la ligne {row} : {taux}");
                            }

                            // Convertir les montants en décimal
                            if (!decimal.TryParse(montantEmprunte, out decimal montantEmprunteDecimal))
                            {
                                throw new Exception($"Valeur de montant emprunté invalide à la ligne {row} : {montantEmprunte}");
                            }

                            if (!decimal.TryParse(enCours, out decimal enCoursDecimal))
                            {
                                throw new Exception($"Valeur en cours invalide à la ligne {row} : {enCours}");
                            }

                            if (!decimal.TryParse(mensualites, out decimal mensualitesDecimal))
                            {
                                throw new Exception($"Valeur de mensualités invalide à la ligne {row} : {mensualites}");
                            }

                            // Convertir les dates au format date
                            if (!DateTime.TryParse(dateDebutStr, out DateTime dateDebut))
                            {
                                throw new Exception($"Format de date invalide pour DateDebut à la ligne {row} : {dateDebutStr}");
                            }
                            if (!DateTime.TryParse(finPretStr, out DateTime finPret))
                            {
                                throw new Exception($"Format de date invalide pour FinPret à la ligne {row} : {finPretStr}");
                            }

                            RHSqlQuery Prets = new RHSqlQuery();
                            Prets.InsertLoans(numeroCompte, reference, type, montantEmprunteDecimal, enCoursDecimal, tauxFloat, mensualitesDecimal, dateDebut, finPret);
                        }
                        catch (Exception ex)
                        {
                            TempData["ErrorMessage"] = $"Erreur lors de l'insertion des données dans la base de données : {ex.Message}";
                            return RedirectToAction("UploadLoans");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la lecture du fichier Excel : {ex.Message}";
                return RedirectToAction("UploadLoans");
            }

            TempData["SuccessMessage"] = "Importation des prêts réussie.";
            return RedirectToAction("UploadLoans");
        }

        public ActionResult DownloadLoansTemplate(string fileName)
        {
            // Chemin du fichier sur le serveur
            string filePath = "//10.8.14.65/SmartLoanList/Content/Files/"+fileName;

         
            // Définir le type de contenu en fonction de l'extension du fichier
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            // Vérifier si le fichier existe
            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound("Fichier non trouvé");
            }

            return File(filePath, contentType, fileName);
        }
    }
}

