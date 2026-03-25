/*
 * =============================================================================
 * HELPER DE CALCUL - METHODES UTILITAIRES
 * =============================================================================
 * Contient les methodes utilitaires pour les calculs d'amortissement:
 * - Calcul du taux periodique
 * - Calcul de la mensualite
 * - Calcul de l'assurance par echeance
 * - Calcul des dates
 * - Calcul des interets selon convention de jours
 * =============================================================================
 */

using EcoService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace EcoService.Services.Amortissement
{    /// <summary>
     /// Interface pour les methodes utilitaires de calcul
     /// </summary>
    public interface ICalculHelper
    {
        /// <summary>
        /// Calcule le taux periodique a partir du taux annuel
        /// </summary>
        decimal CalculerTauxPeriodique(decimal tauxAnnuel, Periodicite periodicite, TypeTaux typeTaux);

        /// <summary>
        /// Calcule la mensualite avec la formule d'annuite constante
        /// </summary>
        decimal CalculerMensualite(decimal capital, decimal tauxPeriodique, int nombreEcheances);

        /// <summary>
        /// Calcule le montant d'assurance par echeance
        /// </summary>
        decimal CalculerAssuranceParEcheance(AmortissementInput input);

        /// <summary>
        /// Calcule la date de la prochaine echeance selon la periodicite
        /// </summary>
        DateTime CalculerProchaineDate(DateTime dateActuelle, Periodicite periodicite);

        /// <summary>
        /// Calcule le coefficient d'interet selon la convention de jours
        /// </summary>
        decimal CalculerCoefficientInteret(DateTime dateDebut, DateTime dateFin, decimal tauxAnnuel, ConventionJours convention);

        /// <summary>
        /// Retourne le libelle de la periodicite
        /// </summary>
        string GetPeriodiciteLibelle(Periodicite periodicite);

        /// <summary>
        /// Retourne le libelle du mode de remboursement
        /// </summary>
        string GetModeRemboursementLibelle(ModeRemboursement mode);

        /// <summary>
        /// Calcule le taux periodique implicite a partir de la mensualite (equivalent TAUX Excel)
        /// </summary>
        decimal CalculerTauxImplicite(decimal capital, decimal mensualite, int nombreEcheances, decimal tauxInitial = 0.01m);
    }

    /// <summary>
    /// Implementation des methodes utilitaires de calcul - tous calculs en decimal
    /// </summary>
    public class CalculHelper : ICalculHelper
    {
        /// <summary>
        /// Puissance decimale via Math.Pow (seule utilisation de double)
        /// </summary>
        private static decimal DecimalPow(decimal baseVal, decimal exponent)
        {
            return (decimal)Math.Pow((double)baseVal, (double)exponent);
        }

        /// <summary>
        /// Calcule le taux periodique a partir du taux annuel selon le type de taux
        /// </summary>
        public decimal CalculerTauxPeriodique(decimal tauxAnnuel, Periodicite periodicite, TypeTaux typeTaux)
        {
            int periodesParAn = 12 / (int)periodicite;

            if (typeTaux == TypeTaux.Actuariel)
            {
                // Taux actuariel: (1 + taux annuel)^(1/n) - 1
                decimal tauxActuariel = DecimalPow(1m + tauxAnnuel / 100m, 1m / periodesParAn) - 1m;
                return tauxActuariel * 100m;
            }
            else
            {
                return tauxAnnuel / periodesParAn;
            }
        }

        /// <summary>
        /// Calcul de la mensualite - tous calculs en decimal
        /// Formule: M = C * (t * (1+t)^n) / ((1+t)^n - 1)
        /// </summary>
        public decimal CalculerMensualite(decimal capital, decimal tauxPeriodique, int nombreEcheances)
        {
            if (tauxPeriodique == 0)
            {
                return capital / nombreEcheances;
            }

            decimal t = tauxPeriodique / 100m;
            decimal puissance = DecimalPow(1m + t, nombreEcheances);
            decimal mensualite = capital * t * puissance / (puissance - 1m);

            return mensualite;
        }

        /// <summary>
        /// Calcule le taux periodique implicite (Newton-Raphson) - calculs en decimal
        /// </summary>
        public decimal CalculerTauxImplicite(decimal capital, decimal mensualite, int nombreEcheances, decimal tauxInitial = 0.01m)
        {
            decimal r = tauxInitial / 100m;
            const int maxIterations = 100;
            const decimal tolerance = 0.0000000001m;

            for (int i = 0; i < maxIterations; i++)
            {
                decimal pow = DecimalPow(1m + r, nombreEcheances);
                decimal f = mensualite - capital * r * pow / (pow - 1m);

                decimal pow_n1 = DecimalPow(1m + r, nombreEcheances - 1);
                decimal df = -capital * (pow * (pow - 1m) - r * nombreEcheances * pow_n1 * (pow - 1m) + r * pow * nombreEcheances * pow_n1) / ((pow - 1m) * (pow - 1m));

                if (Math.Abs(df) < tolerance)
                    break;

                decimal rNew = r - f / df;

                if (Math.Abs(rNew - r) < tolerance)
                {
                    r = rNew;
                    break;
                }

                r = rNew;
                if (r < 0) r = 0.0001m;
                if (r > 1) r = 0.5m;
            }

            return r * 100m;
        }

        /// <summary>
        /// Calcule le montant d'assurance par echeance (montant fixe)
        /// </summary>
        public decimal CalculerAssuranceParEcheance(AmortissementInput input)
        {
            return input.AssuranceFixe;
        }

        /// <summary>
        /// Calcule la date de la prochaine echeance selon la periodicite
        /// </summary>
        public DateTime CalculerProchaineDate(DateTime dateActuelle, Periodicite periodicite)
        {
            return periodicite switch
            {
                Periodicite.Mensuel => dateActuelle.AddMonths(1),
                Periodicite.Trimestriel => dateActuelle.AddMonths(3),
                Periodicite.Semestriel => dateActuelle.AddMonths(6),
                Periodicite.Annuel => dateActuelle.AddYears(1),
                _ => dateActuelle.AddMonths(1)
            };
        }

        /// <summary>
        /// Calcule le coefficient d'interet selon la convention de jours
        /// </summary>
        public decimal CalculerCoefficientInteret(DateTime dateDebut, DateTime dateFin, decimal tauxAnnuel, ConventionJours convention)
        {
            if (convention == ConventionJours.JoursExacts)
            {
                // Exact/365: jours reels / 365
                int joursExacts = (dateFin - dateDebut).Days;
                return tauxAnnuel / 100 * joursExacts / 365m;
            }
            else
            {
                // 30/360: calcul selon convention bancaire
                int jours360 = CalculerJours360(dateDebut, dateFin);
                return tauxAnnuel / 100 * jours360 / 360m;
            }
        }

        /// <summary>
        /// Calcule le nombre de jours selon la convention 30/360 (European)
        /// Formule: (Y2-Y1)*360 + (M2-M1)*30 + (D2-D1)
        /// </summary>
        private int CalculerJours360(DateTime d1, DateTime d2)
        {
            int jour1 = Math.Min(d1.Day, 30);
            int jour2 = Math.Min(d2.Day, 30);

            return (d2.Year - d1.Year) * 360 + (d2.Month - d1.Month) * 30 + (jour2 - jour1);
        }

        /// <summary>
        /// Retourne le libelle de la periodicite
        /// </summary>
        public string GetPeriodiciteLibelle(Periodicite periodicite)
        {
            return periodicite switch
            {
                Periodicite.Mensuel => "Mensuel",
                Periodicite.Trimestriel => "Trimestriel",
                Periodicite.Semestriel => "Semestriel",
                Periodicite.Annuel => "Annuel",
                _ => "Mensuel"
            };
        }

        /// <summary>
        /// Retourne le libelle du mode de remboursement
        /// </summary>
        public string GetModeRemboursementLibelle(ModeRemboursement mode)
        {
            return mode switch
            {
                ModeRemboursement.EcheancesConstantes => "Echeances constantes",
                ModeRemboursement.AmortissementConstant => "Amortissement constant",
                ModeRemboursement.InFine => "In Fine",
                _ => "Echeances constantes"
            };
        }
    }
}
