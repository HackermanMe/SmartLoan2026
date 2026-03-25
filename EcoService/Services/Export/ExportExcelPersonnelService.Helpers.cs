/*
 * =============================================================================
 * HELPERS DE STYLE — partagés entre les deux feuilles
 * =============================================================================
 */

using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;
using System.Drawing;

namespace EcoService.Services.Export
{
    public partial class ExportExcelPersonnelService
    {
        // ── Fusion de cellules ────────────────────────────────────────────────
        private static void Merge(ExcelWorksheet ws, int r1, int c1, int r2, int c2)
            => ws.Cells[r1, c1, r2, c2].Merge = true;

        // ── Style multiple en une passe ───────────────────────────────────────
        private static void S(ExcelRange c,
            bool bold = false, float size = 0,
            ExcelHorizontalAlignment hAlign = ExcelHorizontalAlignment.Left,
            bool underline = false)
        {
            if (bold)     c.Style.Font.Bold      = true;
            if (size > 0) c.Style.Font.Size      = size;
            if (underline) c.Style.Font.UnderLine = true;
            c.Style.HorizontalAlignment = hAlign;
        }

        // ── Titre de section (fond bleu, texte blanc) ────────────────────────
        private static void TitreSection(ExcelWorksheet ws, int r, int c, string titre)
        {
            ws.Cells[r, c].Value = titre;
            ws.Cells[r, c].Style.Font.Bold = true;
            ws.Cells[r, c].Style.Font.Color.SetColor(Color.White);
            ws.Cells[r, c].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[r, c].Style.Fill.BackgroundColor.SetColor(BleuEco);
        }

        // ── Titre de bloc de signature (centré, gras, souligné) ──────────────
        private static void BlockTitre(ExcelWorksheet ws, ref int r, string titre)
        {
            ws.Cells[r, 1, r, 8].Merge = true;
            ws.Cells[r, 1].Value = titre;
            ws.Cells[r, 1].Style.Font.Bold = true;
            ws.Cells[r, 1].Style.Font.UnderLine = true;
            ws.Cells[r, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            r++;
        }

        // ── Ligne vide avec trait en bas (zone de saisie) ────────────────────
        private static void LigneVide(ExcelWorksheet ws, int r, int c1, int c2, double height = 18)
        {
            ws.Cells[r, c1, r, c2].Merge = true;
            ws.Cells[r, c1].Style.Border.Bottom.Style = ExcelBorderStyle.Hair;
            ws.Row(r).Height = height;
        }

        // ── Label + valeur texte ──────────────────────────────────────────────
        private static void LabelVal(ExcelWorksheet ws, int r, int cLabel, string label, int cVal, string val)
        {
            ws.Cells[r, cLabel].Value = label;
            ws.Cells[r, cLabel].Style.Font.Bold = true;
            ws.Cells[r, cVal].Value = val;
        }

        // ── Valeur numérique formatée "# ##0" (arrondi à l'affichage) ────────
        private static void SetNum(ExcelRange c, decimal val)
        {
            if (val != 0) c.Value = val;          // pleine précision stockée
            c.Style.Numberformat.Format = "#,##0";
            c.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        }

        // ── Valeur pourcentage "0.00%" ────────────────────────────────────────
        private static void SetPct(ExcelRange c, decimal val)
        {
            c.Value = val;
            c.Style.Numberformat.Format = "0.00%";
            c.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        // ── Liste déroulante (DataValidation) ────────────────────────────────
        // c1..c2 = colonnes de la cellule (fusionnée ou simple)
        // EPPlus 7 : le dropdown est affiché automatiquement pour les listes
        private static void AjouterDropdown(ExcelWorksheet ws, int r, int c1, int c2, string[] valeurs)
        {
            string adr = c1 == c2
                ? ws.Cells[r, c1].Address
                : ws.Cells[r, c1, r, c2].Address;

            var dv = ws.DataValidations.AddListValidation(adr);
            dv.AllowBlank       = true;
            dv.ShowErrorMessage = false;
            foreach (var v in valeurs) dv.Formula.Values.Add(v);
        }

        // ── Cellule jaune (zone de saisie numérique) ─────────────────────────
        private static void Jaune(ExcelRange c, decimal val)
        {
            c.Style.Fill.PatternType = ExcelFillStyle.Solid;
            c.Style.Fill.BackgroundColor.SetColor(JauneSaisie);
            if (val != 0) { c.Value = val; c.Style.Numberformat.Format = "#,##0"; }
            c.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        }
    }
}
