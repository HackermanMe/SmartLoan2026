/*
 * =============================================================================
 * CALCULATEUR TEG - FORMULE EXCEL RATE
 * =============================================================================
 * Calcule le Taux Effectif Global selon la formule Excel RATE.
 *
 * Formule Excel officielle:
 * TEG = (1 + TAUX(duree; mensualiteHT; -(Montant - Frais - AssuranceTotale); 0))^12 - 1
 *
 * Important:
 * - La mensualite utilisee est SANS assurance (mensualite HT)
 * - L'assurance TOTALE est soustraite du capital (financee dans le pret)
 * - L'algorithme utilise la methode Newton-Raphson pour resoudre l'equation
 * =============================================================================
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EcoService.Services.Amortissement
{
    /// <summary>
    /// Interface pour le calcul du TEG
    /// </summary>
    public interface ITEGCalculator
    {
        /// <summary>
        /// Calcule le TEG selon la formule Excel RATE
        /// </summary>
        /// <param name="duree">Nombre de periodes (echeances)</param>
        /// <param name="mensualiteHT">Mensualite SANS assurance</param>
        /// <param name="montantPret">Montant du pret</param>
        /// <param name="fraisDossier">Frais de dossier</param>
        /// <param name="primeAssuranceTotale">Prime d'assurance TOTALE sur la duree du pret</param>
        /// <returns>TEG en pourcentage</returns>
        decimal CalculerTEG(int duree, decimal mensualiteHT, decimal montantPret, decimal fraisDossier, decimal primeAssuranceTotale);
    }

    /// <summary>
    /// Implementation du calcul du TEG par Newton-Raphson - calculs en decimal
    /// </summary>
    public class TEGCalculator : ITEGCalculator
    {
        private const int MAX_ITERATIONS = 100;
        private const decimal TOLERANCE = 0.0000000001m;

        private static decimal DecimalPow(decimal baseVal, decimal exp)
        {
            return (decimal)Math.Pow((double)baseVal, (double)exp);
        }

        /// <summary>
        /// Calcule le TEG selon la formule Excel RATE - tous calculs en decimal
        /// </summary>
        public decimal CalculerTEG(int duree, decimal mensualiteHT, decimal montantPret, decimal fraisDossier, decimal primeAssuranceTotale)
        {
            if (duree <= 0 || mensualiteHT <= 0 || montantPret <= 0)
                return 0;

            decimal capitalNet = montantPret - fraisDossier - primeAssuranceTotale;
            if (capitalNet <= 0) return 0;

            decimal pmt = -mensualiteHT;
            decimal pv = capitalNet;
            decimal fv = 0;

            decimal tauxPeriodique = CalculerTauxExcelRATE(duree, pmt, pv, fv);

            if (tauxPeriodique <= -1)
                return 0;

            decimal tegAnnuel = DecimalPow(1m + tauxPeriodique, 12m) - 1m;
            return tegAnnuel * 100m;
        }

        private decimal CalculerTauxExcelRATE(int nper, decimal pmt, decimal pv, decimal fv, int type = 0, decimal guess = 0.1m)
        {
            decimal rate = guess / 12m;

            for (int i = 0; i < MAX_ITERATIONS; i++)
            {
                decimal temp = DecimalPow(1m + rate, nper);
                decimal f, df;

                if (Math.Abs(rate) < 0.0000000001m)
                {
                    f = pv + pmt * nper + fv;
                    df = pmt * nper * (nper - 1) / 2m;
                }
                else
                {
                    decimal term1 = (temp - 1m) / rate;
                    f = pv * temp + pmt * (1m + rate * type) * term1 + fv;

                    decimal dterm1 = (nper * temp / (1m + rate) - term1) / rate;
                    df = pv * nper * temp / (1m + rate) + pmt * (1m + rate * type) * dterm1 + pmt * type * term1;
                }

                if (Math.Abs(df) < 0.000000000000001m)
                    break;

                decimal newRate = rate - f / df;

                if (Math.Abs(newRate - rate) < TOLERANCE)
                    return newRate;

                rate = newRate;
                if (rate < -0.99m) rate = -0.99m;
                if (rate > 1m) rate = 1m;
            }

            return rate;
        }
    }
}