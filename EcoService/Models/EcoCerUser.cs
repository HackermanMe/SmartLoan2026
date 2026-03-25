//Model représentant un utilisateur

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace EcoService.Models
{
    public class EcoCerUser
    {
        public int UserId { get; set; }

        [MaxLength(25)]
        public string? Login { get; set; }

        [MaxLength(10)]
        public string? Role { get; set; }
    }
}