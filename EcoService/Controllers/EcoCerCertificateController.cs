using EcoService.Models;
using iText.Kernel.Font;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using static EcoService.Models.Enums;
using EcoService.Controllers;
using Ecoservice.Models;
using Microsoft.Extensions.Logging;
//using ActionResult = System.Web.Mvc.ActionResult;
//using ActionResult = Microsoft.AspNetCore.Mvc.ActionResult;


namespace EcoService.Controllers
{
    //[System.Web.Mvc.Authorize(Roles ="Admin,User")]
    public class EcoCerCertificateController : Controller
    {
        private readonly EcoCerLogger _logger;
        private readonly EcoCerDbUtility _db;
        LoginViewModel _loginViewModel;
        private readonly EcoCerCertificateService _certificateService;


        public EcoCerCertificateController()
        {
            _logger = new EcoCerLogger();  // Initialisation manuelle
            _db = new EcoCerDbUtility();   // Initialisation manuelle
            _certificateService = new EcoCerCertificateService();

        }

        public EcoCerCertificateController(EcoCerLogger logger, EcoCerDbUtility db)
        {
            _logger = logger;
            _db = db;
        }

        // GET: EcoCerCertificate
        public System.Web.Mvc.ActionResult Index()
        {
            return View();
        }

        //Action qui charge des données sur le formulaire de la vue GenerateCertificate
        public System.Web.Mvc.ActionResult GenerateCertificate()
        //public System.Web.Mvc.ActionResult GenerateCertificate(string login, string civilite, string genre)
        {
            try
            {
                //Récupération du nom d'utilisateur de l'utilisateur connecté
                //var userLogin = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
                var userLogin = User.Identity.Name;

                if (userLogin == null)
                {
                    // return Unauthorized();
                    return new HttpStatusCodeResult(401, "Non autorisé");
                }

                //_logger?.LogInformation("_db est-il null ? {DbState}", _db == null ? "OUI" : "NON");


                //Récupération du modèle d'attestation de l'utilisateur connecté à partir de son id
                var certificateDataModel = _db.GetCertificateDataModel(userLogin);

                //var certificateDataModel = _db.GetCertificateDataModel(userLogin,civilite,genre);

                /*Si le modèle d'attestation existe on charge le modèle dans certificateFormModel pour l'afficher sur la page, sinon
				 on charge les informations de l'utilisateur*/
                if (!string.IsNullOrWhiteSpace(userLogin))
                {
                    if (certificateDataModel != null)
                    {
                        var FormModel = new EcoCerCertificateFormModel
                        {
                            Nom = certificateDataModel.Nom,
                            Prenom = certificateDataModel.Prenom,
                            Sexe = certificateDataModel.Sexe,
                            Civilite = certificateDataModel.Civilite,
                            CategorieProfessionnelle = certificateDataModel.CategorieProfessionnelle,
                            DateRecrutement = certificateDataModel.DateRecrutement
                            //DateRecrutement = certificateDataModel.DateRecrutement?.ToString("dd/MM/yyyy"),
                            //SexOptions = new SelectList(Enum.GetValues(typeof(Sexe)).Cast<Sexe>(), certificateDataModel.Sexe),
                            //CiviliteOptions = new SelectList(Enum.GetValues(typeof(Civilite)).Cast<Civilite>(), certificateDataModel.Civilite),
                        };

                        return View("GenerateCertificate", FormModel);
                    }
                    else
                    {
                        var user = _db.GetUserByLogin(userLogin);

                        if (user != null)
                        {
                            var FormModel2 = new EcoCerCertificateFormModel

                            {
                                Nom = user.Nom,
                                Prenom = user.Prenom,
                                Sexe = user.Sexe,
                                Civilite = user.Civilite,
                                CategorieProfessionnelle = user.CategorieProfessionnelle,
                                DateRecrutement = user.DateRecrutement
                                //SexOptions = new SelectList(Enum.GetValues(typeof(Sexe)).Cast<Sexe>(), user.Sexe),
                                //CiviliteOptions = new SelectList(Enum.GetValues(typeof(Civilite)).Cast<Civilite>(), user.Civilite),
                            };

                            return View("GenerateCertificate", FormModel2);
                        }
                    }
                }
                return RedirectToAction("GenerateCertificate", "EcoCerCertificate");
            }
            catch (Exception ex)
            {
                _logger.LogError("An unexpected error occured in GenerateCertificate post action method:", ex);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return RedirectToAction(nameof(GenerateCertificate));
            }
        }

