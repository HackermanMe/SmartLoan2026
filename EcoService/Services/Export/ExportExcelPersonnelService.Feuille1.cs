/*
 * =============================================================================
 * FEUILLE 1 — ETAT DES PRETS  (portrait A4, 1 page)
 * =============================================================================
 * Grille 10 colonnes (A–J) pour reproduire fidèlement le formulaire original.
 * =============================================================================
 */

using EcoService.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Drawing;

namespace EcoService.Services.Export
{
    public partial class ExportExcelPersonnelService
    {
        private void FeuilleEtatPrets(
            ExcelWorksheet ws,
            EtatPretsPersonnelInput inp,
            decimal mensualite,
            decimal totalDeductions,
            decimal quotite,
            decimal ratioFPE,
            DateTime dateFin,
            decimal mensExistant,
            decimal mensCredits1,
            decimal mensCredits2,
            decimal montantExistant,
            decimal montantCredits1,
            decimal montantCredits2,
            decimal salaireNet,
            decimal cumulA,
            decimal fpeB,
            decimal cumulFPE)
        {
            ws.Cells.Style.Font.Name = "Calibri";
            ws.Cells.Style.Font.Size = 10;
            ws.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            // ── Largeurs colonnes A(1)–J(10) ────────────────────────────────
            double[] cw = { 13, 9, 10, 11, 9, 9, 7, 10, 7, 14 };
            for (int c = 1; c <= 10; c++) ws.Column(c).Width = cw[c - 1];

            int r = 1;

            // ═══════════════════════════════════════════════════════════════════
            // TITRE
            // ═══════════════════════════════════════════════════════════════════
            Merge(ws, r, 2, r, 8);
            ws.Cells[r, 2].Value = "ETAT  DES  PRETS";
            S(ws.Cells[r, 2], bold: true, size: 14, hAlign: ExcelHorizontalAlignment.Center);
            ws.Cells[r, 2].Style.Font.Color.SetColor(Color.Black);
            ws.Cells[r, 10].Value = "21";
            S(ws.Cells[r, 10], size: 11);
            ws.Row(r).Height = 22; r++;

            // Sous-titre gauche + quotité max droite
            Merge(ws, r, 1, r, 5);
            ws.Cells[r, 1].Value = "le service du Personnel et";
            Merge(ws, r, 9, r, 10);
            ws.Cells[r, 9].Value = "35%";
            ws.Cells[r, 9].Style.Font.Bold = true;
            r++;

            Merge(ws, r, 3, r, 8);
            ws.Cells[r, 3].Value = "à joindre à la demande de prêt)";
            S(ws.Cells[r, 3], hAlign: ExcelHorizontalAlignment.Center);
            r++; r++; // ligne vide

            // ═══════════════════════════════════════════════════════════════════
            // IDENTITÉ DU DEMANDEUR
            // ═══════════════════════════════════════════════════════════════════

            // Nom du demandeur
            Merge(ws, r, 1, r, 2);
            ws.Cells[r, 1].Value = "Nom du demandeur :";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Merge(ws, r, 3, r, 5);
            ws.Cells[r, 3].Value = inp.NomDemandeur;
            ws.Cells[r, 3].Style.Font.Bold = true;
            ws.Cells[r, 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            Merge(ws, r, 6, r, 7);
            ws.Cells[r, 6].Value = "Classe/Catégorie :";
            Merge(ws, r, 8, r, 10);
            ws.Cells[r, 8].Value = inp.ClasseCategorie;
            ws.Cells[r, 8].Style.Font.Bold = true;
            ws.Cells[r, 8].Style.Font.Color.SetColor(BleuFonce);
            ws.Cells[r, 8].Style.Font.UnderLine = true;
            r++;

            r++; // espacement

            // Département
            ws.Cells[r, 1].Value = "Département :";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Merge(ws, r, 2, r, 4);
            ws.Cells[r, 2].Value = inp.Departement;
            ws.Cells[r, 2].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            Merge(ws, r, 6, r, 9);
            ws.Cells[r, 6].Value = "Nbre d'années dans la banque :";
            ws.Cells[r, 10].Value = inp.NbreAnnees;
            ws.Cells[r, 10].Style.Font.Bold = true;
            ws.Cells[r, 10].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            r++;

            r++; // espacement

            // Fonction
            ws.Cells[r, 1].Value = "Fonction :";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Merge(ws, r, 2, r, 5);
            ws.Cells[r, 2].Value = inp.Fonction;
            ws.Cells[r, 2].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            r++;

            r++; // espacement avant section

            // ═══════════════════════════════════════════════════════════════════
            // PRET SOLLICITE
            // ═══════════════════════════════════════════════════════════════════

            // En-tête bleu + dropdown + numéro de compte
            Merge(ws, r, 1, r, 2);
            TitreSection(ws, r, 1, "PRET SOLLICITE");
            ws.Cells[r, 1, r, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            Merge(ws, r, 3, r, 5);
            ws.Cells[r, 3].Value = inp.TypePret;
            ws.Cells[r, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            AjouterDropdown(ws, r, 3, 5, TypesPret);
            Merge(ws, r, 7, r, 8);
            ws.Cells[r, 7].Value = "Numéro de Compte";
            ws.Cells[r, 7].Style.Font.Bold = true;
            Merge(ws, r, 9, r, 10);
            ws.Cells[r, 9].Value = inp.NumeroCompte;
            ws.Cells[r, 9].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            r++;

            // Sous-label "Taxe"
            ws.Cells[r, 8].Value = "Taxe";
            r++;

            // Sous-titres "Nbre de" et "Nbre" au-dessus des valeurs
            ws.Cells[r, 4].Value = "Nbre de";
            S(ws.Cells[r, 4], size: 8);
            ws.Cells[r, 6].Value = "Nbre";
            S(ws.Cells[r, 6], size: 8);
            r++;

            // Montant | valeur | différés | 0 | d'échéances | 60 | Taux d'intérêts | 3,00%
            ws.Cells[r, 1].Value = "Montant";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Merge(ws, r, 2, r, 3);
            Jaune(ws.Cells[r, 2], inp.Montant);
            ws.Cells[r, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws.Cells[r, 4].Value = "différés";
            S(ws.Cells[r, 4], size: 8);
            ws.Cells[r, 5].Value = (decimal)inp.NbreDifferes;
            ws.Cells[r, 5].Style.Numberformat.Format = "0";
            ws.Cells[r, 5].Style.Font.Bold = true;
            ws.Cells[r, 6].Value = "d'échéances";
            S(ws.Cells[r, 6], size: 8);
            ws.Cells[r, 7].Value = (decimal)inp.NbreEcheances;
            ws.Cells[r, 7].Style.Numberformat.Format = "0";
            ws.Cells[r, 7].Style.Font.Bold = true;
            Merge(ws, r, 8, r, 9);
            ws.Cells[r, 8].Value = "Taux d'intérêts";
            ws.Cells[r, 8].Style.Font.Bold = true;
            ws.Cells[r, 10].Value = (double)(inp.TauxAnnuel / 100m);
            ws.Cells[r, 10].Style.Numberformat.Format = "0.00%";
            r++;

            // Mensualités | valeur | Intérêts/Taxe | Total/mois (1) : | valeur
            ws.Cells[r, 1].Value = "Mensualités";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Jaune(ws.Cells[r, 2], mensualite);
            ws.Cells[r, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            Merge(ws, r, 3, r, 4);
            ws.Cells[r, 3].Value = "Intérêts/Taxe";
            ws.Cells[r, 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            Merge(ws, r, 7, r, 8);
            ws.Cells[r, 7].Value = "Total/mois (1) :";
            ws.Cells[r, 7].Style.Font.Bold = true;
            Merge(ws, r, 9, r, 10);
            Jaune(ws.Cells[r, 9], mensualite);
            ws.Cells[r, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            r++;

            r++; // espacement

            // Dates de remboursement
            Merge(ws, r, 1, r, 3);
            ws.Cells[r, 1].Value = "Date début remboursement";
            ws.Cells[r, 1].Style.Font.Bold = true;
            ws.Cells[r, 1].Style.Font.Color.SetColor(BleuFonce);
            Merge(ws, r, 4, r, 5);
            ws.Cells[r, 4].Value = inp.DateDebut;
            ws.Cells[r, 4].Style.Numberformat.Format = "dd/MM/yyyy";
            ws.Cells[r, 4].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            Merge(ws, r, 6, r, 7);
            ws.Cells[r, 6].Value = "Date fin remboursement";
            ws.Cells[r, 6].Style.Font.Bold = true;
            Merge(ws, r, 8, r, 10);
            ws.Cells[r, 8].Value = dateFin;
            ws.Cells[r, 8].Style.Numberformat.Format = "dd/MM/yyyy";
            ws.Cells[r, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            r++;

            // ═══════════════════════════════════════════════════════════════════
            // PRETS EXISTANTS
            // ═══════════════════════════════════════════════════════════════════
            Merge(ws, r, 1, r, 2);
            TitreSection(ws, r, 1, "PRETS EXISTANTS");
            ws.Cells[r, 1, r, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            Merge(ws, r, 3, r, 5);
            ws.Cells[r, 3].Value = inp.TypePretExistant;
            ws.Cells[r, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            AjouterDropdown(ws, r, 3, 5, TypesPret);
            r++;

            r++; // espacement

            // Montant | jaune | Mensualités (Intérêts) (2) : | jaune
            ws.Cells[r, 1].Value = "Montant";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Merge(ws, r, 2, r, 3);
            Jaune(ws.Cells[r, 2], montantExistant);
            ws.Cells[r, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            Merge(ws, r, 5, r, 8);
            ws.Cells[r, 5].Value = "Mensualités (Intérêts)  (2) :";
            Merge(ws, r, 9, r, 10);
            Jaune(ws.Cells[r, 9], mensExistant);
            ws.Cells[r, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            r++;

            r++; // espacement

            // ═══════════════════════════════════════════════════════════════════
            // AUTRES CREDITS 1
            // ═══════════════════════════════════════════════════════════════════
            Merge(ws, r, 1, r, 2);
            TitreSection(ws, r, 1, "AUTRES CREDITS");
            ws.Cells[r, 1, r, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            Merge(ws, r, 3, r, 5);
            ws.Cells[r, 3].Value = inp.TypeAutresCredits1;
            ws.Cells[r, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            AjouterDropdown(ws, r, 3, 5, TypesPret);
            r++;

            r++; // espacement

            ws.Cells[r, 1].Value = "Montant";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Merge(ws, r, 2, r, 3);
            Jaune(ws.Cells[r, 2], montantCredits1);
            ws.Cells[r, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            Merge(ws, r, 5, r, 8);
            ws.Cells[r, 5].Value = "Mensualités (Intérêts)  (3) :";
            Merge(ws, r, 9, r, 10);
            Jaune(ws.Cells[r, 9], mensCredits1);
            ws.Cells[r, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            r++;

            r++; // espacement

            // ═══════════════════════════════════════════════════════════════════
            // AUTRES CREDITS 2
            // ═══════════════════════════════════════════════════════════════════
            Merge(ws, r, 1, r, 2);
            TitreSection(ws, r, 1, "AUTRES CREDITS");
            ws.Cells[r, 1, r, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            Merge(ws, r, 3, r, 5);
            ws.Cells[r, 3].Value = inp.TypeAutresCredits2;
            ws.Cells[r, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            AjouterDropdown(ws, r, 3, 5, TypesPret);
            r++;

            r++; // espacement

            ws.Cells[r, 1].Value = "Montant";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Merge(ws, r, 2, r, 3);
            Jaune(ws.Cells[r, 2], montantCredits2);
            ws.Cells[r, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            Merge(ws, r, 5, r, 8);
            ws.Cells[r, 5].Value = "Mensualités (Intérêts)  (3) :";
            Merge(ws, r, 9, r, 10);
            Jaune(ws.Cells[r, 9], mensCredits2);
            ws.Cells[r, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            r++;

            r++; // espacement

            // ═══════════════════════════════════════════════════════════════════
            // TOTAL DES DÉDUCTIONS
            // ═══════════════════════════════════════════════════════════════════
            Merge(ws, r, 1, r, 5);
            ws.Cells[r, 1].Value = "Total des déductions par mois (1+2+3+4) :";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Merge(ws, r, 6, r, 7);
            SetNum(ws.Cells[r, 6], totalDeductions);
            ws.Cells[r, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws.Cells[r, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            r++;

            r++; // espacement

            // Salaire Net actuel | valeur | Ratio : | valeur%
            Merge(ws, r, 1, r, 2);
            ws.Cells[r, 1].Value = "Salaire Net actuel";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Merge(ws, r, 3, r, 4);
            SetNum(ws.Cells[r, 3], salaireNet);
            ws.Cells[r, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws.Cells[r, 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            ws.Cells[r, 7].Value = "Ratio :";
            ws.Cells[r, 7].Style.Font.Bold = true;
            Merge(ws, r, 8, r, 9);
            ws.Cells[r, 8].Value = (double)(quotite / 100m);
            ws.Cells[r, 8].Style.Numberformat.Format = "0.00%";
            ws.Cells[r, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            r++;

            r++; // espacement

            // ═══════════════════════════════════════════════════════════════════
            // SITUATION GLOBALE DES CREDITS
            // ═══════════════════════════════════════════════════════════════════
            Merge(ws, r, 3, r, 8);
            ws.Cells[r, 3].Value = "SITUATION GLOBALE DES CREDITS";
            S(ws.Cells[r, 3], bold: true, underline: true, hAlign: ExcelHorizontalAlignment.Center);
            ws.Cells[r, 3, r, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            r++;

            r++; // espacement

            // Cumul Prêt Personnel (A) | valeur | FPE (B) | valeur
            Merge(ws, r, 1, r, 2);
            ws.Cells[r, 1].Value = "Cumul Prêt Personnel (A)";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Merge(ws, r, 3, r, 5);
            SetNum(ws.Cells[r, 3], cumulA);
            ws.Cells[r, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws.Cells[r, 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            Merge(ws, r, 7, r, 8);
            ws.Cells[r, 7].Value = "FPE (B)";
            ws.Cells[r, 7].Style.Font.Bold = true;
            Merge(ws, r, 9, r, 10);
            SetNum(ws.Cells[r, 9], fpeB);
            ws.Cells[r, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws.Cells[r, 9].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            r++;

            r++; // espacement

            // Cumul Prêt Personnel / FPE | Ratio | valeur%
            Merge(ws, r, 1, r, 2);
            ws.Cells[r, 1].Value = "Cumul Prêt Personnel / FPE";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Merge(ws, r, 7, r, 8);
            ws.Cells[r, 7].Value = "Ratio:";
            ws.Cells[r, 7].Style.Font.Bold = true;
            Merge(ws, r, 9, r, 10);
            ws.Cells[r, 9].Value = (double)(ratioFPE / 100m);
            ws.Cells[r, 9].Style.Numberformat.Format = "0.00%";
            ws.Cells[r, 9].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            r++;

            // Valeur cumulFPE | Date du jour
            Merge(ws, r, 2, r, 4);
            SetNum(ws.Cells[r, 2], cumulFPE);
            ws.Cells[r, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws.Cells[r, 2].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            ws.Cells[r, 8].Value = "Date :";
            ws.Cells[r, 8].Style.Font.Bold = true;
            Merge(ws, r, 9, r, 10);
            ws.Cells[r, 9].Value = DateTime.Today;
            ws.Cells[r, 9].Style.Numberformat.Format = "dd-MMM-yy";
            ws.Cells[r, 9].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            r++;

            r++; // espacement

            // ═══════════════════════════════════════════════════════════════════
            // OBSERVATIONS / AVIS DU CHEF DE DEPARTEMENT
            // ═══════════════════════════════════════════════════════════════════
            ws.Cells[r, 1, r, 10].Style.Border.Top.Style = ExcelBorderStyle.Medium;
            r++;

            Merge(ws, r, 1, r, 10);
            ws.Cells[r, 1].Value = "OBSERVATIONS/AVIS DU CHEF DE DEPARTEMENT";
            S(ws.Cells[r, 1], bold: true, underline: true, hAlign: ExcelHorizontalAlignment.Center);
            r++;

            r++; // espacement
            LigneVide(ws, r, 1, 10); // ligne 1 pour écrire
            r++;

            r++; // espacement
            LigneVide(ws, r, 1, 10); // ligne 2 pour écrire
            r++;

            r++; // espacement

            // Date : ___  |  Nom & Signature : ___
            ws.Cells[r, 1].Value = "Date :";
            ws.Cells[r, 1].Style.Font.Bold = true;
            LigneVide(ws, r, 2, 3);
            Merge(ws, r, 5, r, 6);
            ws.Cells[r, 5].Value = "Nom & Signature :";
            ws.Cells[r, 5].Style.Font.Bold = true;
            LigneVide(ws, r, 7, 10);
            r++;

            r++; // espacement

            // ═══════════════════════════════════════════════════════════════════
            // OBSERVATIONS / AVIS DU COMITE DE PRET
            // ═══════════════════════════════════════════════════════════════════
            Merge(ws, r, 1, r, 10);
            ws.Cells[r, 1].Value = "OBSERVATIONS/AVIS DU COMITE DE PRET";
            S(ws.Cells[r, 1], bold: true, underline: true, hAlign: ExcelHorizontalAlignment.Center);
            r++;

            r++; // espacement

            // En-têtes numérotées 1 | 2 | 3 avec bordures
            Merge(ws, r, 1, r, 3);  ws.Cells[r, 1].Value = "1";
            Merge(ws, r, 4, r, 7);  ws.Cells[r, 4].Value = "2";
            Merge(ws, r, 8, r, 10); ws.Cells[r, 8].Value = "3";
            ws.Cells[r, 1, r, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            ws.Cells[r, 4, r, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            ws.Cells[r, 8, r, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            r++;

            // Lignes vides avec bordures latérales pour le comité
            for (int i = 0; i < 3; i++)
            {
                Merge(ws, r, 1, r, 3);
                Merge(ws, r, 4, r, 7);
                Merge(ws, r, 8, r, 10);
                ws.Cells[r, 1, r, 3].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws.Cells[r, 3].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws.Cells[r, 4, r, 7].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws.Cells[r, 7].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws.Cells[r, 8, r, 10].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws.Cells[r, 10].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws.Row(r).Height = 16;
                r++;
            }

            r++; // espacement

            // Visa + noms des membres du comité
            ws.Cells[r, 1].Value = "Visa";
            ws.Cells[r, 1].Style.Font.Bold = true;
            Merge(ws, r, 2, r, 3);
            ws.Cells[r, 2].Value = "";
            ws.Cells[r, 2].Style.Font.Bold = true;

            ws.Cells[r, 4].Value = "Visa";
            ws.Cells[r, 4].Style.Font.Bold = true;
            Merge(ws, r, 5, r, 7);
            ws.Cells[r, 5].Value = "";
            ws.Cells[r, 5].Style.Font.Bold = true;

            ws.Cells[r, 8].Value = "Visa";
            ws.Cells[r, 8].Style.Font.Bold = true;
            Merge(ws, r, 9, r, 10);
            ws.Cells[r, 9].Value = "";
            ws.Cells[r, 9].Style.Font.Bold = true;
            r++;

            // Date pour chaque membre
            ws.Cells[r, 1].Value = "Date";
            LigneVide(ws, r, 2, 3);
            ws.Cells[r, 4].Value = "Date";
            LigneVide(ws, r, 5, 7);
            ws.Cells[r, 8].Value = "Date";
            LigneVide(ws, r, 9, 10);
            r++;

            r++; // espacement

            // ═══════════════════════════════════════════════════════════════════
            // DIRECTION GENERALE
            // ═══════════════════════════════════════════════════════════════════
            Merge(ws, r, 4, r, 5);
            ws.Cells[r, 4].Value = "Visa";
            ws.Cells[r, 4].Style.Font.Bold = true;
            Merge(ws, r, 6, r, 8);
            ws.Cells[r, 6].Value = "DIRECTION GENERALE";
            S(ws.Cells[r, 6], bold: true, underline: true, hAlign: ExcelHorizontalAlignment.Center);
            r++;

            Merge(ws, r, 4, r, 5);
            ws.Cells[r, 4].Value = "Date";
            LigneVide(ws, r, 6, 8);
            r++;

            // ── Impression portrait A4, 1 page ──────────────────────────────
            ws.PrinterSettings.PaperSize    = ePaperSize.A4;
            ws.PrinterSettings.Orientation  = eOrientation.Portrait;
            ws.PrinterSettings.FitToPage    = true;
            ws.PrinterSettings.FitToWidth   = 1;
            ws.PrinterSettings.FitToHeight  = 1;
            ws.PrinterSettings.TopMargin    = 0.4m;
            ws.PrinterSettings.BottomMargin = 0.4m;
            ws.PrinterSettings.LeftMargin   = 0.5m;
            ws.PrinterSettings.RightMargin  = 0.5m;
        }
    }
}
