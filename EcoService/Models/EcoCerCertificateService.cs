using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iText.Layout;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.Globalization;
using System.IO;
using System.Web.Mvc;
using Org.BouncyCastle.Utilities;
//using iText.Bouncycastle.Crypto;

namespace EcoService.Models
{
    public class EcoCerCertificateService
    {
        private readonly EcoCerDbUtility _db;

        public EcoCerCertificateService()
        {
            _db = new EcoCerDbUtility();
        }
        public EcoCerCertificateService(EcoCerDbUtility db)
        {
            _db = db;
        }

        //public string FormatDateString(string dateToFormat)
        //{

        //    DateTime dateTimeFormatDate = DateTime.Parse(dateToFormat);
        //    string formattedDate = dateTimeFormatDate.ToString("dd MMMM yyyy", new CultureInfo("fr-FR"));
        //    return formattedDate;
        //}

        //public string FormatDateString(string dateToFormat)
        //{
        //    System.Diagnostics.Debug.WriteLine($"Date to format: {dateToFormat}");

        //    string[] formats = { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "M/d/yyyy", "d/M/yyyy" };
        //    DateTime convertedDate;
        //    string formattedDate;

        //    bool success = DateTime.TryParseExact(dateToFormat.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out convertedDate);

        //    if (success)
        //    {
        //        formattedDate = convertedDate.ToString("dd MMMM yyyy", new CultureInfo("fr-FR"));
        //        return formattedDate;
        //    }
        //    else
        //    {
        //        System.Diagnostics.Debug.WriteLine("Date format invalid.");
        //        return null;
        //    }

        //}

        //Méthode de formattage de date
        public string FormatDateString(string dateToFormat, string formatType)
        {
            System.Diagnostics.Debug.WriteLine($"Date to format: {dateToFormat}");

            string[] formats = { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "M/d/yyyy", "d/M/yyyy" };
            DateTime convertedDate;
            string formattedDate;

            bool success = DateTime.TryParseExact(dateToFormat.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out convertedDate);

            if (success && formatType != "")
            {
                formattedDate = convertedDate.ToString(formatType, new CultureInfo("fr-FR"));
                return formattedDate;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Date format invalid.");
                return null;
            }

        }

        public string GenerateReferenceNumber()
        {
            //Récupération des infos de référence de la dernière attestation créée
            const string startRefNumber = "001";
            int currentYear = DateTime.Now.Year;
            int lastYear = currentYear - 1;
            /*Console.WriteLine("Last year: " + lastYear);*/
            var lastCertificateReferences = _db.GetLastCertificateReferences(lastYear).Rows.Count == 0 ? _db.GetLastCertificateReferences(currentYear) : _db.GetLastCertificateReferences(lastYear);
            /*Console.WriteLine("Rows count: " + lastCertificateReferences.Rows.Count);
            Console.WriteLine("Last certificate Id: " + Convert.ToInt32(lastCertificateReferences.Rows[0]["CdmId"]));*/

            if (lastCertificateReferences.Rows.Count > 0)
            {
                string refNumber = lastCertificateReferences.Rows[0]["RefNumber"].ToString();
                /*Console.WriteLine("Reference number: " + refNumber);*/
                int creationYear = Convert.ToInt32(lastCertificateReferences.Rows[0]["CreationYear"]);
                /*Console.WriteLine("CreationYear: " + creationYear);*/

                //Extraction de l'année depuis le format string
                string creationDateString = lastCertificateReferences.Rows[0]["CreationDate"].ToString();


                //int extractedCreationYear = DateTime.ParseExact(creationDateString, "dd/MM/yyyy", CultureInfo.InvariantCulture).Year;


                //int extractedCreationYear = Convert.ToDateTime(lastCertificateReferences.Rows[0]["CreationDate"]).Year;
                /*Console.WriteLine("CreationYear extracted from CreationDate: " + extractedCreationYear);*/

                if (creationYear < currentYear)
                {
                    return startRefNumber;
                }
                else
                {
                    /*Console.WriteLine("Second phase of the if statement");*/
                    int nextRefNumber = int.Parse(refNumber) + 1;
                    /*Console.WriteLine("Next reference number: " + nextRefNumber);*/
                    return nextRefNumber.ToString($"D{startRefNumber.Length}");
                    /*return nextRefNumber.ToString("D3");*/
                    /*return refNumber == "000" ? "001" : nextRefNumber.ToString("D3");*/

                }
            }
            /*Console.WriteLine("End of method");*/
            return startRefNumber;
        }


