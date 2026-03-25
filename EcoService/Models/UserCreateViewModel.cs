using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Le matricule est requis.")]
        [Display(Name = "Matricule")]
        public int Matricule { get; set; }

        [Required(ErrorMessage = "Le nom est requis.")]
        [StringLength(100, ErrorMessage = "Le nom ne doit pas dépasser 100 caractères.")]
        [Display(Name = "Nom")]
        public string Nom { get; set; }

        [Required(ErrorMessage = "Le prénom est requis.")]
        [StringLength(100, ErrorMessage = "Le prénom ne doit pas dépasser 100 caractères.")]
        [Display(Name = "Prenom")]
        public string Prenom { get; set; }

        [Required(ErrorMessage = "Le numéro de compte est requis.")]
        [StringLength(14, ErrorMessage = "Le numéro de compte ne doit pas dépasser 14 caractères.")]
        [Display(Name = "Nom")]
        public string NumeroCompte { get; set; }

        [Required(ErrorMessage = "L'adresse email est requise.")]
        [EmailAddress(ErrorMessage = "Veuillez entrer une adresse email valide.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Un rôle doit être sélectionné.")]
        [Display(Name = "Rôle")]
        public int RoleId { get; set; }
    }

}