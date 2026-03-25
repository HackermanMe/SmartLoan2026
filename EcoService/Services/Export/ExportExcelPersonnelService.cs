/*
 * =============================================================================
 * SERVICE D'EXPORT EXCEL — AMORTISSEMENT PERSONNEL  (point d'entrée + palette)
 * =============================================================================
 * Fichiers du service :
 *   ExportExcelPersonnelService.cs          ← ce fichier : entrée + palette
 *   ExportExcelPersonnelService.Feuille1.cs ← ETAT DES PRETS
 *   ExportExcelPersonnelService.Feuille2.cs ← TABLEAU D'AMORTISSEMENT + calculs
 *   ExportExcelPersonnelService.Helpers.cs  ← helpers de style partagés
 *
 * Formules
 * ─────────
 *   r_TTC  = TauxAnnuel × (1 + TauxTAF/100) / 12 / 100
 *   r_HT   = TauxAnnuel / 12 / 100
 *   PMT    = M × r_TTC / (1 − (1+r_TTC)^−n)      n = NbreEcheances − NbreDifferes
 *
 *   Par ligne :
 *     Intérêts   = CapRestant × r_HT
 *     TAF        = Intérêts × TauxTAF / 100
 *     CapRmb     = PMT − Intérêts − TAF
 *
 *   Quotité = (PMT + ΣMensualités_existantes) / SalaireNet
 *
 * Précision : toutes les valeurs intermédiaires sont en pleine précision decimal.
 *             L'arrondi n'a lieu qu'à l'affichage (format Excel "# ##0").
 * =============================================================================
 */

using EcoService.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace EcoService.Services.Export
{
    public partial class ExportExcelPersonnelService
    {
        // ── Palette ──────────────────────────────────────────────────────────
        protected static readonly Color BleuEco     = Color.FromArgb(0, 130, 187);
        protected static readonly Color BleuFonce   = Color.FromArgb(0, 70, 127);
        protected static readonly Color BleuEntete  = Color.FromArgb(0, 32, 96);
        protected static readonly Color JauneSaisie = Color.FromArgb(255, 255, 0);
        protected static readonly Color OrangeTotal = Color.FromArgb(255, 192, 0);
        protected static readonly Color BleuClair   = Color.FromArgb(217, 235, 247);

        // ── Types de prêt ────────────────────────────────────────────────────
        public static readonly string[] TypesPret = {
            "Néant",
            "Prêt d'urgence",
            "Prêt personnel",
            "Prêt automobile (véhicule d'occasion)",
            "Prêt automobile (véhicule neuf)",
            "Achat de terrain",
            "Prêt de scolarisation",
            "Prêt immobilier",
            "Prêt aménagement habitat",
            "Prêt aménagement gros oeuvres"
        };

        // ════════════════════════════════════════════════════════════════════
        // POINT D'ENTRÉE
        // ════════════════════════════════════════════════════════════════════
        public byte[] Generer(EtatPretsPersonnelInput inp)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // ── Résolution des champs optionnels ─────────────────────────────
            decimal assuranceFixe   = inp.AssuranceFixe.GetValueOrDefault(0);
            decimal tauxAssurance   = inp.TauxAssurance.GetValueOrDefault(0);
            decimal tauxTAF         = inp.TauxTAF;
            decimal mensExistant    = inp.MensualitesPretExistant.GetValueOrDefault(0);
            decimal mensCredits1    = inp.MensualitesAutresCredits1.GetValueOrDefault(0);
            decimal mensCredits2    = inp.MensualitesAutresCredits2.GetValueOrDefault(0);
            decimal salaireNet      = inp.SalaireNetActuel.GetValueOrDefault(0);
            decimal cumulA          = inp.CumulPretPersonnelA.GetValueOrDefault(0);
            decimal fpeB            = inp.FPE_B.GetValueOrDefault(0);
            decimal cumulFPE        = inp.CumulPretPersonnelFPE.GetValueOrDefault(0);
            decimal montantExistant = inp.MontantPretExistant.GetValueOrDefault(0);
            decimal montantCredits1 = inp.MontantAutresCredits1.GetValueOrDefault(0);
            decimal montantCredits2 = inp.MontantAutresCredits2.GetValueOrDefault(0);

            // ── Calculs centraux (pleine précision) ──────────────────────────
            decimal rMoisTTC = inp.TauxAnnuel * (1m + tauxTAF / 100m) / 12m / 100m;
            decimal rMoisHT  = inp.TauxAnnuel / 12m / 100m;
            int     nEff     = Math.Max(1, inp.NbreEcheances - inp.NbreDifferes);

            decimal mensualite = PMT(inp.Montant, rMoisTTC, nEff);

            decimal assuranceMens = assuranceFixe > 0
                ? assuranceFixe
                : inp.Montant * tauxAssurance / 100m / 12m;   // pleine précision

            decimal mensAvecAss = mensualite + assuranceMens;

            decimal totalDeductions = mensualite + mensExistant + mensCredits1 + mensCredits2;

            decimal quotite = salaireNet > 0
                ? totalDeductions / salaireNet * 100m          // pleine précision — affiché avec 0.00%
                : 0m;

            decimal ratioFPE = fpeB > 0
                ? cumulFPE / fpeB * 100m                       // pleine précision
                : 0m;

            DateTime dateFin = inp.DateDebut.AddMonths(inp.NbreEcheances);

            // ── Amortissement ────────────────────────────────────────────────
            var lignes = GenererLignes(inp, mensualite, rMoisHT);

            decimal totInterets = 0, totTAF = 0, totCapital = 0, totMens = 0;
            foreach (var l in lignes)
            {
                totInterets += l.Interets;
                totTAF      += l.TAF;
                totCapital  += l.CapitalRembourse;
                totMens     += l.Mensualite;
            }
            decimal coutTotal = totInterets + totTAF + assuranceMens * inp.NbreEcheances;

            // ── Classeur ─────────────────────────────────────────────────────
            using (var pkg = new ExcelPackage())
            {
                var s1 = pkg.Workbook.Worksheets.Add("ETAT DES PRETS");
                var s2 = pkg.Workbook.Worksheets.Add("TABLEAU D'AMORTISSEMENT");

                FeuilleEtatPrets(s1, inp,
                    mensualite, totalDeductions, quotite, ratioFPE, dateFin,
                    mensExistant, mensCredits1, mensCredits2,
                    montantExistant, montantCredits1, montantCredits2,
                    salaireNet, cumulA, fpeB, cumulFPE);

                FeuilleTableau(s2, inp, lignes, mensualite, assuranceMens,
                    mensAvecAss, coutTotal, tauxAssurance, assuranceFixe,
                    totInterets, totTAF, totCapital, totMens);

                return pkg.GetAsByteArray();
            }
        }
    }
}