        public byte[] GenerateCertificatePdf(EcoCerCertificateDataModel certificateModel)
        {
            var certificateTemplate = _db.GetTemplate();

            var bolderFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.TIMES_BOLD);
            var bodyFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.TIMES_ROMAN);
            string title = certificateModel.Civilite.Equals("Monsieur", StringComparison.OrdinalIgnoreCase) ? "Monsieur" : "Madame";
            string emp = certificateModel.Sexe.Equals("Homme", StringComparison.OrdinalIgnoreCase) ? "employé" : "employée";
            string definiteArticle = certificateModel.CategorieProfessionnelle.Equals("Agent de banque", StringComparison.OrdinalIgnoreCase) ? "d'" : "de ";
            //string? formattedRecruitmentDate = FormatDateString(certificateModel.DateRecrutement)?? "Date Invalide";
            string? formattedRecruitmentDate = FormatDateString(certificateModel.DateRecrutement, "dd MMMM yyyy") ?? "Date Invalide";

            //string? formattedRecruitmentDate = FormatDateString(certificateModel.DateRecrutement) ?? "Date Invalide";
            //string? formattedRecruitmentDate = certificateModel.DateRecrutement?.ToString("dd MMMM yyyy", new CultureInfo("fr-FR"));
            string? todayDate = DateTime.Now.ToString("dd MMMM yyyy", new CultureInfo("fr-FR"));
            //string? creationDate = certificateModel.CreationDate;
            //string formattedCreationDate = FormatDateString(certificateModel.CreationDate);
            string? creationDate = certificateModel.CreationDate?.ToString("dd MMMM yyyy", new CultureInfo("fr-FR"));
            string? referenceNumber = certificateModel.RefNumber;
            int currentYear = DateTime.Now.Year;

            //var bolderFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.TIMES_BOLD);
            //var bodyFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.TIMES_ROMAN);

            //string bodyTextPart1 = $"{certificateTemplate?.BodyTextPart1} {title} ";
            //Text boldName = new Text($"{certificateModel.Nom.Trim()} {certificateModel.Prenom.Trim()} ").SetFont(bolderFont);
            //Text boldRecruitmentDate = new Text($"{formattedRecruitmentDate} ").SetFont(bolderFont);
            //string bodyTextPart2 = $"{certificateTemplate?.BodyTextPart2} {emp} {certificateTemplate?.BodyTextPart3} ";
            //string bodyTextPart3 = $"{certificateTemplate?.BodyTextPart4} {definiteArticle}{certificateModel.CategorieProfessionnelle}.";
            Text bodyTextPart1 = new Text($"{certificateTemplate?.BodyTextPart1} {title} ").SetFont(bodyFont);
            Text boldName = new Text($"{certificateModel.Nom.Trim()} {certificateModel.Prenom.Trim()} ").SetFont(bolderFont);
            Text boldRecruitmentDate = new Text($"{formattedRecruitmentDate} ").SetFont(bolderFont);

            //Extraction des parties de la date de recrutement
            //System.Diagnostics.Debug.WriteLine($"Formatted date: {formattedRecruitmentDate}");

            string[] formattedRecruitmentDateParts = formattedRecruitmentDate.Split(' ');

            string recruitmentDateDay = formattedRecruitmentDateParts[0];

            string recruitmentDateMonth = formattedRecruitmentDateParts[1];

            string recruitmentDateYear = formattedRecruitmentDateParts[2];

            Text one = new Text("1").SetFont(bolderFont);
            Text er = new Text("er").SetTextRise(6).SetFontSize(8).SetFont(bolderFont);
            Text month = new Text($" {recruitmentDateMonth}").SetFont(bolderFont);
            Text year = new Text($" {recruitmentDateYear} ").SetFont(bolderFont);
            Text bodyTextPart2 = new Text($"{certificateTemplate?.BodyTextPart2} {emp} {certificateTemplate?.BodyTextPart3} ").SetFont(bodyFont);
            Text bodyTextPart3 = new Text($"{certificateTemplate?.BodyTextPart4} {definiteArticle}{certificateModel.CategorieProfessionnelle}.").SetFont(bodyFont);