        //Action permettant d'enrégistrer un modèle d'attestation de travail, les modèles sont utilisés pour générer les attestations
        [System.Web.Mvc.HttpPost]
        public System.Web.Mvc.ActionResult GenerateCertificate(EcoCerCertificateFormModel model)
        {
            try
            {
                //var userLogin = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Name)?.Value;
                var userLogin = User.Identity.Name;

                if (userLogin == null)
                {
                    //return Unauthorized();
                    return new HttpStatusCodeResult(400, "Invalid certificate ID");


                }

                if (ModelState.IsValid)
                {
                    string formattedDateRecrutement = _certificateService.FormatDateString(model.DateRecrutement.Trim(), "dd/MM/yyyy");

                    //Verification de l'existence d'un modèle, si oui on met à jour l'existant sinon on créé un nouveau
                    var ExistingCertificateDataModel = _db.GetCertificateDataModel(userLogin);

                    if (ExistingCertificateDataModel != null)
                    {

                        /*string[] includedProperties = { "Nom", "Prenom", "Sexe", "Civilite", "CategorieProfessionnelle"};
						bool hasChanges = HasChanges(ExistingCertificateDataModel, model, includedProperties);*/
                        //_db.UpdateCertificateDataModel(ExistingCertificateDataModel.Login, model.Nom, model.Prenom, model.Sexe, model.Civilite, model.DateRecrutement, model.CategorieProfessionnelle);
                        _db.UpdateCertificateDataModel(ExistingCertificateDataModel.Login, model.Nom.Trim(), model.Prenom.Trim(), model.Sexe.Trim(), model.Civilite.Trim(), model.DateRecrutement.Trim(), model.CategorieProfessionnelle.Trim());

                    }
                    else
                    {
                        bool isDateOnly = false;
                        DateTime ParsedDateRecrutement;
                        isDateOnly = DateTime.TryParse(model.DateRecrutement, out ParsedDateRecrutement);
                        var CertificateDataModel = new EcoCerCertificateDataModel
                        {
                            Login = userLogin,
                            Nom = model.Nom.Trim(),
                            Prenom = model.Prenom.Trim(),
                            Sexe = model.Sexe.Trim(),
                            Civilite = model.Civilite.Trim(),
                            CategorieProfessionnelle = model.CategorieProfessionnelle.Trim(),
                            //DateRecrutement = isDateOnly ? DateTime.ParseExact(model.DateRecrutement, "dd/MM/yyyy", CultureInfo.InvariantCulture) : ParsedDateRecrutement
                            //DateRecrutement = model.DateRecrutement.Trim()
                            DateRecrutement = formattedDateRecrutement
                        };
                        //var CertificateDataModel = new EcoCerCertificateDataModel
                        //{
                        //    Login = userLogin,
                        //    Nom = model.Nom,
                        //    Prenom = model.Prenom,
                        //    Sexe = model.Sexe,
                        //    Civilite = model.Civilite,
                        //    CategorieProfessionnelle = model.CategorieProfessionnelle,
                        //    //DateRecrutement = DateOnly.ParseExact(model.DateRecrutement, "dd/MM/yyyy", CultureInfo.InvariantCulture)
                        //    DateRecrutement = DateTime.ParseExact(model.DateRecrutement, "dd/MM/yyyy", CultureInfo.InvariantCulture)

                        //};
                        //Verification de l'existence de Login dans la table EcoCerUsers
                        if (!_db.UserExists(userLogin))
                        {
                            _db.InsertLogin(userLogin);
                        }
                        else
                        { 
                            _db.InsertCertificateDataModel(CertificateDataModel);
                        }
                    }

                    return RedirectToAction(nameof(CertificateOverview));
                }
                else
                {
                    //Chargement des valeurs des types enums defini dans le fichier Enums.cs dans les <Select /> de la vue GenerateCertificate
                    //model.SexOptions = new SelectList(Enum.GetValues(typeof(Sexe)).Cast<Sexe>(), model.Sexe);
                    //model.CiviliteOptions = new SelectList(Enum.GetValues(typeof(Civilite)).Cast<Civilite>(), model.Civilite);

                    return View("GenerateCertificate", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An unexpected error occured in GenerateCertificate post action method:", ex);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return RedirectToAction(nameof(GenerateCertificate));
            }

        }

        /*Action permettant d'afficher une vue d'ensemble des informations enregistrées par l'utilisateur, 
		Ces informations sont récupérées depuis les modèles d'attestation des utilisateurs.*/
        public System.Web.Mvc.ActionResult CertificateOverview()
        {
            try
            {
                var userLogin = User.Identity.Name;
                //var userLogin = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Name)?.Value;

                if (userLogin == null)
                {
                    //return Unauthorized();
                    return new HttpStatusCodeResult(400, "Invalid certificate ID");

                }

                var certificateDataModel = _db.GetCertificateDataModel(userLogin);

                if (certificateDataModel == null)
                {
                    return RedirectToAction(nameof(GenerateCertificate));
                }

                return View("CertificateOverview", certificateDataModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("An unexpected error occured in CertificateOverview action method:", ex);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return RedirectToAction(nameof(CertificateOverview));
            }
        }

        //Action permettant d'enregistrer les informations de référence d'une attestation comme sa date de création ou son numéro de référence
        public System.Web.Mvc.ActionResult CreateCertificate()
        {
            try
            {
                var userLogin = User.Identity.Name;
                //var userLogin = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Name)?.Value;

                if (userLogin == null)
                {
                    // return Unauthorized();
                    return new HttpStatusCodeResult(400, "Invalid certificate ID");
                }

                var certificateDataModel = _db.GetCertificateDataModel(userLogin);

                if (certificateDataModel == null)
                {
                    //return NotFound();
                    return HttpNotFound("Certificate not found");
                }

                /*if (certificateDataModel.RefNumber == "000")
    {
        string refNumber = GenerateReferenceNumber();
        _db.AddCertificateRefNumber(userLogin, refNumber);
    }*/

                /*int cdmId = Convert.ToInt32(certificateDataModel.CdmId);*/

                string statusText = Statut.En_attente.ToString().Replace("_", " ");

                string refNumber = _certificateService.GenerateReferenceNumber();

                //_db.AddCertificateRefNumber(userLogin, refNumber);
                _db.AddCertificateRefNumberAndStatus(userLogin, refNumber, statusText);

                //_db.UpdateCertificateStatus(statusText, certificateDataModel.CdmId);

                /* return RedirectToAction(nameof(GeneratePdf));*/

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                _logger.LogError("An unexpected error occured in CreateCertificate action method:", ex);
                TempData["ErrorMessage"] = $"An unexpected error occurred. Please try again. Error: {ex.Message}";
                /*return RedirectToAction(nameof(CertificateOverview));*/
                return Json(new { success = false, message = "An error occurred while creating the certificate" }, JsonRequestBehavior.AllowGet);
            }
        }
        //[System.Web.Mvc.HttpPost]
        //public JsonResult CreateCertificate()
        //{
        //    try
        //    {
        //        var userLogin = User.Identity.Name;
        //        if (userLogin == null)
        //        {
        //            return Json(new { success = false, error = "Invalid user" });
        //        }

        //        var certificateDataModel = _db.GetCertificateDataModel(userLogin);
        //        if (certificateDataModel == null)
        //        {
        //            return Json(new { success = false, error = "Certificate not found" });
        //        }

        //        string statusText = Statut.En_attente.ToString().Replace("_", " ");
        //        string refNumber = _certificateService.GenerateReferenceNumber();

        //        _db.AddCertificateRefNumber(userLogin, refNumber);
        //        _db.UpdateCertificateStatus(statusText, certificateDataModel.CdmId);

        //        return Json(new { success = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("An unexpected error occurred in CreateCertificate action method:", ex);
        //        return Json(new { success = false, error = "An unexpected error occurred. Please try again." });
        //    }

        //}

    }
}
