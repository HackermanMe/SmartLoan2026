using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EcoService.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

namespace EcoService.Services.Export
{
    /// <summary>
    /// Service pour générer le dossier complet Word avec tableau d'amortissement
    /// </summary>
    public class ExportDossierCompletWordService
    {
        private readonly string _templatePath;

        public ExportDossierCompletWordService()
        {
            _templatePath = HttpContext.Current.Server.MapPath("~/Templates/DOSSIER - MONDO 2.docx");
        }

        /// <summary>
        /// Génère le dossier complet Word avec tous les placeholders et tableaux
        /// </summary>
        public byte[] Generer(AmortissementResultViewModel data)
        {
            if (!File.Exists(_templatePath))
            {
                throw new FileNotFoundException($"Template introuvable : {_templatePath}");
            }

            // Créer une copie en mémoire du template
            using (var memStream = new MemoryStream())
            {
                using (var fileStream = File.OpenRead(_templatePath))
                {
                    fileStream.CopyTo(memStream);
                }
                memStream.Position = 0;

                using (WordprocessingDocument doc = WordprocessingDocument.Open(memStream, true))
                {
                    // Remplacer tous les placeholders
                    RemplacerPlaceholders(doc, data);

                    // Insérer le tableau d'amortissement
                    InsererTableauAmortissement(doc, data);

                    doc.Save();
                }

                return memStream.ToArray();
            }
        }

        /// <summary>
        /// Remplace tous les placeholders <<...>> dans le document
        /// Gère les placeholders fragmentés en plusieurs runs
        /// </summary>
        private void RemplacerPlaceholders(WordprocessingDocument doc, AmortissementResultViewModel data)
        {
            var replacements = ConstruireDictionnaireReplacements(data);

            // Traiter le document principal
            if (doc.MainDocumentPart?.Document?.Body != null)
            {
                RemplacerDansParagraphes(doc.MainDocumentPart.Document.Body.Descendants<Paragraph>(), replacements);
            }

            // Traiter les en-têtes
            foreach (var headerPart in doc.MainDocumentPart?.HeaderParts ?? Enumerable.Empty<HeaderPart>())
            {
                if (headerPart.Header != null)
                {
                    RemplacerDansParagraphes(headerPart.Header.Descendants<Paragraph>(), replacements);
                }
            }

            // Traiter les pieds de page
            foreach (var footerPart in doc.MainDocumentPart?.FooterParts ?? Enumerable.Empty<FooterPart>())
            {
                if (footerPart.Footer != null)
                {
                    RemplacerDansParagraphes(footerPart.Footer.Descendants<Paragraph>(), replacements);
                }
            }
        }

        /// <summary>
        /// Remplace les placeholders dans une collection de paragraphes
        /// </summary>
        private void RemplacerDansParagraphes(IEnumerable<Paragraph> paragraphs, Dictionary<string, string> replacements)
        {
            foreach (var paragraph in paragraphs)
            {
                // Récupérer tous les runs du paragraphe
                var runs = paragraph.Descendants<Run>().ToList();
                if (!runs.Any()) continue;

                // Reconstituer le texte complet du paragraphe
                string fullText = string.Concat(runs.SelectMany(r => r.Descendants<Text>()).Select(t => t.Text));

                // Normaliser les placeholders (supprimer espaces: "<< NomClient >>" -> "<<NomClient>>")
                fullText = NormaliserPlaceholders(fullText);

                // Vérifier si le paragraphe contient des placeholders
                bool hasPlaceholder = false;
                foreach (var kvp in replacements)
                {
                    if (fullText.Contains(kvp.Key))
                    {
                        hasPlaceholder = true;
                        fullText = fullText.Replace(kvp.Key, kvp.Value);
                    }
                }

                // Si des remplacements ont été effectués, recréer le contenu
                if (hasPlaceholder)
                {
                    // Conserver le premier run avec son formatage
                    var firstRun = runs.FirstOrDefault();
                    if (firstRun != null)
                    {
                        // Copier les propriétés du premier run
                        var runProps = firstRun.GetFirstChild<RunProperties>()?.CloneNode(true) as RunProperties;

                        // Supprimer tous les runs existants
                        foreach (var run in runs)
                        {
                            run.Remove();
                        }

                        // Créer un nouveau run avec le texte remplacé
                        var newRun = new Run();
                        if (runProps != null)
                        {
                            newRun.Append(runProps);
                        }
                        newRun.Append(new Text(fullText) { Space = SpaceProcessingModeValues.Preserve });

                        paragraph.Append(newRun);
                    }
                }
            }
        }

