/*Model utilisé pour charger les données du CertificateDataModel dans la vue GenerateCertificate et pour récupérer les 
 informations saisies dans la vue pour les insérer dans la base de données*/

using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace EcoService.Models
{
    public class EcoCerCertificateFormModel
    {
        [Required(ErrorMessage = "Vous ne pouvez pas laisser ce champ vide")]
        public string? Nom { get; set; }

        [Required(ErrorMessage = "Vous ne pouvez pas laisser ce champ vide")]
        public string? Prenom { get; set; }

        [Required(ErrorMessage = "Veuillez définir votre sexe")]
        public string? Sexe { get; set; }

        [Required(ErrorMessage = "Veuillez définir votre civilité")]
        public string? Civilite { get; set; }

        [Required(ErrorMessage = "Cette information est nécessaire à la génération de votre attestation")]
        public string? CategorieProfessionnelle { get; set; }

        [Required(ErrorMessage = "Cette information est necessaire à la génération de votre attestation")]
        public string? DateRecrutement { get; set; }

        public SelectList? SexOptions { get; set; }

        public SelectList? CiviliteOptions { get; set; }
    }
}