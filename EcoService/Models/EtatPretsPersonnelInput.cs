using System;
using System.ComponentModel.DataAnnotations;

namespace EcoService.Models
{
    public class EtatPretsPersonnelInput
    {
        // ── Informations du demandeur ──────────────────────────────────────────
        public string NomDemandeur { get; set; } = string.Empty;
        public string ClasseCategorie { get; set; } = string.Empty;
        public string Departement { get; set; } = string.Empty;
        public int NbreAnnees { get; set; }
        public string Fonction { get; set; } = string.Empty;

        // ── Coordonnées du souscripteur (billet à ordre) ──────────────────────
        public string Adresse { get; set; } = string.Empty;
        public string BP { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;

        // ── Prêt sollicité ────────────────────────────────────────────────────
        public string TypePret { get; set; } = "Prêt personnel";
        public string NumeroCompte { get; set; } = string.Empty;

        [Required, Range(1, double.MaxValue)]
        public decimal Montant { get; set; }

        public int NbreDifferes { get; set; } = 0;

        [Required, Range(1, 600)]
        public int NbreEcheances { get; set; }

        [Required, Range(0, 100)]
        public decimal TauxAnnuel { get; set; }   // HT en %

        [Range(0, 100)]
        public decimal TauxTAF { get; set; } = 10;  // taxe en %

        public DateTime DateDebut { get; set; } = DateTime.Today;

        // ── Prêts existants Ecobank ───────────────────────────────────────────
        public string TypePretExistant { get; set; } = "Néant";
        public decimal? MontantPretExistant { get; set; }
        public decimal? MensualitesPretExistant { get; set; }

        // ── Autres crédits 1 ──────────────────────────────────────────────────
        public string TypeAutresCredits1 { get; set; } = "Néant";
        public decimal? MontantAutresCredits1 { get; set; }
        public decimal? MensualitesAutresCredits1 { get; set; }

        // ── Autres crédits 2 ──────────────────────────────────────────────────
        public string TypeAutresCredits2 { get; set; } = "Néant";
        public decimal? MontantAutresCredits2 { get; set; }
        public decimal? MensualitesAutresCredits2 { get; set; }

        // ── Situation financière ──────────────────────────────────────────────
        public decimal? SalaireNetActuel { get; set; }

        public decimal? CumulPretPersonnelA { get; set; }
        public decimal? FPE_B { get; set; }
        public decimal? CumulPretPersonnelFPE { get; set; }

        // ── Assurance ────────────────────────────────────────────────────────
        public decimal? TauxAssurance { get; set; }
        public decimal? AssuranceFixe { get; set; }
    }
}
