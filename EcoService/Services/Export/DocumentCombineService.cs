/*
 * =============================================================================
 * SERVICE DE GENERATION DE DOCUMENT COMBINE
 * =============================================================================
 * Genere un document PDF unique contenant:
 * - Page de garde (Portrait)
 * - Resume du pret (Portrait)
 * - Tableau d'amortissement (Paysage)
 * - Contrat de pret (Portrait)
 * - Fiche d'approbation (Portrait)
 *
 * Utilise iText7 pour supporter les orientations mixtes dans un meme document.
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




namespace EcoService.Services.Export
{
    /// <summary>
    /// Interface pour la generation du document combine
    /// </summary>
    public interface IDocumentCombineService
    {
        byte[] GenererDocumentComplet(AmortissementResult result, AmortissementInput input);
    }

    /// <summary>
    /// Implementation du service de document combine avec iText7
    /// </summary>
    public class DocumentCombineService : IDocumentCombineService
    {
        // Couleurs du theme
        private static readonly DeviceRgb CouleurPrimaire = new DeviceRgb(0, 120, 212);
        private static readonly DeviceRgb CouleurSecondaire = new DeviceRgb(16, 110, 190);
        private static readonly DeviceRgb CouleurHeader = new DeviceRgb(230, 242, 255);
        private static readonly DeviceRgb CouleurDiffere = new DeviceRgb(255, 243, 224);
        private static readonly DeviceRgb CouleurBlanc = new DeviceRgb(255, 255, 255);
        private static readonly DeviceRgb CouleurNoir = new DeviceRgb(0, 0, 0);
        private static readonly DeviceRgb CouleurGris = new DeviceRgb(128, 128, 128);
        private static readonly DeviceRgb CouleurRouge = new DeviceRgb(200, 0, 0);

        public byte[] GenererDocumentComplet(AmortissementResult result, AmortissementInput input)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdfDocument = new PdfDocument(writer);

            // Polices
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // Gestionnaire pour pagination
           // pdfDocument.AddEventHandler(PdfDocumentEvent.END_PAGE, new CombinedFooterHandler(font));

            // Creer le document avec la taille par defaut (Portrait A4)
            using var document = new Document(pdfDocument, PageSize.A4);
            document.SetMargins(50, 50, 50, 50);

            // Page 1: Page de garde (Portrait)
            ComposePageDeGarde(document, result, input, font, fontBold);

            // Page 2: Resume du pret (Portrait)
            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            document.SetMargins(40, 40, 50, 40);
            ComposeResumePage(document, result, input, font, fontBold);

            // Page 3+: Tableau d'amortissement (Paysage)
            document.Add(new AreaBreak(new PageSize(PageSize.A4.Rotate())));
            document.SetMargins(30, 30, 50, 30);
            ComposeTableauPage(document, result, font, fontBold);

            // Page: Contrat de pret (Portrait)
            document.Add(new AreaBreak(new PageSize(PageSize.A4)));
            document.SetMargins(40, 40, 50, 40);
            ComposeContratPage(document, result, input, font, fontBold);

            // Page: Fiche d'approbation (Portrait)
            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            ComposeFicheApprobationPage(document, result, input, font, fontBold);

            document.Close();
            return memoryStream.ToArray();
        }

        #region Page de Garde

        private void ComposePageDeGarde(Document document, AmortissementResult result, AmortissementInput input,
            PdfFont font, PdfFont fontBold)
        {
            // Espace en haut
            document.Add(new Paragraph("\n\n\n\n"));

            // Titre principal
            document.Add(new Paragraph("Tous les documents généré par ce programme sont toujours cours de developpement...")
                .SetFont(fontBold)
                .SetFontSize(10)
                .SetFontColor(CouleurGris)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("\n"));
            document.Add(new Paragraph("DOSSIER DE CREDIT")
                .SetFont(fontBold)
                .SetFontSize(28)
                .SetFontColor(CouleurPrimaire)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("\n"));

            document.Add(new Paragraph("TABLEAU D'AMORTISSEMENT")
                .SetFont(fontBold)
                .SetFontSize(20)
                .SetFontColor(CouleurSecondaire)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("\n\n\n"));

            // Informations client
            if (result.Client != null)
            {
                document.Add(new Paragraph($"Client: {result.Client.NomComplet}")
                    .SetFont(fontBold)
                    .SetFontSize(16)
                    .SetTextAlignment(TextAlignment.CENTER));
                document.Add(new Paragraph($"Compte: {result.Client.NumeroCompte}")
                    .SetFont(font)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER));
            }
            else if (!string.IsNullOrEmpty(result.NomClient))
            {
                document.Add(new Paragraph($"Client: {result.NomClient}")
                    .SetFont(fontBold)
                    .SetFontSize(16)
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            document.Add(new Paragraph("\n\n"));

            // Cadre resume
            var resumeTable = new Table(1).UseAllAvailableWidth()
                .SetBorder(new SolidBorder(CouleurPrimaire, 2))
                .SetPadding(20)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetWidth(UnitValue.CreatePercentValue(60));

            var resumeCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .Add(new Paragraph($"Montant: {ExportStyleHelper.FormatMontant(result.MontantPret)} FCFA").SetFont(font).SetFontSize(14).SetTextAlignment(TextAlignment.CENTER))
                .Add(new Paragraph($"Duree: {result.NombreEcheances} echeances ({result.PeriodiciteLibelle})").SetFont(font).SetFontSize(14).SetTextAlignment(TextAlignment.CENTER))
                .Add(new Paragraph($"Taux: {ExportStyleHelper.FormatPourcentage(result.TauxAnnuel)}").SetFont(font).SetFontSize(14).SetTextAlignment(TextAlignment.CENTER))
                .Add(new Paragraph($"Mensualite: {ExportStyleHelper.FormatMontant(result.MensualiteAvecAssurance)} FCFA").SetFont(fontBold).SetFontSize(14).SetTextAlignment(TextAlignment.CENTER));
            resumeTable.AddCell(resumeCell);

            document.Add(resumeTable);

            document.Add(new Paragraph("\n\n\n"));

            // Objet du credit
            if (!string.IsNullOrEmpty(input.ObjetCredit))
            {
                document.Add(new Paragraph($"Objet: {input.ObjetCredit}")
                    .SetFont(font)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            document.Add(new Paragraph("\n\n\n\n"));

            // Date de generation
            document.Add(new Paragraph($"Document genere le {DateTime.Now:dd/MM/yyyy a HH:mm}")
                .SetFont(font)
                .SetFontSize(10)
                .SetFontColor(CouleurGris)
                .SetTextAlignment(TextAlignment.CENTER));
        }

        #endregion

        #region Resume

        private void ComposeResumePage(Document document, AmortissementResult result, AmortissementInput input,
            PdfFont font, PdfFont fontBold)
        {
            // En-tete
            AddPageHeader(document, "RESUME DU PRET", fontBold, font);

            // Informations client
            if (result.Client != null)
            {
                AddSectionTitle(document, "INFORMATIONS CLIENT", fontBold);
                var clientTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();
                AddInfoRow(clientTable, "Nom complet", result.Client.NomComplet, font);
                AddInfoRow(clientTable, "Numero compte", result.Client.NumeroCompte, font);
                if (result.Client.DateNaissance.HasValue)
                    AddInfoRow(clientTable, "Date naissance", ExportStyleHelper.FormatDate(result.Client.DateNaissance.Value), font);
                if (!string.IsNullOrEmpty(result.Client.Telephone))
                    AddInfoRow(clientTable, "Telephone", result.Client.Telephone, font);
                if (!string.IsNullOrEmpty(result.Client.Employeur))
                    AddInfoRow(clientTable, "Employeur", result.Client.Employeur, font);
                document.Add(clientTable);
                document.Add(new Paragraph("\n"));
            }

            // Caracteristiques du pret
            AddSectionTitle(document, "CARACTERISTIQUES DU PRET", fontBold);
            var pretTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();
            AddInfoRow(pretTable, "Montant du pret", $"{ExportStyleHelper.FormatMontant(result.MontantPret)} FCFA", font);
            AddInfoRow(pretTable, "Taux annuel", ExportStyleHelper.FormatPourcentage(result.TauxAnnuel), font);
            AddInfoRow(pretTable, "Nombre d'echeances", result.NombreEcheances.ToString(), font);
            AddInfoRow(pretTable, "Periodicite", result.PeriodiciteLibelle, font);
            AddInfoRow(pretTable, "Mode remboursement", result.ModeRemboursementLibelle, font);
            if (!string.IsNullOrEmpty(input.ObjetCredit))
                AddInfoRow(pretTable, "Objet du credit", input.ObjetCredit, font);
            document.Add(pretTable);
            document.Add(new Paragraph("\n"));

            // Echeances
            AddSectionTitle(document, "ECHEANCES", fontBold);
            var echTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();
            AddInfoRow(echTable, "Mensualite HT", $"{ExportStyleHelper.FormatMontant(result.Mensualite)} FCFA", font);
            AddInfoRow(echTable, "Assurance/echeance", $"{ExportStyleHelper.FormatMontant(result.MensualiteAvecAssurance - result.Mensualite)} FCFA", font);
            AddInfoRow(echTable, "Mensualite TTC", $"{ExportStyleHelper.FormatMontant(result.MensualiteAvecAssurance)} FCFA", font, true);
            document.Add(echTable);
            document.Add(new Paragraph("\n"));

            // Couts
            AddSectionTitle(document, "COUTS", fontBold);
            var coutTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();
            AddInfoRow(coutTable, "Total interets", $"{ExportStyleHelper.FormatMontant(result.TotalInterets)} FCFA", font);
            AddInfoRow(coutTable, "Total TAF", $"{ExportStyleHelper.FormatMontant(result.TotalTAF)} FCFA", font);
            AddInfoRow(coutTable, "Total assurance", $"{ExportStyleHelper.FormatMontant(result.TotalAssurance)} FCFA", font);
            AddInfoRow(coutTable, "Frais de dossier", $"{ExportStyleHelper.FormatMontant(result.FraisDossier)} FCFA", font);
            AddInfoRow(coutTable, "Cout total du credit", $"{ExportStyleHelper.FormatMontant(result.CoutTotalCredit)} FCFA", font, true);
            AddInfoRow(coutTable, "TEG calcule", ExportStyleHelper.FormatPourcentage(result.TEGCalcule), font, true);
            document.Add(coutTable);
            document.Add(new Paragraph("\n"));

            // Dates
            AddSectionTitle(document, "DATES", fontBold);
            var dateTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();
            AddInfoRow(dateTable, "Date premiere echeance", ExportStyleHelper.FormatDate(result.DateDebut), font);
            AddInfoRow(dateTable, "Date derniere echeance", ExportStyleHelper.FormatDate(result.DateFin), font);
            if (result.DiffereEcheances > 0)
                AddInfoRow(dateTable, "Differe", $"{result.DiffereEcheances} echeance(s)", font);
            document.Add(dateTable);
        }

        #endregion

        #region Tableau d'amortissement

        private void ComposeTableauPage(Document document, AmortissementResult result, PdfFont font, PdfFont fontBold)
        {
            // En-tete
            AddPageHeader(document, "TABLEAU D'AMORTISSEMENT", fontBold, font);

            // Tableau
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 5, 12, 14, 12, 10, 14, 14, 14 }))
                .UseAllAvailableWidth();

            // En-tetes
            foreach (var header in ExportStyleHelper.TABLEAU_HEADERS)
            {
                table.AddHeaderCell(new Cell()
                    .SetBackgroundColor(CouleurPrimaire)
                    .SetBorder(new SolidBorder(CouleurNoir, 0.5f))
                    .SetPadding(4)
                    .Add(new Paragraph(header).SetFont(fontBold).SetFontSize(8).SetFontColor(CouleurBlanc)));
            }

            // Lignes
            int index = 0;
            foreach (var ligne in result.Lignes)
            {
                var bgColor = index % 2 == 0 ? CouleurBlanc : CouleurHeader;
                if (ligne.EstEnDiffere) bgColor = CouleurDiffere;

                table.AddCell(CreateDataCell(ligne.NumeroEcheance.ToString(), bgColor, TextAlignment.CENTER, ligne.EstEnDiffere));
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatDate(ligne.DateEcheance), bgColor, TextAlignment.CENTER, ligne.EstEnDiffere));
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatMontant(ligne.CapitalRestantDebut), bgColor, TextAlignment.RIGHT, ligne.EstEnDiffere));
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatMontant(ligne.Interets), bgColor, TextAlignment.RIGHT, ligne.EstEnDiffere));
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatMontant(ligne.TAF), bgColor, TextAlignment.RIGHT, ligne.EstEnDiffere));
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatMontant(ligne.CapitalRembourse), bgColor, TextAlignment.RIGHT, ligne.EstEnDiffere));
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatMontant(ligne.Mensualite), bgColor, TextAlignment.RIGHT, ligne.EstEnDiffere));
                table.AddCell(CreateDataCell(ExportStyleHelper.FormatMontant(ligne.CapitalRestantFin), bgColor, TextAlignment.RIGHT, ligne.EstEnDiffere));

                index++;
            }

            // Total
            string[] totaux = { "TOTAL", "", "",
                ExportStyleHelper.FormatMontant(result.TotalInterets),
                ExportStyleHelper.FormatMontant(result.TotalTAF),
                ExportStyleHelper.FormatMontant(result.TotalCapitalRembourse),
                ExportStyleHelper.FormatMontant(result.TotalMensualites), "" };

            for (int i = 0; i < totaux.Length; i++)
            {
                var align = i >= 3 && i <= 6 ? TextAlignment.RIGHT : (i == 0 ? TextAlignment.LEFT : TextAlignment.CENTER);
                table.AddCell(CreateTotalCell(totaux[i], fontBold, align));
            }

            document.Add(table);
        }

        #endregion

        #region Contrat de pret

        private void ComposeContratPage(Document document, AmortissementResult result, AmortissementInput input,
            PdfFont font, PdfFont fontBold)
        {
            AddPageHeader(document, "CONTRAT DE PRET", fontBold, font);

            document.Add(new Paragraph("ENTRE LES SOUSSIGNES :").SetFont(fontBold).SetFontSize(10));
            document.Add(new Paragraph("\n"));
            document.Add(new Paragraph("La BANQUE, representee par son Directeur General,").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph("Ci-apres denommee \"LE PRETEUR\"").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph("\n"));
            document.Add(new Paragraph("D'UNE PART,").SetFont(font).SetFontSize(10).SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph("\n"));

            if (result.Client != null)
            {
                document.Add(new Paragraph($"Et {result.Client.NomComplet},").SetFont(font).SetFontSize(10));
                if (!string.IsNullOrEmpty(result.Client.Adresse))
                    document.Add(new Paragraph($"Demeurant a {result.Client.Adresse} {result.Client.Ville}").SetFont(font).SetFontSize(10));
            }
            else
            {
                document.Add(new Paragraph($"Et {result.NomClient ?? "[NOM DU CLIENT]"},").SetFont(font).SetFontSize(10));
            }
            document.Add(new Paragraph("Ci-apres denomme(e) \"L'EMPRUNTEUR\"").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph("\n"));
            document.Add(new Paragraph("D'AUTRE PART,").SetFont(font).SetFontSize(10).SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph("\n"));

            document.Add(new Paragraph("IL A ETE CONVENU CE QUI SUIT :").SetFont(fontBold).SetFontSize(10));
            document.Add(new Paragraph("\n"));

            // Articles
            document.Add(new Paragraph("ARTICLE 1 - OBJET DU PRET").SetFont(fontBold).SetFontSize(10));
            document.Add(new Paragraph($"Le Preteur consent a l'Emprunteur un pret d'un montant de {ExportStyleHelper.FormatMontant(result.MontantPret)} FCFA (en lettres: ... Francs CFA).").SetFont(font).SetFontSize(10));
            if (!string.IsNullOrEmpty(input.ObjetCredit))
                document.Add(new Paragraph($"Ce pret est destine a : {input.ObjetCredit}.").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph("\n"));

            document.Add(new Paragraph("ARTICLE 2 - DUREE ET REMBOURSEMENT").SetFont(fontBold).SetFontSize(10));
            document.Add(new Paragraph($"Le pret est consenti pour une duree de {result.NombreEcheances} echeances {result.PeriodiciteLibelle.ToLower()}es.").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph($"La premiere echeance prendra effet le {ExportStyleHelper.FormatDate(result.DateDebut)}.").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph($"La derniere echeance sera exigible le {ExportStyleHelper.FormatDate(result.DateFin)}.").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph("\n"));

            document.Add(new Paragraph("ARTICLE 3 - TAUX ET CONDITIONS").SetFont(fontBold).SetFontSize(10));
            document.Add(new Paragraph($"- Taux d'interet nominal annuel: {ExportStyleHelper.FormatPourcentage(result.TauxAnnuel)}").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph($"- TEG (Taux Effectif Global): {ExportStyleHelper.FormatPourcentage(result.TEGCalcule)}").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph($"- Frais de dossier: {ExportStyleHelper.FormatMontant(result.FraisDossier)} FCFA").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph($"- Mensualite: {ExportStyleHelper.FormatMontant(result.MensualiteAvecAssurance)} FCFA (assurance incluse)").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph("\n"));

            document.Add(new Paragraph("ARTICLE 4 - COUT TOTAL DU CREDIT").SetFont(fontBold).SetFontSize(10));
            document.Add(new Paragraph($"Le cout total du credit s'eleve a {ExportStyleHelper.FormatMontant(result.CoutTotalCredit)} FCFA, comprenant:").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph($"- Total des interets: {ExportStyleHelper.FormatMontant(result.TotalInterets)} FCFA").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph($"- Total TAF: {ExportStyleHelper.FormatMontant(result.TotalTAF)} FCFA").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph($"- Total assurance: {ExportStyleHelper.FormatMontant(result.TotalAssurance)} FCFA").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph("\n\n"));

            // Signatures
            var sigTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();
            sigTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("LE PRETEUR").SetFont(fontBold).SetFontSize(10))
                .Add(new Paragraph("\n\n\n"))
                .Add(new Paragraph("_______________________").SetFont(font).SetFontSize(10))
                .Add(new Paragraph("Date et Signature").SetFont(font).SetFontSize(9)));
            sigTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("L'EMPRUNTEUR").SetFont(fontBold).SetFontSize(10))
                .Add(new Paragraph("\n\n\n"))
                .Add(new Paragraph("_______________________").SetFont(font).SetFontSize(10))
                .Add(new Paragraph("Lu et approuve, Date et Signature").SetFont(font).SetFontSize(9)));
            document.Add(sigTable);
        }

        #endregion

        #region Fiche d'approbation

        private void ComposeFicheApprobationPage(Document document, AmortissementResult result, AmortissementInput input,
            PdfFont font, PdfFont fontBold)
        {
            AddPageHeader(document, "FICHE D'APPROBATION", fontBold, font);

            document.Add(new Paragraph("FICHE D'APPROBATION DE CREDIT")
                .SetFont(fontBold)
                .SetFontSize(16)
                .SetFontColor(CouleurPrimaire)
                .SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph("\n"));

            // Informations client
            var clientBox = new Table(1).UseAllAvailableWidth()
                .SetBorder(new SolidBorder(CouleurGris, 1))
                .SetPadding(10);
            var clientCell = new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("INFORMATIONS CLIENT").SetFont(fontBold).SetFontSize(10));
            if (result.Client != null)
            {
                clientCell.Add(new Paragraph($"Nom: {result.Client.NomComplet}").SetFont(font).SetFontSize(10));
                clientCell.Add(new Paragraph($"Compte: {result.Client.NumeroCompte}").SetFont(font).SetFontSize(10));
                if (!string.IsNullOrEmpty(result.Client.Profession))
                    clientCell.Add(new Paragraph($"Profession: {result.Client.Profession}").SetFont(font).SetFontSize(10));
                if (!string.IsNullOrEmpty(result.Client.Employeur))
                    clientCell.Add(new Paragraph($"Employeur: {result.Client.Employeur}").SetFont(font).SetFontSize(10));
            }
            else
            {
                clientCell.Add(new Paragraph($"Nom: {result.NomClient ?? "_______________"}").SetFont(font).SetFontSize(10));
            }
            clientBox.AddCell(clientCell);
            document.Add(clientBox);
            document.Add(new Paragraph("\n"));

            // Caracteristiques du pret
            var pretBox = new Table(1).UseAllAvailableWidth()
                .SetBorder(new SolidBorder(CouleurGris, 1))
                .SetPadding(10);
            var pretCell = new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("CARACTERISTIQUES DU PRET").SetFont(fontBold).SetFontSize(10))
                .Add(new Paragraph($"Montant: {ExportStyleHelper.FormatMontant(result.MontantPret)} FCFA").SetFont(font).SetFontSize(10))
                .Add(new Paragraph($"Duree: {result.NombreEcheances} mois").SetFont(font).SetFontSize(10))
                .Add(new Paragraph($"Taux: {ExportStyleHelper.FormatPourcentage(result.TauxAnnuel)} | TEG: {ExportStyleHelper.FormatPourcentage(result.TEGCalcule)}").SetFont(font).SetFontSize(10))
                .Add(new Paragraph($"Mensualite: {ExportStyleHelper.FormatMontant(result.MensualiteAvecAssurance)} FCFA").SetFont(font).SetFontSize(10));
            if (!string.IsNullOrEmpty(input.ObjetCredit))
                pretCell.Add(new Paragraph($"Objet: {input.ObjetCredit}").SetFont(font).SetFontSize(10));
            pretBox.AddCell(pretCell);
            document.Add(pretBox);
            document.Add(new Paragraph("\n"));

            // Analyse de risque
            var riskBox = new Table(1).UseAllAvailableWidth()
                .SetBorder(new SolidBorder(CouleurGris, 1))
                .SetPadding(10);
            var riskCell = new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("ANALYSE DE RISQUE").SetFont(fontBold).SetFontSize(10));
            if (input.SalaireMensuel > 0)
            {
                riskCell.Add(new Paragraph($"Salaire mensuel: {ExportStyleHelper.FormatMontant(input.SalaireMensuel)} FCFA").SetFont(font).SetFontSize(10));
                riskCell.Add(new Paragraph($"Taux d'endettement: {result.TauxEndettementActuel:F1}% (max autorise: {result.TauxEndettementMax:F0}%)").SetFont(font).SetFontSize(10));
                riskCell.Add(new Paragraph($"Reste a vivre: {ExportStyleHelper.FormatMontant(result.ResteAVivre)} FCFA").SetFont(font).SetFontSize(10));
            }
            if (result.Alertes.Any(a => a.Type == TypeAlerte.Critique))
            {
                riskCell.Add(new Paragraph("\nALERTES:").SetFont(fontBold).SetFontSize(10).SetFontColor(CouleurRouge));
                foreach (var alerte in result.Alertes.Where(a => a.Type == TypeAlerte.Critique))
                {
                    riskCell.Add(new Paragraph($"- {alerte.Message}").SetFont(font).SetFontSize(10).SetFontColor(CouleurRouge));
                }
            }
            riskBox.AddCell(riskCell);
            document.Add(riskBox);
            document.Add(new Paragraph("\n\n"));

            // Zone de decision
            document.Add(new Paragraph("DECISION").SetFont(fontBold).SetFontSize(12));
            document.Add(new Paragraph("\n"));

            var decisionTable = new Table(UnitValue.CreatePercentArray(new float[] { 33, 33, 34 })).UseAllAvailableWidth();
            decisionTable.AddCell(new Cell().SetBorder(new SolidBorder(CouleurNoir, 1)).SetPadding(5)
                .Add(new Paragraph("[ ] APPROUVE").SetFont(font).SetFontSize(10)));
            decisionTable.AddCell(new Cell().SetBorder(new SolidBorder(CouleurNoir, 1)).SetPadding(5)
                .Add(new Paragraph("[ ] REJETE").SetFont(font).SetFontSize(10)));
            decisionTable.AddCell(new Cell().SetBorder(new SolidBorder(CouleurNoir, 1)).SetPadding(5)
                .Add(new Paragraph("[ ] EN ATTENTE").SetFont(font).SetFontSize(10)));
            document.Add(decisionTable);
            document.Add(new Paragraph("\n"));

            document.Add(new Paragraph("Observations: _______________________________________________").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph("____________________________________________________________").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph("\n\n"));

            // Signatures
            var sigTable = new Table(UnitValue.CreatePercentArray(new float[] { 33, 33, 34 })).UseAllAvailableWidth();
            sigTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("Charge de clientele").SetFont(fontBold).SetFontSize(10))
                .Add(new Paragraph("\n\n"))
                .Add(new Paragraph("Nom: ________________").SetFont(font).SetFontSize(9))
                .Add(new Paragraph("Date: ________________").SetFont(font).SetFontSize(9))
                .Add(new Paragraph("Signature:").SetFont(font).SetFontSize(9)));
            sigTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("Responsable Credit").SetFont(fontBold).SetFontSize(10))
                .Add(new Paragraph("\n\n"))
                .Add(new Paragraph("Nom: ________________").SetFont(font).SetFontSize(9))
                .Add(new Paragraph("Date: ________________").SetFont(font).SetFontSize(9))
                .Add(new Paragraph("Signature:").SetFont(font).SetFontSize(9)));
            sigTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("Direction").SetFont(fontBold).SetFontSize(10))
                .Add(new Paragraph("\n\n"))
                .Add(new Paragraph("Nom: ________________").SetFont(font).SetFontSize(9))
                .Add(new Paragraph("Date: ________________").SetFont(font).SetFontSize(9))
                .Add(new Paragraph("Signature:").SetFont(font).SetFontSize(9)));
            document.Add(sigTable);
        }

        #endregion

        #region Helpers

        private void AddPageHeader(Document document, string titre, PdfFont fontBold, PdfFont font)
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 })).UseAllAvailableWidth();
            headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph(titre).SetFont(fontBold).SetFontSize(14).SetFontColor(CouleurPrimaire)));
            headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                .Add(new Paragraph(DateTime.Now.ToString("dd/MM/yyyy")).SetFont(font).SetFontSize(10)));
            document.Add(headerTable);
            document.Add(new Paragraph("\n"));
        }

        private void AddSectionTitle(Document document, string titre, PdfFont fontBold)
        {
            document.Add(new Paragraph(titre)
                .SetFont(fontBold)
                .SetFontSize(12)
                .SetFontColor(CouleurPrimaire)
                .SetBorderBottom(new SolidBorder(CouleurPrimaire, 1))
                .SetPaddingBottom(5));
        }

        private void AddInfoRow(Table table, string label, string valeur, PdfFont font, bool highlight = false)
        {
            table.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetPadding(3)
                .Add(new Paragraph(label).SetFont(font).SetFontSize(10)));
            var valueCell = new Cell().SetBorder(Border.NO_BORDER).SetPadding(3).SetTextAlignment(TextAlignment.RIGHT);
            var valuePara = new Paragraph(valeur).SetFont(font).SetFontSize(10);
            //if (highlight) valuePara.SetBold();
            valueCell.Add(valuePara);
            table.AddCell(valueCell);
        }

        private Cell CreateDataCell(string content, DeviceRgb bgColor, TextAlignment alignment, bool isItalic = false)
        {
            var para = new Paragraph(content).SetFontSize(8);
            //if (isItalic) para.SetItalic();
            return new Cell()
                .SetBackgroundColor(bgColor)
                .SetBorder(new SolidBorder(CouleurNoir, 0.5f))
                .SetPadding(3)
                .SetTextAlignment(alignment)
                .Add(para);
        }

        private Cell CreateTotalCell(string content, PdfFont fontBold, TextAlignment alignment)
        {
            return new Cell()
                .SetBackgroundColor(CouleurPrimaire)
                .SetBorder(new SolidBorder(CouleurNoir, 0.5f))
                .SetPadding(4)
                .SetTextAlignment(alignment)
                .Add(new Paragraph(content).SetFont(fontBold).SetFontSize(8).SetFontColor(CouleurBlanc));
        }

        #endregion
    }

    /// <summary>
    /// Gestionnaire d'evenements pour la pagination du document combine
    /// </summary>
    //internal class CombinedFooterHandler : IEventHandler
    //{
    //    private readonly PdfFont _font;

    //    public CombinedFooterHandler(PdfFont font)
    //    {
    //        _font = font;
    //    }

    //    public void HandleEvent(Event @event)
    //    {
    //        var docEvent = (PdfDocumentEvent)@event;
    //        var pdfDoc = docEvent.GetDocument();
    //        var page = docEvent.GetPage();
    //        var pageSize = page.GetPageSize();
    //        int pageNumber = pdfDoc.GetPageNumber(page);

    //        var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);
    //        canvas.BeginText()
    //            .SetFontAndSize(_font, 8)
    //            .MoveText(pageSize.GetWidth() / 2 - 25, 25)
    //            .ShowText($"Page {pageNumber}")
    //            .EndText();
    //    }
    //}

}