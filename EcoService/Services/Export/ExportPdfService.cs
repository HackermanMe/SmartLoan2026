/*
 * =============================================================================
 * SERVICE D'EXPORT PDF
 * =============================================================================
 * Genere un fichier PDF avec:
 * - En-tete avec titre et date
 * - Resume du pret
 * - Tableau d'amortissement avec bordures
 * - Pied de page avec pagination
 *
 * Utilise la bibliotheque iText7
 * =============================================================================
 */

using EcoService.Models;
using iText.Commons.Actions;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

//using System.Windows.Documents;

namespace EcoService.Services.Export
{
    /// <summary>
    /// Interface pour l'export PDF
    /// </summary>
    public interface IExportPdfService
    {
        byte[] ExportToPdf(AmortissementResult result, AmortissementInput input);
    }

    /// <summary>
    /// Implementation de l'export PDF avec iText7
    /// </summary>
    public class ExportPdfService : IExportPdfService
    {
        // Couleurs du theme
        private static readonly DeviceRgb CouleurPrimaire = new DeviceRgb(0, 120, 212);
        private static readonly DeviceRgb CouleurHeader = new DeviceRgb(230, 242, 255);
        private static readonly DeviceRgb CouleurDiffere = new DeviceRgb(255, 243, 224);
        private static readonly DeviceRgb CouleurBlanc = new DeviceRgb(255, 255, 255);
        private static readonly DeviceRgb CouleurNoir = new DeviceRgb(0, 0, 0);

        public byte[] ExportToPdf(AmortissementResult result, AmortissementInput input)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdfDocument = new PdfDocument(writer);

            // Police par defaut
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // Ajouter le gestionnaire d'evenements pour la pagination
            //pdfDocument.AddEventHandler(PdfDocumentEvent.END_PAGE, new FooterEventHandler(font));

            // Page en mode paysage
            var pageSize = PageSize.A4.Rotate();
            using var document = new Document(pdfDocument, pageSize);
            document.SetMargins(30, 30, 50, 30); // Marge bas plus grande pour le footer
            document.SetFont(font);
            document.SetFontSize(ExportStyleHelper.FONT_SIZE_PETIT);

            // En-tete
            ComposeHeader(document, result, fontBold);

            // Resume
            ComposeResume(document, result, input, font, fontBold);

            // Tableau
            ComposeTableau(document, result, font, fontBold);

            document.Close();
            return memoryStream.ToArray();
        }

