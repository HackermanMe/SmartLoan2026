using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EcoService.Models;
using System;
using System.IO;

namespace EcoService.Services.Export
{
    public class BilletOrdreService
    {
        private const string DOTS_SHORT = "..........";
        private const string DOTS_MEDIUM = "....................";
        private const string DOTS_LONG = "........................................";

        public byte[] Generer(EtatPretsPersonnelInput inp, decimal mensualite, DateTime dateFin)
        {
            long montantArrondi = (long)Math.Round(inp.Montant);
            long mensualiteArrondie = (long)Math.Round(mensualite);
            string dateJour = DateTime.Today.ToString("dd/MM/yyyy");
            string dateDebut = inp.DateDebut.ToString("dd/MM/yyyy");
            string datFinStr = dateFin.ToString("dd/MM/yyyy");
            string montantFmt = FormatNombre(montantArrondi);
            string mensualiteFmt = FormatNombre(mensualiteArrondie);
            string echeancesLettres = NombreEnLettres.ConvertirMajuscule(inp.NbreEcheances);
            string montantLettres = NombreEnLettres.ConvertirMajuscule(montantArrondi);
            string mensualiteLettres = NombreEnLettres.ConvertirMajuscule(mensualiteArrondie);
            string nom = inp.NomDemandeur ?? "";
            string adresse = inp.Adresse ?? "";
            string bp = inp.BP ?? "";
            string tel = inp.Telephone ?? "";
            string compte = inp.NumeroCompte ?? "";

            using (var ms = new MemoryStream())
            {
                using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = new Body();

                    var secProps = new SectionProperties(
                        new PageMargin
                        {
                            Top = 1134,     // ~2 cm
                            Bottom = 1134,
                            Left = 1134,    // ~2 cm
                            Right = 1134
                        },
                        new PageSize
                        {
                            Width = 11906,  // A4
                            Height = 16838
                        }
                    );

                    // ══════════════════════════════════════════════════════════
                    //  TITRE : BILLET A ORDRE
                    // ══════════════════════════════════════════════════════════
                    body.Append(LigneVide());
                    body.Append(CreerParagraphe("BILLET A ORDRE", gras: true, taille: 28,
                        centre: true, souligne: false));
                    body.Append(LigneVide());

                    // ══════════════════════════════════════════════════════════
                    //  LIEU ET DATE (aligné à gauche comme l'original)
                    // ══════════════════════════════════════════════════════════
                    body.Append(CreerParagraphe($"LOME, LE {dateJour}"));
                    body.Append(LigneVide());

                    // ══════════════════════════════════════════════════════════
                    //  MONTANT POUR CONTROLE (avec pointillés)
                    // ══════════════════════════════════════════════════════════
                    body.Append(CreerParagraphe(
                        $"MONTANT POUR CONTROLE : XOF {DOTS_MEDIUM} {montantFmt} {DOTS_MEDIUM} + INTERETS ET TAF",
                        gras: true, taille: 22));

                    // ══════════════════════════════════════════════════════════
                    //  ECHEANCES (avec pointillés comme l'original)
                    // ══════════════════════════════════════════════════════════
                    var pEch = new Paragraph();

                    pEch.Append(CreerRun("Payable en ... "));
                    pEch.Append(CreerRun(echeancesLettres, gras: true));
                    pEch.Append(CreerRun($" ({inp.NbreEcheances}) ", gras: true));

                    pEch.Append(CreerRun("......mensualités de XOF......... "));
                    pEch.Append(CreerRun(mensualiteFmt, gras: true));

                    pEch.Append(CreerRun(" ("));
                    pEch.Append(CreerRun(mensualiteLettres, gras: true));
                    pEch.Append(CreerRun(" francs CFA) "));

                    pEch.Append(CreerRun(".........(En chiffres et en lettres)"));

                    body.Append(pEch);
                    body.Append(LigneVide());

                    // DU ... AU ... (avec pointillés)
                    var pDates = new Paragraph();
                    pDates.Append(CreerRun($"Du{DOTS_SHORT}"));
                    pDates.Append(CreerRun(dateDebut, gras: true));
                    pDates.Append(CreerRun($".......                    Au........."));
                    pDates.Append(CreerRun(datFinStr, gras: true));
                    pDates.Append(CreerRun(DOTS_SHORT));
                    body.Append(pDates);
                    body.Append(LigneVide());

                    // ══════════════════════════════════════════════════════════
                    //  CORPS DU BILLET (texte exact de l'original)
                    // ══════════════════════════════════════════════════════════
                    body.Append(CreerParagraphe("Je paierai contre le présent billet à ordre à"));
                    body.Append(LigneVide());

                    var pCorps = new Paragraph(
                        new ParagraphProperties(new Justification { Val = JustificationValues.Both }));

                    pCorps.Append(CreerRun("ECOBANK TOGO, 20 AVENUE SYLVANUS OLYMPIO, B.P. 3302 LOME-TOGO, "));
                    pCorps.Append(CreerRun("la somme de XOF "));
                    pCorps.Append(CreerRun(montantFmt, gras: true));
                    pCorps.Append(CreerRun(DOTS_SHORT));
                    pCorps.Append(CreerRun(" ("));
                    pCorps.Append(CreerRun(montantLettres, gras: true));
                    pCorps.Append(CreerRun(" francs FCFA) "));
                    pCorps.Append(CreerRun("(En chiffres et en lettres) + INTERETS ET TAF en "));
                    pCorps.Append(CreerRun("...... "));
                    pCorps.Append(CreerRun(echeancesLettres, gras: true));
                    pCorps.Append(CreerRun($" ({inp.NbreEcheances})", gras: true));
                    pCorps.Append(CreerRun("....mensualités de XOF................. "));
                    pCorps.Append(CreerRun(mensualiteFmt, gras: true));
                    pCorps.Append(CreerRun(" ("));
                    pCorps.Append(CreerRun(mensualiteLettres, gras: true));
                    pCorps.Append(CreerRun(" francs CFA)"));

                    body.Append(pCorps);
                    body.Append(LigneVide());
                    // body.Append(LigneVide());

                    // ══════════════════════════════════════════════════════════
                    //  RETENUES SUR COMPTE
                    // ══════════════════════════════════════════════════════════
                    var pRetenue = new Paragraph();
                    pRetenue.Append(CreerRun(
                        $"Retenues directement sur mon compte n°{DOTS_MEDIUM}"));
                    pRetenue.Append(CreerRun(compte, gras: true));
                    pRetenue.Append(CreerRun(DOTS_LONG));
                    body.Append(pRetenue);
                    body.Append(LigneVide());
                    // body.Append(LigneVide());

                    // ══════════════════════════════════════════════════════════
                    //  NOMS ET PRENOMS DU SOUSCRIPTEUR
                    // ══════════════════════════════════════════════════════════
                    var pNom = new Paragraph();
                    pNom.Append(CreerRun(
                        $"NOMS ET PRENOMS DU SOUSCRIPTEUR : {DOTS_MEDIUM} ", gras: true));
                    pNom.Append(CreerRun(nom, gras: true));
                    pNom.Append(CreerRun($" {DOTS_MEDIUM}"));
                    body.Append(pNom);
                    body.Append(LigneVide());
                    // body.Append(LigneVide());

                    // ══════════════════════════════════════════════════════════
                    //  ADRESSE DU SOUSCRIPTEUR + BP + TEL (une seule ligne)
                    // ══════════════════════════════════════════════════════════
                    var pAdr = new Paragraph();
                    pAdr.Append(CreerRun("ADRESSE DU SOUSCRIPTEUR : ", gras: true));
                    pAdr.Append(CreerRun(
                        string.IsNullOrWhiteSpace(adresse) ? DOTS_LONG : adresse));
                    pAdr.Append(CreerRun("    BP : "));
                    pAdr.Append(CreerRun(
                        string.IsNullOrWhiteSpace(bp) ? DOTS_SHORT : bp));
                    pAdr.Append(CreerRun("    Tél "));
                    pAdr.Append(CreerRun(
                        string.IsNullOrWhiteSpace(tel) ? DOTS_MEDIUM : tel));
                    body.Append(pAdr);
                    body.Append(LigneVide());
                    body.Append(LigneVide());

                    // ══════════════════════════════════════════════════════════
                    //  COMPTE DE DOMICILIATION
                    // ══════════════════════════════════════════════════════════
                    var pCompte = new Paragraph();
                    pCompte.Append(CreerRun(
                        $"COMPTE DE DOMICILIATION :{DOTS_LONG} ", gras: true));
                    pCompte.Append(CreerRun(compte, gras: true));
                    pCompte.Append(CreerRun(DOTS_LONG));
                    body.Append(pCompte);
                    // body.Append(LigneVide());
                    body.Append(LigneVide());
                    body.Append(LigneVide());
                    body.Append(LigneVide());

                    // ══════════════════════════════════════════════════════════
                    //  SIGNATURE
                    // ══════════════════════════════════════════════════════════
                    body.Append(CreerParagraphe("SIGNATURE :", gras: true));

                    body.Append(secProps);
                    mainPart.Document.Append(body);
                    mainPart.Document.Save();
                }

                return ms.ToArray();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════════

        private static Paragraph CreerParagraphe(string texte,
            bool gras = false, int taille = 22,
            bool centre = false, bool droite = false,
            bool souligne = false)
        {
            var pp = new ParagraphProperties();
            if (centre)
                pp.Append(new Justification { Val = JustificationValues.Center });
            else if (droite)
                pp.Append(new Justification { Val = JustificationValues.Right });

            var para = new Paragraph(pp);
            para.Append(CreerRun(texte, gras, taille, souligne));
            return para;
        }

        private static Run CreerRun(string texte,
            bool gras = false, int taille = 22,
            bool souligne = false)
        {
            var rp = new RunProperties();
            rp.Append(new RunFonts { Ascii = "Arial", HighAnsi = "Arial", ComplexScript = "Arial" });
            rp.Append(new FontSize { Val = taille.ToString() });
            rp.Append(new FontSizeComplexScript { Val = taille.ToString() });

            if (gras)
                rp.Append(new Bold());
            if (souligne)
                rp.Append(new Underline { Val = UnderlineValues.Single });

            var run = new Run(rp);
            run.Append(new Text(texte) { Space = SpaceProcessingModeValues.Preserve });
            return run;
        }

        private static Paragraph LigneVide()
        {
            return new Paragraph(new Run(new Text("")));
        }

        private static string FormatNombre(long nombre)
        {
            return nombre.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"))
                         .Replace((char)8239, ' ')
                         .Replace((char)160, ' ');
        }
    }
}
