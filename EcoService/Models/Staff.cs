using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EcoService.Models
{
    public class Staff
    {
        public int StaffId { get; set; }
        public string Matricule { get; set; }
        public string Email { get; set; }
        public string NumeroDeCompte { get; set; }
        public string SalaireNet { get; set; }
    }
}