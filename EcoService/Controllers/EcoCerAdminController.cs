using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
//using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using EcoService.Models;
using System.Threading.Tasks;
using static EcoService.Models.Enums;
using OfficeOpenXml;
using System.IO;
using System.Web.Http;
using System.ComponentModel.DataAnnotations;
using DocumentFormat.OpenXml.EMMA;


namespace EcoService.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class EcoCerAdminController : Controller
    {
        private readonly EcoCerDbUtility _db;
        private readonly EcoCerCertificateService _certificateService;
        private readonly EcoCerMailService _mailService;
        private readonly Tools _tools;
        private readonly EcoCerLogger _logger;

        public EcoCerAdminController()
        {
            _db = new EcoCerDbUtility();
            _certificateService = new EcoCerCertificateService();
            _mailService = new EcoCerMailService();
            _tools = new Tools();
            _logger = new EcoCerLogger();
        }

        public EcoCerAdminController(EcoCerDbUtility db, EcoCerCertificateService certificateService, EcoCerMailService mailService)
        {
            _db = db;
            _certificateService = certificateService;
            _mailService = mailService;
        }

        public class CertificateFormData : EcoCerCertificateFormModel
        {

            [Required(ErrorMessage = "Vous ne pouvez pas laisser ce champ vide")]
            public string UserLogin { get; set; }
        }
        // GET: EcoCerAdmin
        public ActionResult Index()
        {
            return View();
        }


        public ActionResult LandingPage()
        {
            return View();
        }

        public async Task<ActionResult> ManageCertificates()
        {
            var pendingCertificates = await _db.GetCertificatesByStatus("En attente", "");
            var validateCertificates = await _db.GetCertificatesByStatus("Valide", "");

            ViewBag.PendingCertificates = pendingCertificates;
            ViewBag.ValidateCertificates = validateCertificates;

            return View();
        }

        /*public async Task<ActionResult> ManageCertificates(int page = 1, int pageSize = 5)
        {
            var pendingCertificates = await _db.GetCertificatesByStatus("En attente", "");
            var validateCertificates = await _db.GetCertificatesByStatus("Valide", "");

            var paginatedPendingCertificates = pendingCertificates.Skip((page - 1) * pageSize)
                                                                  .Take(pageSize)
                                                                  .ToList();

            var paginatedValidateCertificates = validateCertificates.Skip((page - 1) * pageSize)
                                                      .Take(pageSize)
                                                      .ToList();

            ViewBag.PendingCertificates = paginatedPendingCertificates;
            ViewBag.ValidateCertificates = paginatedValidateCertificates;

            ViewBag.TotalPendingPages = (int)Math.Ceiling(pendingCertificates.Count / (double)pageSize);
            ViewBag.TotalValidatePages = (int)Math.Ceiling(validateCertificates.Count / (double)pageSize);
            ViewBag.CurrentPage = page;

            return View();
        }*/

        //Recuperation de l'attestation à partir de l'id de la ligne du tableau a partir de la selection lors du clic
        public ActionResult GetCertificate(int id)
        {
            if (id <= 0)
            {
                return new HttpStatusCodeResult(400, "Invalid certificate ID");
            }

            var certificate = _db.GetCerCertificateDataModelById(id);

            if (certificate == null)
            {
                return HttpNotFound("Certificate not found");
            }

            return Json(certificate, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetCertificatePdf(int id)
        {
            if (id <= 0)
            {
                return new HttpStatusCodeResult(400, "Invalid certificate ID");
            }

            var certificate = _db.GetCerCertificateDataModelById(id);

            //Console.WriteLine(certificate.CdmId);
            if (certificate == null)
            {
                return HttpNotFound("Certificate not found");
            }

            //Ancienne appelation de la methode de recuperation du fichier PDF
            /*byte[] pdfBytes = _certificateService.GenerateCertificatePdf(certificate);
            string fileName = $"Attestation-{certificate.Nom} {certificate.Prenom}.pdf";
            return File(pdfBytes, "application/pdf", fileName);*/

            // Utilisation de la nouvelle méthode DownloadCertificatePdf
            string filePath = _certificateService.DownloadCertificatePdf(certificate);
            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound("PDF file not found");
            }

            return File(filePath, "application/pdf", System.IO.Path.GetFileName(filePath));



        }

        [System.Web.Mvc.HttpPost]
        public ActionResult UpdateCertificate(EcoCerCertificateDataModel model)
        {
            Console.WriteLine(model.Sexe);
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Json(errors);
            }

            _db.UpdateCertificate(model);
            return RedirectToAction(nameof(ManageCertificates));
        }

        public JsonResult SearchPendingCertificates(string param)
        {
            string statut = Statut.En_attente.ToString().Replace("_", " ");
            var searchedPendingCertificates = _db.GetCertificatesByStatus(statut, param);
            return Json(searchedPendingCertificates, JsonRequestBehavior.AllowGet);

        }

        public JsonResult SearchValidateCertificates(string param)
        {
            string statut = Statut.Valide.ToString();
            var searchedValidateCertificates = _db.GetCertificatesByStatus(statut, param);
            return Json(searchedValidateCertificates, JsonRequestBehavior.AllowGet);

        }

        public ActionResult EditCertificateTemplate()
        {
            var template = _db.GetTemplate();

            if (template == null)
            {
                return HttpNotFound();
            }

            var FormModel = new EcoCerTempViewModel
            {
                HeaderText = template.HeaderText,
                TitleText = template.TitleText,
                BodyTextPart1 = template.BodyTextPart1,
                BodyTextPart2 = template.BodyTextPart4,
                BodyTextPart3 = template.BodyTextPart5,
                FooterTextPart1 = template.FooterTextPart1,
                FooterTextPart2 = template.FooterTextPart2,
            };

            return View(FormModel);
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult EditCertificateTemplate(EcoCerTempViewModel model)
        {
            if (ModelState.IsValid)
            {
                var isUpdated = _db.UpdateCertificateTemplate(model);
                if (isUpdated)
                {
                    TempData["SuccessMessage"] = "Le template a été mis à jour avec succès!";

                }
                return RedirectToAction(nameof(EditCertificateTemplate));
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Des valeurs de champ sont incorrectes, veuillez vérifier les informations saisies");
                return View("EditCertificateTemplate", model);
            }

        }

        public JsonResult ValidateCertificate(int certificateId)
        {

            //Console.WriteLine(certificateId);
            string statusText = Statut.Valide.ToString();
            string? userMail = _db.GetUserMailByCertificateId(certificateId);

            /*Console.WriteLine(certificateId);*/

            _db.UpdateCertificateStatus(statusText, certificateId);

            if (!string.IsNullOrEmpty(userMail))
            {
                string subject = "Etat d'avancement de la demande d'attestation de travail";
                string body = @"<p>Bonjour Cher(è) collègue,</p>
                                <p>Votre demande d'attestation de travail a été approuvée et validée. Nous vous prions de passer au
                                Département des Ressources Humaines ou vous faire représenter pour le retrait.</p>
                                <P>Merci.</p>";

                _mailService.SendEmail(userMail, subject, body);
            }

            //return Json(new { success = true });
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UploadStaffInfos()
        {
            return View();
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult UploadStaffInfos(HttpPostedFileBase excelFile)
        {
            if (excelFile == null || excelFile.ContentLength <= 0)
            {
                TempData["ErrorMessage"] = "Fichier invalide ou vide";
                return RedirectToAction(nameof(UploadStaffInfos));
            }

            //string uploadFolder = "D:/Projects/EcoCertificate/EcoCertificate/AppData/Uploads";
            string uploadFolder = Server.MapPath("~/EcoCerUploads");
            var staffRecords = new List<dynamic>();

            if (!Directory.Exists(uploadFolder))
            {
                try
                {
                    Directory.CreateDirectory(uploadFolder);

                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Impossible de créer le repertoire de téléchargement: {ex.Message}";
                    return RedirectToAction(nameof(UploadStaffInfos));
                }
            }

            string fileName = $"{Path.GetFileNameWithoutExtension(excelFile.FileName)}_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(excelFile.FileName)}";
            string filePath = Path.Combine(uploadFolder, fileName.Trim());

            try
            {
                excelFile.SaveAs(filePath);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de l'écriture du fichier: {ex.Message}";
                return RedirectToAction(nameof(UploadStaffInfos));
            }

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                    if (worksheet == null)
                    {
                        TempData["ErrorMessage"] = "Le fichier excel est vide ou ne contient aucune feuille de calcul valide";
                        return RedirectToAction(nameof(UploadStaffInfos));
                    }

                    int columnCount = worksheet.Dimension.End.Column;
                    int rowCount = worksheet.Dimension.End.Row;

                    /*Console.WriteLine("Number of columns in the sheet: " + columnCount);
                    Console.WriteLine("Number of rows in the sheet: " + rowCount);*/

                    //Reinitialisation de la table EcoCerStaffInfos
                    try
                    {
                        _logger.LogInfo("Début de la reinitialisation de la table EcoCerStaffInfos....");

                        bool wasReset = _db.ResetStaffInfos();

                        if (wasReset)
                        {
                            _logger.LogInfo("La table EcoCerStaffInfos a été réitinialisé avec succès");
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Impossible de reinitailiser les informations du staff. Erreur: {ex.Message}";
                        return RedirectToAction(nameof(UploadStaffInfos));
                    }

                    //Insertion des données dans la base de données
                    int startRow = 6;

                    /*Console.WriteLine("Recruitement date column data type: " + worksheet.Cells[5, 4].Value.GetType());
                    Console.WriteLine("Registration number column data type: " + worksheet.Cells[5, 5].Value.GetType());*/

                    for (int row = startRow; row <= rowCount; row++)
                    {
                        staffRecords.Add(new
                        {
                            Email = worksheet.Cells[row, 1].Value.ToString().Trim(),
                            NumeroCompte = worksheet.Cells[row, 2].Value.ToString().Trim(),
                            CategorieProfessionnelle = worksheet.Cells[row, 3].Value.ToString().Trim(),
                            DateRecrutement = worksheet.Cells[row, 4].Text.ToString().Trim(),
                            Matricule = worksheet.Cells[row, 5].Value.ToString().Trim()
                        });
                    }

                    try
                    {
                        foreach (var staffRecord in staffRecords)
                        {
                            /*Console.WriteLine("Email: " + staffRecord.Email);
                            Console.WriteLine("Account Number: " + staffRecord.NumeroCompte);
                            Console.WriteLine("Professional category: " + staffRecord.CategorieProfessionnelle);
                            Console.WriteLine("Recruitment date: " + staffRecord.DateRecrutement);
                            Console.WriteLine("Registration number: " + staffRecord.Matricule);*/
                            _db.InsertStaffInfos(staffRecord.Email, staffRecord.NumeroCompte, staffRecord.CategorieProfessionnelle, staffRecord.DateRecrutement, staffRecord.Matricule);
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Erreur lors de l'insertion des données. Erreur: {ex.Message}";
                        return RedirectToAction(nameof(UploadStaffInfos));
                    }


                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Les données n'ont pas pu être chargées correctement. Erreur: {ex.Message}";
                return RedirectToAction(nameof(UploadStaffInfos));
            }

            TempData["SuccessMessage"] = "Les données ont été chargées avec succès";
            return RedirectToAction(nameof(UploadStaffInfos));
        }

        public FileResult DownloadStaffTemplate()
        {
            string filePath = Server.MapPath("~/EcoCerUploads/Template de données ETG staff_new.xlsx");
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            string fileName = "Template de données ETG staff_V0.xlsx";

            return File(filePath, contentType, fileName);

        }

        //Partie création d'attestation pour les autres utilisateurs
        public ActionResult CreateUserCertificate()
        {
            return View();
        }

        //Rechercher un utilisateur
        public JsonResult SearchUser(string param)
        {
            try
            {
                if (!string.IsNullOrEmpty(param))
                {
                    var searchedUser = _db.SearchUser(param.Trim());
                    if (searchedUser != null && searchedUser.Rows.Count > 0)
                    {
                        //var row = searchedUser.Rows[0];
                        var userData = new
                        {
                            UserId = Convert.ToInt32(searchedUser.Rows[0]["IDUser"]),
                            Login = searchedUser.Rows[0]["Login"].ToString().Trim(),
                            Nom = searchedUser.Rows[0]["Nom"].ToString().Trim(),
                            Prenom = searchedUser.Rows[0]["Prenom"].ToString().Trim(),
                            CategorieProfessionnelle = searchedUser.Rows[0]["CategorieProfessionnelle"].ToString().Trim(),
                            DateRecrutement = searchedUser.Rows[0]["DateRecrutement"].ToString().Trim(),
                        };
                        return Json(new { statusCode = 200, data = userData }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { statusCode = 204, message = "Aucun utilisateur n'a été trouvé" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { statusCode = 400, message = "Paramètre de recherche invalide" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { statusCode = 500, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        //Création de la demande d'attestation
        [System.Web.Mvc.HttpPost]
        public ActionResult CreateUserCertificate([FromBody] CertificateFormData formData)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    var ExistingCertificateDataModel = _db.GetCertificateDataModel(formData.UserLogin);

                    if (ExistingCertificateDataModel != null)
                    {
                        bool WasInserted = _db.UpdateCertificateDataModel(ExistingCertificateDataModel.Login, formData.Nom.Trim(), formData.Prenom.Trim(), formData.Sexe.Trim(), formData.Civilite.Trim(), formData.DateRecrutement.Trim(), formData.CategorieProfessionnelle.Trim());

                        if (WasInserted)
                        {
                            int wasUpdated = UpdateCertificateReferences(formData.UserLogin.Trim(), WasInserted);

                            if (wasUpdated == 1)
                            {
                                return Json(new { statusCode = 200, message = "La demande a été soumise avec succès" }, JsonRequestBehavior.AllowGet);

                            }
                        }
                    }
                    else
                    {
                        var CertificateDataModel = new EcoCerCertificateDataModel
                        {
                            Login = formData.UserLogin.Trim(),
                            Nom = formData.Nom.Trim(),
                            Prenom = formData.Prenom.Trim(),
                            Sexe = formData.Sexe.Trim(),
                            Civilite = formData.Civilite.Trim(),
                            CategorieProfessionnelle = formData.CategorieProfessionnelle.Trim(),
                            DateRecrutement = _certificateService.FormatDateString(formData.DateRecrutement.Trim(), "dd/MM/yyyy")
                        };

                        bool WasInserted = false;
                        bool IsLoginCreated = false;

                        if (!_db.UserExists(formData.UserLogin.Trim()))
                        {
                            IsLoginCreated = _db.InsertLogin(formData.UserLogin.Trim());

                            if (IsLoginCreated)
                            {
                                WasInserted = _db.InsertCertificateDataModel(CertificateDataModel);
                            }
                            else
                            {
                                return Json(new { statusCode = 400, message = "Erreur au niveau de l'insertion du login" }, JsonRequestBehavior.AllowGet);
                            }
                        }
                        else
                        {
                            WasInserted = _db.InsertCertificateDataModel(CertificateDataModel);
                        }


                        if (WasInserted)
                        {
                            /*string statusText = Statut.En_attente.ToString().Replace("_", " ");

                            string refNumber = _certificateService.GenerateReferenceNumber();

                            bool WasAdded = _db.AddCertificateRefNumberAndStatus(formData.UserLogin.Trim(), refNumber, statusText);

                            if (WasAdded)
                            {
                                return Json(new { statusCode = 200, message = "La demande a été soumise avec succès" }, JsonRequestBehavior.AllowGet);
                            }*/
                            int wasUpdated = UpdateCertificateReferences(formData.UserLogin.Trim(), WasInserted);

                            if (wasUpdated == 1)
                            {
                                return Json(new { statusCode = 200, message = "La demande a été soumise avec succès" }, JsonRequestBehavior.AllowGet);

                            }
                        }

                    }
                }

                return Json(new { statusCode = 500, message = "Tentative de création de l'attestation échouée, prière vérifier les données envoyées" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { statusCode = 500, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        //Méthode qui met à jour le numéro de référence et le statut de l'attestation
        public int UpdateCertificateReferences(string userLogin, bool isTrue)
        {
            int returnValue = 0;

            if (isTrue)
            {
                string statusText = Statut.En_attente.ToString().Replace("_", " ");

                string refNumber = _certificateService.GenerateReferenceNumber();

                bool WasAdded = _db.AddCertificateRefNumberAndStatus(userLogin, refNumber, statusText);

                if (WasAdded)
                {
                    returnValue = 1;
                }
            }

            return returnValue;
        }
    }

}
