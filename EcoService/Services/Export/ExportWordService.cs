/*
 * =============================================================================
 * SERVICE D'EXPORT WORD
 * =============================================================================
 * Genere un fichier Word (.docx) avec:
 * - Titre et resume du pret
 * - Tableau d'amortissement avec bordures
 *
 * Utilise la bibliotheque DocumentFormat.OpenXml (MIT License)
 * =============================================================================
 */

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using EcoService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using WordColor = DocumentFormat.OpenXml.Wordprocessing.Color;
using WordPageSize = DocumentFormat.OpenXml.Wordprocessing.PageSize;

namespace EcoService.Services.Export
{
    /// <summary>
    /// Interface pour l'export Word
    /// </summary>
    public interface IExportWordService
    {
        byte[] ExportToWord(AmortissementResult result, AmortissementInput input);
    }

    /// <summary>
    /// Implementation de l'export Word
    /// </summary>
    public class ExportWordService : IExportWordService
    {
        public byte[] ExportToWord(AmortissementResult result, AmortissementInput input)
        {
            using var stream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new WordDocument();
                var body = mainPart.Document.AppendChild(new Body());

                // Titre
                body.Append(CreerParagraphe("TABLEAU D'AMORTISSEMENT", true, 28, ExportStyleHelper.COULEUR_PRIMAIRE));
                body.Append(new Paragraph());

                // Informations client
                if (result.Client != null)
                {
                    body.Append(CreerParagraphe($"Client: {result.Client.NomComplet}", true, 12));
                    body.Append(CreerParagraphe($"Compte: {result.Client.NumeroCompte}", false, 11));
                }
                else if (!string.IsNullOrEmpty(result.NomClient))
                {
                    body.Append(CreerParagraphe($"Client: {result.NomClient}", true, 12));
                }
                body.Append(new Paragraph());

                // Resume du pret
                body.Append(CreerParagraphe("CARACTERISTIQUES DU PRET", true, 14, ExportStyleHelper.COULEUR_SECONDAIRE));
                body.Append(CreerParagraphe($"Montant du pret: {ExportStyleHelper.FormatMontant(result.MontantPret)} FCFA", false, 11));
                body.Append(CreerParagraphe($"Taux annuel: {ExportStyleHelper.FormatPourcentage(result.TauxAnnuel)}", false, 11));
                body.Append(CreerParagraphe($"Nombre d'echeances: {result.NombreEcheances}", false, 11));
                body.Append(CreerParagraphe($"Periodicite: {result.PeriodiciteLibelle}", false, 11));
                body.Append(CreerParagraphe($"Mensualite: {ExportStyleHelper.FormatMontant(result.Mensualite)} FCFA", true, 11));
                body.Append(CreerParagraphe($"TEG: {ExportStyleHelper.FormatPourcentage(result.TEGCalcule)}", false, 11));
                body.Append(CreerParagraphe($"Cout total du credit: {ExportStyleHelper.FormatMontant(result.CoutTotalCredit)} FCFA", false, 11));
                body.Append(CreerParagraphe($"Du {ExportStyleHelper.FormatDate(result.DateDebut)} au {ExportStyleHelper.FormatDate(result.DateFin)}", false, 11));
                body.Append(new Paragraph());

                // Tableau d'amortissement
                body.Append(CreerParagraphe("ECHEANCIER", true, 14, ExportStyleHelper.COULEUR_SECONDAIRE));
                body.Append(new Paragraph());
                var table = CreerTableau(result);
                body.Append(table);

                // Configuration de la page en paysage
                var sectionProps = new SectionProperties(
                    new WordPageSize() { Width = 16838, Height = 11906, Orient = PageOrientationValues.Landscape },
                    new PageMargin() { Top = 720, Right = 720, Bottom = 720, Left = 720 }
                );
                body.Append(sectionProps);
            }

            return stream.ToArray();
        }

        private Paragraph CreerParagraphe(string text, bool bold, int fontSize, string? color = null)
        {
            var run = new Run();
            var runProperties = new RunProperties();
            runProperties.Append(new FontSize() { Val = (fontSize * 2).ToString() });

            if (bold)
                runProperties.Append(new Bold());

            if (!string.IsNullOrEmpty(color))
                runProperties.Append(new WordColor() { Val = ExportStyleHelper.CouleurSansHash(color) });

            run.Append(runProperties);
            run.Append(new Text(text));

            return new Paragraph(run);
        }

