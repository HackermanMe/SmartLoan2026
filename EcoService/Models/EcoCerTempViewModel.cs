using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class EcoCerTempViewModel
    {
        [Required(ErrorMessage = "La valeur du texte d'entête est incorrecte")]
        [MaxLength(25, ErrorMessage = "Ce champ ne peut pas dépasser 25 caractères")]
        public string? HeaderText { get; set; }

        [Required(ErrorMessage = "La valeur du titre est incorrecte")]
        [MaxLength(25, ErrorMessage = "Ce champ ne peut pas dépasser 25 caractères")]
        public string? TitleText { get; set; }

        [Required(ErrorMessage = "La valeur du texte de la partie 1 du corps du texte est incorrecte")]
        [MaxLength(100, ErrorMessage = "Ce champ ne peut pas dépasser 100 caractères")]
        public string? BodyTextPart1 { get; set; }

        [Required(ErrorMessage = "La valeur du texte de la partie 2 du corps du texte est incorrecte")]
        [MaxLength(50, ErrorMessage = "Ce champ ne peut pas dépasser 50 caractères")]
        public string? BodyTextPart2 { get; set; }

        [Required(ErrorMessage = "La valeur du texte de la partie 3 du corps du texte est incorrecte")]
        [MaxLength(100, ErrorMessage = "Ce champ ne peut pas dépasser 100 caractères")]
        public string? BodyTextPart3 { get; set; }

        [Required(ErrorMessage = "La valeur du texte reférençant le nom & prenom du responsable est incorrecte")]
        [MaxLength(30, ErrorMessage = "Ce champ ne peut pas dépasser 30 caractères")]
        public string? FooterTextPart1 { get; set; }

        [Required(ErrorMessage = "La valeur du texte reférençant le titre du responsable est incorrecte")]
        [MaxLength(50, ErrorMessage = "Ce champ ne peut pas dépasser 50 caractères")]
        public string? FooterTextPart2 { get; set; }
    }
}
