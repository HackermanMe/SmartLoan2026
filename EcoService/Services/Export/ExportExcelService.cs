
/*
 * =============================================================================
 * SERVICE D'EXPORT EXCEL
 * =============================================================================
 * Genere un fichier Excel avec:
 * - Feuille Resume du pret
 * - Feuille Tableau d'amortissement
 *
 * Utilise la bibliotheque ClosedXML (MIT License)
 * =============================================================================
 */
using EcoService.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.IO;
using System.Drawing;

namespace EcoService.Services.Export
{
    public interface IExportExcelService
    {
        byte[] ExportToExcel(AmortissementResult result, AmortissementInput input);
    }

    public class ExportExcelService : IExportExcelService
    {
        public byte[] ExportToExcel(AmortissementResult result, AmortissementInput input)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var resumeSheet = package.Workbook.Worksheets.Add("Resume");
                ConfigurerFeuilleResume(resumeSheet, result, input);

                var tableauSheet = package.Workbook.Worksheets.Add("Tableau Amortissement");
                ConfigurerFeuilleTableau(tableauSheet, result);

                return package.GetAsByteArray();
            }
        }

        private void ConfigurerFeuilleResume(ExcelWorksheet sheet, AmortissementResult result, AmortissementInput input)
        {
            int row = 1;

            sheet.Cells[row, 1].Value = "RESUME DU PRET";
            sheet.Cells[row, 1].Style.Font.Bold = true;
            sheet.Cells[row, 1].Style.Font.Size = 16;
            sheet.Cells[row, 1, row, 2].Merge = true;
            row += 2;

            sheet.Cells[row, 1].Value = "Montant du pret";
            sheet.Cells[row, 2].Value = result.MontantPret;
            row++;

            sheet.Cells[row, 1].Value = "Taux annuel";
            sheet.Cells[row, 2].Value = result.TauxAnnuel;
            row++;

            sheet.Cells[row, 1].Value = "Nombre d'echeances";
            sheet.Cells[row, 2].Value = result.NombreEcheances;
            row++;

            sheet.Cells[row, 1].Value = "Mensualite";
            sheet.Cells[row, 2].Value = result.Mensualite;
            row++;

            sheet.Cells[row, 1].Value = "Total interets";
            sheet.Cells[row, 2].Value = result.TotalInterets;
            row++;

            sheet.Cells[row, 1].Value = "Date debut";
            sheet.Cells[row, 2].Value = result.DateDebut.ToShortDateString();
            row++;

            sheet.Cells[row, 1].Value = "Date fin";
            sheet.Cells[row, 2].Value = result.DateFin.ToShortDateString();

            sheet.Column(1).Width = 25;
            sheet.Column(2).Width = 20;
        }

        private void ConfigurerFeuilleTableau(ExcelWorksheet sheet, AmortissementResult result)
        {
            string[] headers = {
                "N°",
                "Date",
                "Capital debut",
                "Interets",
                "TAF",
                "Capital rembourse",
                "Mensualite",
                "Capital restant"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = headers[i];
                sheet.Cells[1, i + 1].Style.Font.Bold = true;
                sheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            int row = 2;

            foreach (var ligne in result.Lignes)
            {
                sheet.Cells[row, 1].Value = ligne.NumeroEcheance;
                sheet.Cells[row, 2].Value = ligne.DateEcheance.ToShortDateString();
                sheet.Cells[row, 3].Value = ligne.CapitalRestantDebut;
                sheet.Cells[row, 4].Value = ligne.Interets;
                sheet.Cells[row, 5].Value = ligne.TAF;
                sheet.Cells[row, 6].Value = ligne.CapitalRembourse;
                sheet.Cells[row, 7].Value = ligne.Mensualite;
                sheet.Cells[row, 8].Value = ligne.CapitalRestantFin;

                row++;
            }

            sheet.Cells.AutoFitColumns();
            sheet.View.FreezePanes(2, 1);
        }
    }
}