        private void ComposeHeader(Document document, AmortissementResult result, PdfFont fontBold)
        {
            // Titre principal
            var titre = new Paragraph("TABLEAU D'AMORTISSEMENT")
                .SetFont(fontBold)
                .SetFontSize(ExportStyleHelper.FONT_SIZE_TITRE)
                .SetFontColor(CouleurPrimaire);
            document.Add(titre);

            // Info client et date
            var infoTable = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 }))
                .UseAllAvailableWidth();

            var clientInfo = "";
            if (result.Client != null)
            {
                clientInfo = $"Client: {result.Client.NomComplet}";
            }
            else if (!string.IsNullOrEmpty(result.NomClient))
            {
                clientInfo = $"Client: {result.NomClient}";
            }

            infoTable.AddCell(new Cell().Add(new Paragraph(clientInfo).SetFontSize(ExportStyleHelper.FONT_SIZE_NORMAL))
                .SetBorder(Border.NO_BORDER));
            infoTable.AddCell(new Cell().Add(new Paragraph(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                .SetFontSize(ExportStyleHelper.FONT_SIZE_NORMAL)
                .SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(Border.NO_BORDER));

            document.Add(infoTable);
            document.Add(new Paragraph("\n"));
        }

        private void ComposeResume(Document document, AmortissementResult result, AmortissementInput input,
            PdfFont font, PdfFont fontBold)
        {
            var resumeTable = new Table(UnitValue.CreatePercentArray(new float[] { 33, 33, 34 }))
                .UseAllAvailableWidth()
                .SetBorder(new SolidBorder(CouleurPrimaire, 1))
                .SetPadding(10);

            // Colonne 1
            var col1 = new Cell()
                .SetBorder(Border.NO_BORDER)
                .Add(new Paragraph($"Montant: {ExportStyleHelper.FormatMontant(result.MontantPret)} FCFA").SetFontSize(ExportStyleHelper.FONT_SIZE_NORMAL))
                .Add(new Paragraph($"Taux: {ExportStyleHelper.FormatPourcentage(result.TauxAnnuel)}").SetFontSize(ExportStyleHelper.FONT_SIZE_NORMAL))
                .Add(new Paragraph($"Echeances: {result.NombreEcheances}").SetFontSize(ExportStyleHelper.FONT_SIZE_NORMAL))
                .Add(new Paragraph($"Periodicite: {result.PeriodiciteLibelle}").SetFontSize(ExportStyleHelper.FONT_SIZE_NORMAL));
            resumeTable.AddCell(col1);

            // Colonne 2
            var col2 = new Cell()
                .SetBorder(Border.NO_BORDER)
                .Add(new Paragraph($"Mensualite HT: {ExportStyleHelper.FormatMontant(result.Mensualite)} FCFA").SetFontSize(ExportStyleHelper.FONT_SIZE_NORMAL))
                .Add(new Paragraph($"Mensualite TTC: {ExportStyleHelper.FormatMontant(result.Mensualite)} FCFA").SetFont(fontBold).SetFontSize(ExportStyleHelper.FONT_SIZE_NORMAL))
                .Add(new Paragraph($"TEG: {ExportStyleHelper.FormatPourcentage(result.TEGCalcule)}").SetFontSize(ExportStyleHelper.FONT_SIZE_NORMAL));
            resumeTable.AddCell(col2);

            // Colonne 3
            var col3 = new Cell()
                .SetBorder(Border.NO_BORDER)
                .Add(new Paragraph($"Cout total: {ExportStyleHelper.FormatMontant(result.CoutTotalCredit)} FCFA").SetFontSize(ExportStyleHelper.FONT_SIZE_NORMAL))
                .Add(new Paragraph($"Frais dossier: {ExportStyleHelper.FormatMontant(result.FraisDossier)} FCFA").SetFontSize(ExportStyleHelper.FONT_SIZE_NORMAL))
                .Add(new Paragraph($"Du {ExportStyleHelper.FormatDate(result.DateDebut)} au {ExportStyleHelper.FormatDate(result.DateFin)}").SetFontSize(ExportStyleHelper.FONT_SIZE_NORMAL));
            resumeTable.AddCell(col3);

            document.Add(resumeTable);
            document.Add(new Paragraph("\n"));
        }

        private void ComposeTableau(Document document, AmortissementResult result, PdfFont font, PdfFont fontBold)
        {
            // Definition des colonnes: N°, Date, Capital Debut, Interets, TAF, Capital Remb, Echeance, Capital Fin
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 5, 12, 14, 12, 10, 14, 14, 14 }))
                .UseAllAvailableWidth();

            // En-tetes
            foreach (var header in ExportStyleHelper.TABLEAU_HEADERS)
            {
                var headerCell = new Cell()
                    .SetBackgroundColor(CouleurPrimaire)
                    .SetBorder(new SolidBorder(CouleurNoir, 0.5f))
                    .SetPadding(4)
                    .Add(new Paragraph(header)
                        .SetFont(fontBold)
                        .SetFontSize(ExportStyleHelper.FONT_SIZE_PETIT)
                        .SetFontColor(CouleurBlanc));
                table.AddHeaderCell(headerCell);
            }

            // Lignes du tableau
            int index = 0;
            foreach (var ligne in result.Lignes)
            {
                var bgColor = index % 2 == 0 ? CouleurBlanc : CouleurHeader;
                if (ligne.EstEnDiffere)
                    bgColor = CouleurDiffere;

                // N°
                table.AddCell(CreateDataCell(ligne.NumeroEcheance.ToString(), bgColor, TextAlignment.CENTER, ligne.EstEnDiffere));
                // Date
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatDate(ligne.DateEcheance), bgColor, TextAlignment.CENTER, ligne.EstEnDiffere));
                // Capital Debut
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatMontant(ligne.CapitalRestantDebut), bgColor, TextAlignment.RIGHT, ligne.EstEnDiffere));
                // Interets
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatMontant(ligne.Interets), bgColor, TextAlignment.RIGHT, ligne.EstEnDiffere));
                // TAF
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatMontant(ligne.TAF), bgColor, TextAlignment.RIGHT, ligne.EstEnDiffere));
                // Capital Remb
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatMontant(ligne.CapitalRembourse), bgColor, TextAlignment.RIGHT, ligne.EstEnDiffere));
                // Echeance
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatMontant(ligne.Mensualite), bgColor, TextAlignment.RIGHT, ligne.EstEnDiffere));
                // Capital Fin
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatMontant(ligne.CapitalRestantFin), bgColor, TextAlignment.RIGHT, ligne.EstEnDiffere));

                index++;
            }

            // Ligne TOTAL
            AddTotalRow(table, result, fontBold);

            document.Add(table);
        }

        private Cell CreateDataCell(string content, DeviceRgb bgColor, TextAlignment alignment, bool isItalic = false)
        {
            var paragraph = new Paragraph(content)
                .SetFontSize(ExportStyleHelper.FONT_SIZE_PETIT);

            //if (isItalic)
            //    paragraph.SetItalic();

            return new Cell()
                .SetBackgroundColor(bgColor)
                .SetBorder(new SolidBorder(CouleurNoir, 0.5f))
                .SetPadding(3)
                .SetTextAlignment(alignment)
                .Add(paragraph);
        }

        private void AddTotalRow(Table table, AmortissementResult result, PdfFont fontBold)
        {
            var totalInterets = result.TotalInterets;
            var totalTAF = result.TotalTAF;
            var totalCapitalRemb = result.TotalCapitalRembourse;
            var totalMensualite = result.TotalMensualites;

            // TOTAL
            table.AddCell(CreateTotalCell("TOTAL", fontBold, TextAlignment.LEFT));
            table.AddCell(CreateTotalCell("", fontBold, TextAlignment.CENTER));
            table.AddCell(CreateTotalCell("", fontBold, TextAlignment.RIGHT));
            table.AddCell(CreateTotalCell(ExportStyleHelper.FormatMontant(totalInterets), fontBold, TextAlignment.RIGHT));
            table.AddCell(CreateTotalCell(ExportStyleHelper.FormatMontant(totalTAF), fontBold, TextAlignment.RIGHT));
            table.AddCell(CreateTotalCell(ExportStyleHelper.FormatMontant(totalCapitalRemb), fontBold, TextAlignment.RIGHT));
            table.AddCell(CreateTotalCell(ExportStyleHelper.FormatMontant(totalMensualite), fontBold, TextAlignment.RIGHT));
            table.AddCell(CreateTotalCell("", fontBold, TextAlignment.RIGHT));
        }

        private Cell CreateTotalCell(string content, PdfFont fontBold, TextAlignment alignment)
        {
            return new Cell()
                .SetBackgroundColor(CouleurPrimaire)
                .SetBorder(new SolidBorder(CouleurNoir, 0.5f))
                .SetPadding(4)
                .SetTextAlignment(alignment)
                .Add(new Paragraph(content)
                    .SetFont(fontBold)
                    .SetFontSize(ExportStyleHelper.FONT_SIZE_PETIT)
                    .SetFontColor(CouleurBlanc));
        }
    }

    /// <summary>
    /// Gestionnaire d'evenements pour ajouter le pied de page avec pagination
    /// </summary>
    //internal class FooterEventHandler : IEventHandler
    //{
    //    private readonly PdfFont _font;

    //    public FooterEventHandler(PdfFont font)
    //    {
    //        _font = font;
    //    }

    //    //public void HandleEvent(Event @event)
    //    public override void HandleEvent(Event @event)
    //    {
    //        var docEvent = (PdfDocumentEvent)@event;
    //        var pdfDoc = docEvent.GetDocument();
    //        var page = docEvent.GetPage();
    //        var pageSize = page.GetPageSize();
    //        int pageNumber = pdfDoc.GetPageNumber(page);
    //        int totalPages = pdfDoc.GetNumberOfPages();

    //        var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);
    //        canvas.BeginText()
    //            .SetFontAndSize(_font, 8)
    //            .MoveText(pageSize.GetWidth() / 2 - 25, 25)
    //            .ShowText($"Page {pageNumber}")
    //            .EndText();
    //    }
    //}
}
