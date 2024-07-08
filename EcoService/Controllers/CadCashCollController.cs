using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EcoService.Models;
using System.Data.Odbc;
using System.IO;
using System.Globalization;
using System.Web.UI.WebControls;
using NLog;
//using System.Data.OracleClient;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Text;
using NPOI.HPSF;
using System.Web.Configuration;

namespace EcoService.Controllers
{
    public class CadCashCollController : Controller
    {


        public ActionResult ExtraireExcel()
        {

            String Action = Request["action"];
            String startDate = Request["beginDate"];
            String endDate = Request["endDate"];

            LoadCompte Lr = new LoadCompte();

            OracleDataReader DbReader = Lr.CadCashColl();

            int ver = 1;
            String Rapport = "CadCashColl";


            var filedetail = Rapport + "_" + startDate + "_au_" + endDate;
            string filename2 = Tools.FileName(ver, filedetail);
            String folderPath = WebConfigurationManager.AppSettings["ExportPath"].ToString();

            string path2 = folderPath + filename2 + ".xlsx";


            var grid = new GridView();
            grid.DataSource = DbReader;
            grid.DataBind();
            //int n = grid.Rows.Count;
            //Console.WriteLine("test  ok " + n);

            Tools.ExportExcel2tofolder(grid, filename2);

            //return File(path2, "application/txt", Server.UrlEncode(filename2 + ".xlsx"));

            return File(path2, "application/txt", Server.UrlEncode(filename2 + ".xlsx"));
        }

        public void ExtraireCsv()
        {

            String Action = Request["action"];
            String startDate = Request["beginDate"];
            String endDate = Request["endDate"];

            LoadCompte Lr = new LoadCompte();

            OracleDataReader DbReader = Lr.CadCashColl();

            var grid = new GridView();
            grid.DataSource = DbReader;
            //grid.DataBind();

            int ver = 1;
            String Rapport = "CadCashColl";

            var filedetail = Rapport + "_au_" + endDate;
            string filename = Tools.FileName(ver, filedetail);

            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment;filename=" + filename + ".csv");
            Response.Charset = "";
            Response.ContentType = "application/text";
            grid.AllowPaging = false;
            grid.DataBind();
            //int n = grid.Rows[0].Cells.Count;
            int n = grid.HeaderRow.Cells.Count;
            Console.WriteLine("test  ok " + n);
            StringBuilder columnbind = new StringBuilder();
            for (int k = 0; k < grid.HeaderRow.Cells.Count; k++)
            {
                //columnbind.Append(grid.Rows[0].Cells[k].Text + ',');
                columnbind.Append(grid.HeaderRow.Cells[k].Text + ',');
            }

            columnbind.Append("\r\n");
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                //for (int k = 0; k < grid.Columns.Count; k++)
                for (int k = 0; k < grid.Rows[i].Cells.Count; k++)
                {

                    columnbind.Append(grid.Rows[i].Cells[k].Text + ',');
                }

                columnbind.Append("\r\n");
            }

            Response.Output.Write(columnbind.ToString());
            Response.Flush();
            Response.End();

        }
    }
}