            using (var memoryStream = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(memoryStream);
                PdfDocument pdf = new PdfDocument(writer);
                pdf.SetDefaultPageSize(PageSize.A4);
                Document document = new Document(pdf);

                float margin = 75f;
                document.SetMargins(margin, margin, margin, margin);



                Paragraph reference = new Paragraph($"{certificateTemplate?.HeaderText}/{referenceNumber}/{currentYear}")
                    .SetFont(bodyFont)
                    .SetFontSize(14)
                    .SetMarginTop(85)
                    .SetMarginBottom(65)
                    .SetTextAlignment(TextAlignment.LEFT);
                document.Add(reference);


                Paragraph header = new Paragraph(certificateTemplate?.TitleText)
                    .SetFontSize(20)
                    .SetFont(bolderFont)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetUnderline()
                    .SetMarginBottom(20);
                document.Add(header);

                //Paragraph paragragh = new Paragraph().Add(bodyTextPart1).Add(boldName).Add(bodyTextPart2).Add(boldRecruitmentDate).Add(bodyTextPart3)
                Paragraph paragragh = recruitmentDateDay == "01" ? new Paragraph()
                                              .Add(bodyTextPart1)
                                              .Add(boldName)
                                              .Add(bodyTextPart2)
                                              .Add(one)
                                              .Add(er)
                                              .Add(month)
                                              .Add(year)
                                              .Add(bodyTextPart3)
                                              .SetFontSize(14)
                                              .SetTextAlignment(TextAlignment.JUSTIFIED) : new Paragraph().Add(bodyTextPart1).Add(boldName).Add(bodyTextPart2).Add(boldRecruitmentDate).Add(bodyTextPart3)

                                              .SetFontSize(14)
                                                     .SetTextAlignment(TextAlignment.JUSTIFIED);

                Table infoTable = new Table(2);

                infoTable.AddCell(new Cell(1, 2).Add(paragragh).SetBorder(Border.NO_BORDER));

                string? additionalBodyText = certificateTemplate?.BodyTextPart5;

                infoTable.AddCell(new Cell(1, 2).Add(new Paragraph(additionalBodyText).SetMarginTop(30)
                                                .SetFont(bodyFont)
                                                .SetFontSize(14)
                                                .SetTextAlignment(TextAlignment.JUSTIFIED))
                                                .SetBorder(Border.NO_BORDER));


                document.Add(infoTable);

                //string bodyEndText = $"{certificateTemplate?.DeliverDateText} {creationDate}";
                string bodyEndText = $"{certificateTemplate?.DeliverDateText} {todayDate}";

                //string bodyEndText = $"{certificateTemplate?.DeliverDateText} {formattedCreationDate}";
                Paragraph bodyEndTextParagraph = new Paragraph(bodyEndText)
                    .SetFont(bodyFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginTop(40)
                    .SetMarginBottom(60);
                document.Add(bodyEndTextParagraph);


                Paragraph footerFirstParagraph = new Paragraph(certificateTemplate?.FooterTextPart1)
                    .SetFont(bolderFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.JUSTIFIED)
                    //.SetTextAlignment(TextAlignment.LEFT)
                    .SetMarginTop(10)
                    .SetMarginBottom(2);
                document.Add(footerFirstParagraph);

                Paragraph footerSecondParagraph = new Paragraph(certificateTemplate?.FooterTextPart2)
                    .SetFont(bodyFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.JUSTIFIED)
                    //.SetTextAlignment(TextAlignment.LEFT)
                    .SetMarginTop(0);
                document.Add(footerSecondParagraph);

                document.Close();

                return memoryStream.ToArray();
            }
        }

        //Methode pour permettre au serveur IIS apres deploiement de recuperer le fichier PDF
        public string DownloadCertificatePdf(EcoCerCertificateDataModel certificateModel)
        {
            if (!Directory.Exists(Tools.folderPath))
            {
                Directory.CreateDirectory(Tools.folderPath);
            }

            string fileName = $"Attestation-{certificateModel.Nom}_{certificateModel.Prenom}.pdf";
            string filePath = System.IO.Path.Combine(Tools.folderPath, fileName.Trim());

            byte[] pdfBytes = GenerateCertificatePdf(certificateModel);
            File.WriteAllBytes(filePath, pdfBytes);

            return filePath;
        }

    }
}
