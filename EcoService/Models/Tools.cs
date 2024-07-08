using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.IO;
using System.Net.Mail;
using System.Web;
using System.Web.Mail;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Runtime.InteropServices;
using Outlook = Microsoft.Office.Interop.Outlook;
using Office = Microsoft.Office.Core;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.Linq;
using System.Web.Configuration;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace EcoService.Models
{
    /// <summary>
    /// Contient les méthodes et fonctions qui permettent de générer
    /// des rapports en pdf, excel et csv
    /// </summary>
    public class Tools
    {
       
        public static HttpResponse response = HttpContext.Current.Response;
        //public static String folderPath = HttpContext.Current.Server.MapPath("~/App_Data/");
         //public static String folderPath = "\\\\10.6.120.9\\ExtractionFile\\";
        public static String folderPath = WebConfigurationManager.AppSettings["ExportPath"].ToString();


        /// <summary>
        /// Génère le rapport en excel et place le fichier dans le 
        /// repertoire App_Data sur le serveur
        /// </summary>
        /// <param name="fileName">le nom du fichier à générer (type : string)</param>
        ///  <param name="grid">tableau de données (type : GridView)</param>

        public static List<object[]> loadSQL(string query, string connectString)
        {
            List<object[]> dataList = new List<object[]>();

            using (SqlConnection connection = new SqlConnection(connectString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object[] tempRow = new object[reader.FieldCount];
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                tempRow[i] = reader[i];
                            }
                            dataList.Add(tempRow);
                        }
                    }
                }
            }
            return dataList;
        }

        public static string Versions(int Ver)
        {
            if (Ver.ToString().Length >= 2)
            {
                return Ver.ToString();
            }
            else
            {
                return "0" + Ver.ToString();
            }
        }

        public static string FileName(int version, string codeEtc)
        {
            string Fname = string.Empty;
            string DateFichier = DateTime.Now.ToString("ddMMyy");
            Fname = string.Format("{0}_{1}_{2}", codeEtc, DateFichier, Versions(version));
            return Fname;
        }


        public static void SendMail(string subject, string body, string chemin, string chemin2=null)
        {
            Outlook.Application application1 = new Outlook.Application();
            string smtpAddress;
            string to;
             smtpAddress = "KAPEDOH@ecobank.com";
           //smtpAddress = "ETGservice@ecobank.com";

            //to = "PKANTANE@ecobank.com;DCREPPY@ecobank.com;KAPEDOH@ecobank.com;ALLETG-ServiceClient@ecobank.com;alletg-it@ecobank.com";
            to = "KAPEDOH@ecobank.com";


            // Create a new MailItem and set the To, Subject, and Body properties.
            Outlook.MailItem newMail = (Outlook.MailItem)application1.CreateItem(Outlook.OlItemType.olMailItem);
            newMail.To = to;
            newMail.Subject = subject;
            newMail.Body = body;   
         
            newMail.Attachments.Add(chemin);
            if (chemin2 != null) { newMail.Attachments.Add(chemin2); }

            // Retrieve the account that has the specific SMTP address.
            Outlook.Account account = GetAccountForEmailAddress(application1, smtpAddress);
            // Use this account to send the e-mail.
            newMail.SendUsingAccount = account;
            newMail.Send();
        }


        public static Outlook.Account GetAccountForEmailAddress(Outlook.Application application, string smtpAddress)
        {

            // Loop over the Accounts collection of the current Outlook session.
            Outlook.Accounts accounts = application.Session.Accounts;
            foreach (Outlook.Account account in accounts)
            {
                // When the e-mail address matches, return the account.
                if (account.SmtpAddress == smtpAddress)
                {
                    return account;
                }
            }
            throw new System.Exception(string.Format("No Account with SmtpAddress: {0} exists!", smtpAddress));
        }

        public static void ExportExcelToFolder(string fileName, GridView grid)
        {
             fileName = folderPath + fileName + ".xls";
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
            StringWriter sw = new StringWriter();
            HtmlTextWriter tw = new HtmlTextWriter(sw);
            grid.RenderControl(tw);
            var bytes = System.Text.Encoding.UTF8.GetBytes(sw.ToString());
            fs.Write(bytes, 0, bytes.Length);
            fs.Close();

        }


        /// <summary>
        /// Génère le rapport en excel et créé le fichier 
        /// sur le poste client
        /// </summary>
        /// <param name="fileName">le nom du fichier à générer (type : string)</param>
        ///  <param name="grid">tableau de données (type : GridView)</param>
        public static void ExportToExcel(string fileName, GridView grid)
        {

            response.AddHeader("content-disposition", "attachment;filename="+ fileName + ".xlsx");
            response.ContentType = "application/vnd.ms-excel";
            StringWriter sw = new StringWriter();
            HtmlTextWriter tw = new HtmlTextWriter(sw);

            grid.RenderControl(tw);

            response.Write(sw.ToString());

            response.End();
        }


        public static FileStreamResult ExportPdfToFolder(string fileName, string data)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            using (var input = new MemoryStream(bytes))
            {
                // var output = new MemoryStream();
                fileName = folderPath + fileName + ".pdf";
                var output = new FileStream(fileName, FileMode.Create);
                var document = new Document(PageSize.A4, 50, 50, 50, 50);
                var writer = PdfWriter.GetInstance(document, output);
                writer.CloseStream = false;
                document.Open();

                var xmlWorker = iTextSharp.tool.xml.XMLWorkerHelper.GetInstance();
                xmlWorker.ParseXHtml(writer, document, input, System.Text.Encoding.UTF8);
                document.Close();
                output.Position = 0;
                return new FileStreamResult(output, "application/pdf");
            }
        }



        public static FileStreamResult ExportPdf(string fileName, string data)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            using (var input = new MemoryStream(bytes))
            {
                var output = new MemoryStream();            
                var document = new Document(PageSize.A4, 50, 50, 50, 50);
                var writer = PdfWriter.GetInstance(document, output);
                writer.CloseStream = false;
                document.Open();

                var xmlWorker = iTextSharp.tool.xml.XMLWorkerHelper.GetInstance();
                xmlWorker.ParseXHtml(writer, document, input, System.Text.Encoding.UTF8);
                document.Close();
                output.Position = 0;
                return new FileStreamResult(output, "application/pdf");
            }
        }



        public static void ExporttoExcelv(GridView dt, string extension, string fileName)
        {

            IWorkbook workbook;

            if (extension == "xlsx")
            {
                workbook = new XSSFWorkbook();
            }
            else if (extension == "xls")
            {
                workbook = new HSSFWorkbook();
            }
            else
            {
                throw new Exception("This format is not supported");
            }

            ISheet sheet1 = workbook.CreateSheet("Sheet 1");

            //make a header row
            IRow row1 = sheet1.CreateRow(0);

            for (int j = 0; j < dt.HeaderRow.Cells.Count; j++)
            {

                ICell cell = row1.CreateCell(j);
                String columnName = dt.HeaderRow.Cells[j].Text /*dt.Columns[j].ToString()*/;
                cell.SetCellValue(columnName);
            }

            //loops through data
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                IRow row = sheet1.CreateRow(i + 1);
                for (int j = 0; j < dt.Rows[i].Cells.Count; j++)
                {

                    ICell cell = row.CreateCell(j);
                    String columnName = dt.HeaderRow.Cells[j].Text;
                    cell.SetCellValue(dt.Rows[i].Cells[j].Text);
                }
            }

            using (var exportData = new MemoryStream())
            {
                response.Clear();
                workbook.Write(exportData);
                if (extension == "xlsx") //xlsx file format
                {
                    response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    response.AddHeader("Content-Disposition", string.Format("attachment;filename=" + fileName + ".xlsx"));
                    response.BinaryWrite(exportData.ToArray());
                }
                else if (extension == "xls")  //xls file format
                {
                    response.ContentType = "application/vnd.ms-excel";
                    response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}", fileName + ".xls"));
                    response.BinaryWrite(exportData.GetBuffer());
                }
                response.End();
            }
        }


        public static void ExportExcel2tofolder(GridView dt, string fileName)
        {

            fileName = folderPath + fileName + ".xlsx";
            using (FileStream stream = new FileStream(@fileName, FileMode.Create, FileAccess.Write))
            {
                IWorkbook wb = new XSSFWorkbook();
                ISheet sheet = wb.CreateSheet("Sheet1");
                ICreationHelper cH = wb.GetCreationHelper();

                //make a header row
                IRow row1 = sheet.CreateRow(0);

                for (int j = 0; j < dt.HeaderRow.Cells.Count; j++)
                {

                    ICell cell = row1.CreateCell(j);
                    String columnName = dt.HeaderRow.Cells[j].Text /*dt.Columns[j].ToString()*/;
                    cell.SetCellValue(columnName);
                }

                //loops through data

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    IRow row = sheet.CreateRow(i + 1);
                    for (int j = 0; j < dt.Rows[i].Cells.Count; j++)
                    {

                        ICell cell = row.CreateCell(j);
                        String columnName = dt.HeaderRow.Cells[j].Text;
                        cell.SetCellValue(dt.Rows[i].Cells[j].Text.Replace("&nbsp;", ""));

             

            }

            }

               
                wb.Write(stream);
            }

          
        }

        public static void CreateGView()

        {

        
        int ver = 1;


            string filename2 = Tools.FileName(ver, "0055");


            string path2 = "D:\\temp" + "\\" + filename2 + ".xlsx";
            //var r = Db.personcip.ToList();

            var grid = new GridView();
            //grid.DataSource = r;
            //grid.DataBind();
            
            ExportExcel2tofolder(grid, filename2);

        }



    }
}