/*
 * =============================================================================
 * HELPER POUR LES STYLES D'EXPORT
 * =============================================================================
 * Contient les constantes et methodes utilitaires pour le formatage des exports.
 * Assure la coherence visuelle entre Excel, PDF et Word.
 * =============================================================================
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EcoService.Services.Export
{
    /// <summary>
    /// Constantes et methodes utilitaires pour le style des exports
    /// </summary>
    public static class ExportStyleHelper
    {
        // Couleurs du theme bancaire (bleu)
        public const string COULEUR_PRIMAIRE = "#0078D4";      // Bleu principal
        public const string COULEUR_SECONDAIRE = "#106EBE";    // Bleu fonce
        public const string COULEUR_HEADER = "#E6F2FF";        // Bleu tres clair
        public const string COULEUR_DIFFERE = "#FFF3E0";       // Orange clair pour differe
        public const string COULEUR_BLANC = "#FFFFFF";

        // Tailles de police
        public const int FONT_SIZE_TITRE = 18;
        public const int FONT_SIZE_SOUS_TITRE = 14;
        public const int FONT_SIZE_NORMAL = 10;
        public const int FONT_SIZE_PETIT = 9;

        // En-tetes du tableau d'amortissement (meme ordre que le web)
        public static readonly string[] TABLEAU_HEADERS = new[]
        {
            "Pmt",
            "Date",
            "Balance Debut",
            "Balance Fin",
            "Principal",
            "Interet TTC",
            "Interet HT",
            "TPS",
            "Echeance"
        };

        // Largeurs des colonnes (en pourcentage)
        public static readonly int[] COLONNES_LARGEURS = new[]
        {
            5,   // Pmt
            10,  // Date
            12,  // Balance Debut
            12,  // Balance Fin
            11,  // Principal
            11,  // Interet TTC
            11,  // Interet HT
            9,   // TPS
            11   // Echeance
        };

        /// <summary>
        /// Formate un montant avec separateur de milliers
        /// </summary>
        public static string FormatMontant(decimal montant)
        {
            return montant.ToString("N0");
        }

        /// <summary>
        /// Formate une date au format francais
        /// </summary>
        public static string FormatDate(DateTime date)
        {
            return date.ToString("dd/MM/yyyy");
        }

        /// <summary>
        /// Formate un pourcentage
        /// </summary>
        public static string FormatPourcentage(decimal valeur)
        {
            return $"{valeur:N2}%";
        }

        /// <summary>
        /// Retire le # du code couleur pour OpenXml
        /// </summary>
        public static string CouleurSansHash(string couleur)
        {
            return couleur.Replace("#", "");
        }
    }
}