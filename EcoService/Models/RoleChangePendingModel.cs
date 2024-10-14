using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EcoService.Models
{
    public class RoleChangePendingModel
    {
        public int Id { get; set; }
        public int Matricule { get; set; }
        public int NewRole { get; set; }
        public int ProposedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // Champs supplémentaires pour afficher les noms complets
        public string NomComplet { get; set; }
        public string ProposePar { get; set; }

        // Optionnel : Ajoutez des propriétés pour afficher le rôle proposé sous forme de texte
        public string RolePropose
        {
            get
            {
                switch (NewRole)
                {
                    case 1:
                        return "User";
                    case 2:
                        return "Staff";
                    case 100:
                        return "RH";
                    case 101:
                        return "Contrôleur";
                    default:
                        return "N/A";
                }
            }
        }
    }
}
