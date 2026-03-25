using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class EcoCerCertificateDataModel
    {

        [Required(ErrorMessage = "La valeur de l'id est incorrecte")]
        public int CdmId { get; set; }

        [MaxLength(5)]
        //[Required(ErrorMessage = "Le numéro de référence est incorrecte")]
        public string? RefNumber { get; set; }

        //[Required(ErrorMessage ="La valeur du nom d'utilisateur est incorrecte")]
        public string? Login { get; set; }

        [MaxLength(100)]
        [Required(ErrorMessage = "La valeur du nom est incorrecte")]
        public string? Nom { get; set; }

        [MaxLength(100)]
        [Required(ErrorMessage = "La valeur du prenom est incorrecte")]
        public string? Prenom { get; set; }

        [MaxLength(7)]
        [Required(ErrorMessage = "La valeur du sexe est incorrecte")]
        public string? Sexe { get; set; }

        [MaxLength(10)]
        [Required(ErrorMessage = "La valeur de la civilité est incorrecte")]
        public string? Civilite { get; set; }

        [MaxLength(20)]
        [Required(ErrorMessage = "La valeur de la catégorie professionnelle est incorrecte")]
        public string? CategorieProfessionnelle { get; set; }

        //[DataType(DataType.Date)]
        [Required(ErrorMessage = "La  valeur de la date de recrutement est incorrecte")]
        //public DateOnly? DateRecrutement { get; set; }
        //public DateTime? DateRecrutement { get; set; }
        public string? DateRecrutement { get; set; }

        [MaxLength(25)]
        public string? Statut { get; set; }

        [DataType(DataType.Date)]
        //[Required(ErrorMessage = "La valeur de la date de création est incorrect")]
        //public DateTime? CreationDate { get; set; }
        //public string? CreationDate { get; set; }
        public DateTime? CreationDate { get; set; }

        public int CreationYear { get; set; }
    }
}