        /// <summary>
        /// Normalise les placeholders en supprimant les espaces superflus
        /// Ex: "<< NomClient >>" -> "<<NomClient>>"
        /// </summary>
        private string NormaliserPlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Pattern pour trouver les placeholders avec espaces possibles
            // Remplace "<< ", "< <", " >>" etc.
            var result = text;

            // Supprimer les espaces après <<
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<<\s+", "<<");

            // Supprimer les espaces avant >>
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+>>", ">>");

            // Gérer le cas où < et < sont séparés
            result = result.Replace("< <", "<<").Replace("> >", ">>");

            return result;
        }

        /// <summary>
        /// Construit le dictionnaire de tous les remplacements
        /// </summary>
        private Dictionary<string, string> ConstruireDictionnaireReplacements(AmortissementResultViewModel data)
        {
            var client = data.ClientInfo ?? new Dictionary<string, string>();

            // Date du jour
            string dateAuj = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            // Dates de prêt
            string dateDebut = data.DateDebut.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            string dateFin = data.DateDebut.AddMonths(data.NombreEcheances).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            string dateDepart = data.DateDeblocage?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? dateDebut;

            // Montants
            string montantPret = FormatMontant(data.MontantPret);
            string mensualite = FormatMontant(data.MensualiteHT);
            string assurance = FormatMontant(data.AssuranceParEcheance);
            string fraisDossier = FormatMontant(data.FraisDossier);

            // Capacité de remboursement max
            decimal capaciteMax = data.SalaireMensuel > 0 ? data.SalaireMensuel * 0.5m : 0;

            // Nom complet client
            string nomClient = $"{client.GetValueOrDefault("Nom", "")} {client.GetValueOrDefault("Prenom", "")}".Trim();
            string nomClientMaj = nomClient.ToUpper();

            // Nom utilisateur connecté (session)
            string nomUtilisateur = HttpContext.Current.Session["accountName"]?.ToString() ?? "Utilisateur";

            var dict = new Dictionary<string, string>
            {
                // Dates
                { "<<DateAuj>>", dateAuj },
                { "<<DateDebut>>", dateDebut },
                { "<<DateFin>>", dateFin },
                { "<<DateDepart>>", dateDepart },
                { "<<DateEmbauche>>", client.GetValueOrDefault("DateEmbauche", "") },

                // Client - Identité
                { "<<NomClient>>", nomClient },
                { "<<NOMCLIENT>>", nomClientMaj },
                { "<<Nom>>", client.GetValueOrDefault("Nom", "") },
                { "<<Prenom>>", client.GetValueOrDefault("Prenom", "") },
                { "<<DateNaissance>>", client.GetValueOrDefault("DateNaissance", "") },
                { "<<LieuNaissance>>", client.GetValueOrDefault("LieuNaissance", "") },
                { "<<NumeroCompte>>", client.GetValueOrDefault("NumeroCompte", "") },

                // Client - Coordonnées
                { "<<ADRESSE>>", client.GetValueOrDefault("Adresse", "") },
                { "<<Adresse>>", client.GetValueOrDefault("Adresse", "") },
                { "<<Ville>>", client.GetValueOrDefault("Ville", "") },
                { "<<Telephone>>", client.GetValueOrDefault("Telephone", "") },
                { "<<Nationalite>>", client.GetValueOrDefault("Nationalite", "") },
                { "<<NATIONALITE>>", client.GetValueOrDefault("Nationalite", "").ToUpper() },

                // Client - Emploi
                { "<<Profession>>", client.GetValueOrDefault("Profession", "") },
                { "<<PROFESSION>>", client.GetValueOrDefault("Profession", "").ToUpper() },
                { "<<Employeur>>", client.GetValueOrDefault("Employeur", "") },
                { "<<EMPLOYEUR>>", client.GetValueOrDefault("Employeur", "").ToUpper() },
                { "<<SalaireMensuel>>", FormatMontant(data.SalaireMensuel) },
                { "<<SituationMatrimoniale>>", client.GetValueOrDefault("SituationMatrimoniale", "") },

                // Client - Pièce d'identité
                { "<<TypePieceIdentite>>", client.GetValueOrDefault("TypePieceIdentite", "") },
                { "<<NumeroPieceIdentite>>", client.GetValueOrDefault("NumeroPieceIdentite", "") },
                { "<<DateDelivrancePiece>>", client.GetValueOrDefault("DateDelivrancePiece", "") },
                { "<<LieuDelivrancePiece>>", client.GetValueOrDefault("LieuDelivrancePiece", "") },
                { "<<DateExpirationPiece>>", client.GetValueOrDefault("DateExpirationPiece", "") },

                // Prêt
                { "<<MONTANTPRET>>", montantPret },
                { "<<MontantPret>>", montantPret },
                { "<<MONTANTPRETENLETTRE>>", ConvertirMontantEnLettres(data.MontantPret) },
                { "<<TauxAnnuel>>", data.TauxAnnuel.ToString("0.00", CultureInfo.InvariantCulture) + "%" },
                { "<<TauxTTC>>", data.TauxTTC.ToString("0.00", CultureInfo.InvariantCulture) + "%" },
                { "<<NombreEcheances>>", data.NombreEcheances.ToString() },
                { "<<DureeEnAnnees>>", (data.NombreEcheances / 12).ToString() },
                { "<<Mensualite>>", mensualite },
                { "<<MENSUALITEENLETTRE>>", ConvertirMontantEnLettres(data.MensualiteHT) },
                { "<<ObjetCredit>>", data.ObjetCredit ?? "" },
                { "<<OBJETCREDIT>>", (data.ObjetCredit ?? "").ToUpper() },

                // Frais et totaux
                { "<<FraisDossier>>", fraisDossier },
                { "<<Assurance>>", assurance },
                { "<<TotalInterets>>", FormatMontant(data.TotalInterets) },
                { "<<TotalTAF>>", FormatMontant(data.TotalTAF) },
                { "<<TotalAssurance>>", FormatMontant(data.TotalAssurance) },
                { "<<CoutTotalCredit>>", FormatMontant(data.CoutTotalCredit) },
                { "<<TEGCalcule>>", data.TEGCalcule.ToString("0.00", CultureInfo.InvariantCulture) + "%" },
                { "<<CapaciteRemboursementMax>>", FormatMontant(capaciteMax) },
                { "<<TauxEndettement>>", data.TauxEndettement.ToString("0.00", CultureInfo.InvariantCulture) + "%" },

                // Utilisateur
                { "<<NOMPRENOMDEUTILISATEURCONNECTE>>", nomUtilisateur }
            };

            return dict;
        }