        private Table CreerTableau(AmortissementResult result)
        {
            var table = new Table();

            // Proprietes du tableau avec bordures
            var tableProperties = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 4, Color = "000000" },
                    new BottomBorder() { Val = BorderValues.Single, Size = 4, Color = "000000" },
                    new LeftBorder() { Val = BorderValues.Single, Size = 4, Color = "000000" },
                    new RightBorder() { Val = BorderValues.Single, Size = 4, Color = "000000" },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4, Color = "000000" },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4, Color = "000000" }
                ),
                new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
            );
            table.Append(tableProperties);

            // En-tete
            var headerRow = new TableRow();
            foreach (var header in ExportStyleHelper.TABLEAU_HEADERS)
            {
                headerRow.Append(CreerCellule(header, true, ExportStyleHelper.CouleurSansHash(ExportStyleHelper.COULEUR_PRIMAIRE)));
            }
            table.Append(headerRow);

            // Donnees
            int index = 0;
            foreach (var ligne in result.Lignes)
            {
                var row = new TableRow();
                string? bgColor = null;

                if (ligne.EstEnDiffere)
                    bgColor = ExportStyleHelper.CouleurSansHash(ExportStyleHelper.COULEUR_DIFFERE);
                else if (index % 2 == 1)
                    bgColor = ExportStyleHelper.CouleurSansHash(ExportStyleHelper.COULEUR_HEADER);

                row.Append(CreerCellule(ligne.NumeroEcheance.ToString(), false, bgColor));
                row.Append(CreerCellule(ExportStyleHelper.FormatDate(ligne.DateEcheance), false, bgColor));
                row.Append(CreerCellule(ExportStyleHelper.FormatMontant(ligne.CapitalRestantDebut), false, bgColor));
                row.Append(CreerCellule(ExportStyleHelper.FormatMontant(ligne.Interets), false, bgColor));
                row.Append(CreerCellule(ExportStyleHelper.FormatMontant(ligne.TAF), false, bgColor));
                row.Append(CreerCellule(ExportStyleHelper.FormatMontant(ligne.CapitalRembourse), false, bgColor));
                row.Append(CreerCellule(ExportStyleHelper.FormatMontant(ligne.Mensualite), false, bgColor));
                row.Append(CreerCellule(ExportStyleHelper.FormatMontant(ligne.CapitalRestantFin), false, bgColor));
                table.Append(row);

                index++;
            }

            // Ligne TOTAL
            AjouterLigneTotal(table, result);

            return table;
        }

        private void AjouterLigneTotal(Table table, AmortissementResult result)
        {
            // Utiliser les totaux pre-calcules du result (coherence avec Excel)
            var totalInterets = result.TotalInterets;
            var totalTAF = result.TotalTAF;
            var totalCapitalRemb = result.TotalCapitalRembourse;
            var totalMensualite = result.TotalMensualites;

            var couleurPrimaire = ExportStyleHelper.CouleurSansHash(ExportStyleHelper.COULEUR_PRIMAIRE);

            var totalRow = new TableRow();
            totalRow.Append(CreerCellule("TOTAL", true, couleurPrimaire, true));
            totalRow.Append(CreerCellule("", true, couleurPrimaire, true));
            totalRow.Append(CreerCellule("", true, couleurPrimaire, true));
            totalRow.Append(CreerCellule(ExportStyleHelper.FormatMontant(totalInterets), true, couleurPrimaire, true));
            totalRow.Append(CreerCellule(ExportStyleHelper.FormatMontant(totalTAF), true, couleurPrimaire, true));
            totalRow.Append(CreerCellule(ExportStyleHelper.FormatMontant(totalCapitalRemb), true, couleurPrimaire, true));
            totalRow.Append(CreerCellule(ExportStyleHelper.FormatMontant(totalMensualite), true, couleurPrimaire, true));
            totalRow.Append(CreerCellule("", true, couleurPrimaire, true));
            table.Append(totalRow);
        }

        private TableCell CreerCellule(string text, bool isHeader = false, string? backgroundColor = null, bool textBlanc = false)
        {
            var cell = new TableCell();

            // Proprietes de la cellule
            var cellProperties = new TableCellProperties();
            if (!string.IsNullOrEmpty(backgroundColor))
            {
                cellProperties.Append(new Shading()
                {
                    Val = ShadingPatternValues.Clear,
                    Fill = backgroundColor
                });
            }
            cell.Append(cellProperties);

            // Contenu
            var run = new Run();
            var runProperties = new RunProperties();
            runProperties.Append(new FontSize() { Val = "18" });

            if (isHeader)
                runProperties.Append(new Bold());

            if (textBlanc || (isHeader && backgroundColor == ExportStyleHelper.CouleurSansHash(ExportStyleHelper.COULEUR_PRIMAIRE)))
                runProperties.Append(new WordColor() { Val = "FFFFFF" });

            run.Append(runProperties);
            run.Append(new Text(text));

            var paragraph = new Paragraph(run);
            cell.Append(paragraph);

            return cell;
        }
    }
}