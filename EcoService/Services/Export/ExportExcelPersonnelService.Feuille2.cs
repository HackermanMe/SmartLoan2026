/*
 * =============================================================================
 * FEUILLE 2 — TABLEAU D'AMORTISSEMENT  (paysage A4, 1 page forcée)
 * =============================================================================
 * Contient également :
 *   - GenererLignes()  : calcul des lignes en pleine précision decimal
 *   - PMT()            : formule d'annuité constante
 *
 * Précision : aucun Math.Round dans les calculs intermédiaires.
 *             Le format Excel "#,##0" arrondit à l'affichage, exactement
 *             comme Math.round() côté JavaScript.
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
        // ════════════════════════════════════════════════════════════════════
        // FEUILLE 2
        // ════════════════════════════════════════════════════════════════════
        private void FeuilleTableau(
            ExcelWorksheet ws,
            EtatPretsPersonnelInput inp,
            List<LigneAmortissement> lignes,
            decimal mensualite,
            decimal assuranceMens,
            decimal mensAvecAss,
            decimal coutTotal,
            decimal tauxAssurance,
            decimal assuranceFixe,
            decimal totInterets,
            decimal totTAF,
            decimal totCapital,
            decimal totMens)
        {
            ws.Cells.Style.Font.Name = "Calibri";
            ws.Cells.Style.Font.Size = 9;

            // Largeurs colonnes A–G
            double[] w = { 7, 15, 13, 11, 13, 13, 16 };
            for (int c = 1; c <= 7; c++) ws.Column(c).Width = w[c - 1];

            int r = 1;

            // ── Mensualité principale ─────────────────────────────────────────
            ws.Cells[r, 1].Value = "LA MENSUALITE EST DE :";
            ws.Cells[r, 1].Style.Font.Bold = true;
            ws.Cells[r, 2].Value = mensualite;
            ws.Cells[r, 2].Style.Numberformat.Format = "#,##0";
            ws.Cells[r, 2].Style.Font.Bold = true; r++; r++;

            ws.Cells[r, 1].Value = "NOM"; ws.Cells[r, 1].Style.Font.Bold = true;
            ws.Cells[r, 2].Value = inp.NomDemandeur; r++; r++;

            // ── Bloc résumé (paramètres du prêt) ─────────────────────────────
            void RL(string lbl, decimal val, string fmt)
            {
                ws.Cells[r, 1].Value = lbl;
                ws.Cells[r, 1].Style.Font.Bold = true;
                ws.Cells[r, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[r, 1].Style.Fill.BackgroundColor.SetColor(BleuClair);
                ws.Cells[r, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                ws.Cells[r, 2].Value = val;
                ws.Cells[r, 2].Style.Numberformat.Format = fmt;
                ws.Cells[r, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws.Cells[r, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                r++;
            }
            RL("Montant du prêt",     inp.Montant,                                "#,##0");
            RL("Taux",               inp.TauxAnnuel / 100m,                      "0.00%");
            RL("Nb Mensualité",      (decimal)inp.NbreEcheances,                 "0");
            RL("Mensualité",         mensualite,                                  "#,##0");
            RL("Assurance %",        tauxAssurance / 100m,                       "0.0000%");
            RL("Assurance Fixe",     assuranceFixe,                              "#,##0.00");
            RL("Mensualité+Assurance", mensAvecAss,                              "#,##0");
            RL("Cout total du crédit", coutTotal,                                "#,##0");

            // Date de début
            ws.Cells[r, 1].Value = "Date de début";
            ws.Cells[r, 1].Style.Font.Bold = true;
            ws.Cells[r, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[r, 1].Style.Fill.BackgroundColor.SetColor(BleuClair);
            ws.Cells[r, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            ws.Cells[r, 2].Value = inp.DateDebut;
            ws.Cells[r, 2].Style.Numberformat.Format = "dd/mm/yyyy";
            ws.Cells[r, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin); r++; r++;

            // ── Grand titre ───────────────────────────────────────────────────
            Merge(ws, r, 1, r, 7);
            ws.Cells[r, 1].Value = "TABLEAU D'AMORTISSEMENT";
            S(ws.Cells[r, 1], bold: true, size: 16, hAlign: ExcelHorizontalAlignment.Center);
            ws.Row(r).Height = 26; r++; r++;

            // ── En-têtes colonnes ─────────────────────────────────────────────
            string[] hdrs = { "NBRE", "MONTANT", "INTERETS", "TAF", "CAPITAL REMB", "MENSUALITE", "CAPITAL RESTANT DU" };
            for (int c = 1; c <= 7; c++)
            {
                ws.Cells[r, c].Value = hdrs[c - 1];
                ws.Cells[r, c].Style.Font.Bold = true;
                ws.Cells[r, c].Style.Font.Color.SetColor(Color.White);
                ws.Cells[r, c].Style.Font.Size = 9;
                ws.Cells[r, c].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[r, c].Style.Fill.BackgroundColor.SetColor(BleuEntete);
                ws.Cells[r, c].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[r, c].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.White);
                ws.Cells[r, c].Style.WrapText = true;
            }
            ws.Row(r).Height = 20; r++;

            // ── Lignes d'amortissement (ordre croissant : échéance 1 en premier) ──
            bool alt = false;
            foreach (var l in lignes)
            {
                var bg = alt ? Color.FromArgb(235, 244, 252) : Color.White;
                ws.Row(r).Height = 13;

                void Cell(int col, object val, bool bold = false)
                {
                    var cell = ws.Cells[r, col];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(bg);
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Hair);
                    cell.Style.Font.Bold = bold;
                    if (val is decimal d)
                    {
                        cell.Value = d;                          // pleine précision
                        cell.Style.Numberformat.Format = "#,##0";  // arrondi à l'affichage
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }
                    else
                    {
                        cell.Value = val;
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                }

                Cell(1, l.NumeroEcheance);
                ws.Cells[r, 1].Style.Font.Color.SetColor(BleuFonce);
                Cell(2, l.CapitalRestantDebut);
                Cell(3, l.Interets);
                Cell(4, l.TAF);
                Cell(5, l.CapitalRembourse, bold: true);
                Cell(6, l.Mensualite);
                Cell(7, l.CapitalRestantFin);

                alt = !alt; r++;
            }

            // ── Ligne TOTAL ────────────────────────────────────────────────────
            ws.Cells[r, 1].Value = "TOTAL";
            ws.Cells[r, 1].Style.Font.Bold = true;
            ws.Cells[r, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            decimal[] totVals = { 0, 0, totInterets, totTAF, totCapital, totMens, 0 };
            for (int c = 1; c <= 7; c++)
            {
                var cell = ws.Cells[r, c];
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(OrangeTotal);
                cell.Style.Font.Bold = true;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                if (totVals[c - 1] != 0)
                {
                    cell.Value = totVals[c - 1];               // pleine précision
                    cell.Style.Numberformat.Format = "#,##0";
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                }
            }

            // ── Impression paysage A4, 1 page ─────────────────────────────────
            ws.PrinterSettings.PaperSize    = ePaperSize.A4;
            ws.PrinterSettings.Orientation  = eOrientation.Landscape;
            ws.PrinterSettings.FitToPage    = true;
            ws.PrinterSettings.FitToWidth   = 1;
            ws.PrinterSettings.FitToHeight  = 1;
            ws.PrinterSettings.TopMargin    = 0.4m;
            ws.PrinterSettings.BottomMargin = 0.4m;
            ws.PrinterSettings.LeftMargin   = 0.5m;
            ws.PrinterSettings.RightMargin  = 0.5m;
        }

        // ════════════════════════════════════════════════════════════════════
        // CALCUL DES LIGNES (pleine précision decimal, sans Math.Round)
        // Algorithme identique au JavaScript côté interface
        // ════════════════════════════════════════════════════════════════════
        private List<LigneAmortissement> GenererLignes(
            EtatPretsPersonnelInput inp,
            decimal mensualite,
            decimal rMoisHT)
        {
            var lignes = new List<LigneAmortissement>();
            decimal cap = inp.Montant;                       // pleine précision

            for (int i = 1; i <= inp.NbreEcheances; i++)
            {
                bool differe = i <= inp.NbreDifferes;

                decimal interets = cap * rMoisHT;            // pleine précision
                decimal taf      = interets * (inp.TauxTAF / 100m);  // pleine précision
                decimal capRmb;

                if (differe)
                {
                    capRmb = 0m;
                }
                else if (i == inp.NbreEcheances)
                {
                    capRmb = cap;  // solde exact sur la dernière échéance
                }
                else
                {
                    capRmb = Math.Max(0m, mensualite - interets - taf);  // pleine précision
                }

                decimal mensEff  = differe ? (interets + taf) : mensualite;
                decimal capFin   = cap - capRmb;

                lignes.Add(new LigneAmortissement
                {
                    NumeroEcheance      = i,
                    CapitalRestantDebut = cap,
                    Interets            = interets,
                    TAF                 = taf,
                    CapitalRembourse    = capRmb,
                    Mensualite          = mensEff,
                    CapitalRestantFin   = capFin,
                    EstEnDiffere        = differe
                });

                cap = capFin;
                if (cap < 0m) cap = 0m;
            }
            return lignes;
        }

        // ════════════════════════════════════════════════════════════════════
        // PMT = M × r / (1 − (1+r)^−n)   — pleine précision, pas d'arrondi
        // ════════════════════════════════════════════════════════════════════
        private static decimal PMT(decimal montant, decimal rMois, int n)
        {
            if (rMois == 0m) return montant / n;
            double r   = (double)rMois;
            double pmt = (double)montant * r / (1.0 - Math.Pow(1.0 + r, -n));
            return (decimal)pmt;
        }
    }
}
