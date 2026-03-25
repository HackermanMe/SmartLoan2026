using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class EcoCerUserLoginModel
    {
        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        public string? Login { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        public string Password { get; set; }
    }
}