        /// <summary>
        /// Insère le tableau d'amortissement dans le document Word
        /// </summary>
        private void InsererTableauAmortissement(WordprocessingDocument doc, AmortissementResultViewModel data)
        {
            var body = doc.MainDocumentPart.Document.Body;

            // Chercher le marqueur où insérer le tableau (par exemple après "TABLEAU D'AMORTISSEMENT")
            var paragraphs = body.Descendants<Paragraph>().ToList();
            Paragraph insertAfter = null;

            foreach (var p in paragraphs)
            {
                var text = p.InnerText;
                if (text.Contains("TABLEAU D'AMORTISSEMENT") || text.Contains("tableau d'amortissement"))
                {
                    insertAfter = p;
                    break;
                }
            }

            if (insertAfter == null)
            {
                // Si pas de marqueur trouvé, insérer à la fin
                insertAfter = paragraphs.LastOrDefault();
            }

            if (insertAfter != null)
            {
                // Créer le tableau
                Table table = CreerTableauAmortissement(data);

                // Insérer après le paragraphe trouvé
                insertAfter.InsertAfterSelf(table);

                // Ajouter un saut de page avant le tableau si nécessaire
                insertAfter.InsertAfterSelf(new Paragraph(new Run(new Break() { Type = BreakValues.Page })));
            }
        }

        /// <summary>
        /// Crée le tableau d'amortissement au format Word
        /// </summary>
        private Table CreerTableauAmortissement(AmortissementResultViewModel data)
        {
            Table table = new Table();

            // Propriétés du tableau avec bordures fines
            TableProperties tblProp = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = "000000" },
                    new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = "000000" },
                    new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = "000000" },
                    new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = "000000" },
                    new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 2, Color = "CCCCCC" },
                    new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 2, Color = "CCCCCC" }
                ),
                new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableLayout() { Type = TableLayoutValues.Fixed }
            );
            table.AppendChild(tblProp);

            // Définir les largeurs de colonnes (en pourcentage approximatif)
            // Pmt(5%), Balance Début(12%), Balance Fin(12%), Principal(10%), Int TTC(10%), Int HT(10%), TPS(8%), Échéance(12%), Mois(8%), Année(8%)
            TableGrid grid = new TableGrid();
            int[] colWidths = { 500, 1100, 1100, 1000, 1000, 1000, 800, 1100, 700, 700 };
            foreach (var width in colWidths)
            {
                grid.Append(new GridColumn() { Width = width.ToString() });
            }
            table.Append(grid);

            // En-tête du tableau
            TableRow headerRow = new TableRow();
            // Hauteur fixe pour l'en-tête
            headerRow.Append(new TableRowProperties(new TableRowHeight() { Val = 400, HeightType = HeightRuleValues.AtLeast }));

            string[] headers = { "N°", "Capital Début", "Capital Fin", "Principal", "Intérêt TTC", "Intérêt HT", "TPS", "Échéance", "Mois", "Année" };

            foreach (var header in headers)
            {
                headerRow.Append(CreerCelluleEntete(header));
            }
            table.Append(headerRow);

            // Lignes de données avec alternance de couleurs
            if (data.LignesAmortissement != null)
            {
                int numero = 1;
                foreach (var ligne in data.LignesAmortissement)
                {
                    bool ligneImpaire = (numero % 2 == 1);
                    TableRow row = new TableRow();
                    row.Append(new TableRowProperties(new TableRowHeight() { Val = 280, HeightType = HeightRuleValues.AtLeast }));

                    DateTime dateEcheance = data.DateDebut.AddMonths(numero - 1);

                    row.Append(CreerCellule(numero.ToString(), false, ligneImpaire, JustificationValues.Center));
                    row.Append(CreerCellule(FormatMontant(ligne.CapitalRestantDebut), false, ligneImpaire));
                    row.Append(CreerCellule(FormatMontant(ligne.CapitalRestantFin), false, ligneImpaire));
                    row.Append(CreerCellule(FormatMontant(ligne.CapitalRembourse), false, ligneImpaire));
                    row.Append(CreerCellule(FormatMontant(ligne.Interets + ligne.TAF), false, ligneImpaire));
                    row.Append(CreerCellule(FormatMontant(ligne.Interets), false, ligneImpaire));
                    row.Append(CreerCellule(FormatMontant(ligne.TAF), false, ligneImpaire));
                    row.Append(CreerCellule(FormatMontant(ligne.Mensualite), false, ligneImpaire));
                    row.Append(CreerCellule(dateEcheance.ToString("MMM", new CultureInfo("fr-FR")), false, ligneImpaire, JustificationValues.Center));
                    row.Append(CreerCellule(dateEcheance.Year.ToString(), false, ligneImpaire, JustificationValues.Center));

                    table.Append(row);
                    numero++;
                }
            }

            // Ligne de total avec fond gris foncé
            TableRow totalRow = new TableRow();
            totalRow.Append(new TableRowProperties(new TableRowHeight() { Val = 350, HeightType = HeightRuleValues.AtLeast }));

            decimal totalCapital = data.LignesAmortissement?.Sum(l => l.CapitalRembourse) ?? 0;
            decimal totalInteretTTC = data.LignesAmortissement?.Sum(l => l.Interets + l.TAF) ?? 0;
            decimal totalInteretHT = data.LignesAmortissement?.Sum(l => l.Interets) ?? 0;
            decimal totalTPS = data.LignesAmortissement?.Sum(l => l.TAF) ?? 0;
            decimal totalEcheances = data.LignesAmortissement?.Sum(l => l.Mensualite) ?? 0;

            totalRow.Append(CreerCelluleTotal("TOTAL"));
            totalRow.Append(CreerCelluleTotal(""));
            totalRow.Append(CreerCelluleTotal(""));
            totalRow.Append(CreerCelluleTotal(FormatMontant(totalCapital)));
            totalRow.Append(CreerCelluleTotal(FormatMontant(totalInteretTTC)));
            totalRow.Append(CreerCelluleTotal(FormatMontant(totalInteretHT)));
            totalRow.Append(CreerCelluleTotal(FormatMontant(totalTPS)));
            totalRow.Append(CreerCelluleTotal(FormatMontant(totalEcheances)));
            totalRow.Append(CreerCelluleTotal(""));
            totalRow.Append(CreerCelluleTotal(""));

            table.Append(totalRow);

            return table;
        }

        /// <summary>
        /// Crée une cellule d'en-tête avec style professionnel
        /// </summary>
        private TableCell CreerCelluleEntete(string texte)
        {
            TableCell cell = new TableCell();

            TableCellProperties cellProp = new TableCellProperties(
                new Shading() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "2C3E50" },
                new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center }
            );
            cell.Append(cellProp);

            Run run = new Run(new Text(texte));
            RunProperties runProp = new RunProperties();
            runProp.Append(new Bold());
            runProp.Append(new Color() { Val = "FFFFFF" });
            runProp.Append(new FontSize() { Val = "16" }); // 8pt
            runProp.Append(new RunFonts() { Ascii = "Arial", HighAnsi = "Arial" });
            run.PrependChild(runProp);

            Paragraph para = new Paragraph(run);
            ParagraphProperties paraProp = new ParagraphProperties(
                new Justification() { Val = JustificationValues.Center },
                new SpacingBetweenLines() { After = "0", Before = "0" }
            );
            para.PrependChild(paraProp);

            cell.Append(para);
            return cell;
        }

        /// <summary>
        /// Crée une cellule normale avec options de style
        /// </summary>
        private TableCell CreerCellule(string texte, bool gras = false, bool fondAlterne = false, JustificationValues? alignement = null)
        {
            TableCell cell = new TableCell();

            TableCellProperties cellProp = new TableCellProperties(
                new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center }
            );
            if (fondAlterne)
            {
                cellProp.Append(new Shading() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "F8F9FA" });
            }
            cell.Append(cellProp);

            Run run = new Run(new Text(texte));
            RunProperties runProp = new RunProperties();
            runProp.Append(new FontSize() { Val = "16" }); // 8pt
            runProp.Append(new RunFonts() { Ascii = "Arial", HighAnsi = "Arial" });
            if (gras)
            {
                runProp.Append(new Bold());
            }
            run.PrependChild(runProp);

            Paragraph para = new Paragraph(run);
            ParagraphProperties paraProp = new ParagraphProperties(
                new Justification() { Val = alignement ?? JustificationValues.Right },
                new SpacingBetweenLines() { After = "0", Before = "0" }
            );
            para.PrependChild(paraProp);

            cell.Append(para);
            return cell;
        }

        /// <summary>
        /// Crée une cellule pour la ligne de total
        /// </summary>
        private TableCell CreerCelluleTotal(string texte)
        {
            TableCell cell = new TableCell();

            TableCellProperties cellProp = new TableCellProperties(
                new Shading() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "2C3E50" },
                new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center }
            );
            cell.Append(cellProp);

            Run run = new Run(new Text(texte));
            RunProperties runProp = new RunProperties();
            runProp.Append(new Bold());
            runProp.Append(new Color() { Val = "FFFFFF" });
            runProp.Append(new FontSize() { Val = "16" }); // 8pt
            runProp.Append(new RunFonts() { Ascii = "Arial", HighAnsi = "Arial" });
            run.PrependChild(runProp);

            Paragraph para = new Paragraph(run);
            ParagraphProperties paraProp = new ParagraphProperties(
                new Justification() { Val = JustificationValues.Right },
                new SpacingBetweenLines() { After = "0", Before = "0" }
            );
            para.PrependChild(paraProp);

            cell.Append(para);
            return cell;
        }

        /// <summary>
        /// Formate un montant avec séparateurs de milliers
        /// </summary>
        private string FormatMontant(decimal montant)
        {
            return montant.ToString("N0", new CultureInfo("fr-FR"));
        }

        /// <summary>
        /// Convertit un montant en lettres (français)
        /// </summary>
        private string ConvertirMontantEnLettres(decimal montant)
        {
            // Arrondir à l'entier le plus proche
            long montantEntier = (long)Math.Round(montant);
            return NombreEnLettres.ConvertirFrancsCFA(montantEntier);
        }
    }

    /// <summary>
    /// Extension pour Dictionary
    /// </summary>
    public static class DictionaryExtensions
    {
        public static string GetValueOrDefault(this Dictionary<string, string> dict, string key, string defaultValue = "")
        {
            return dict.ContainsKey(key) ? dict[key] : defaultValue;
        }
    }
}
