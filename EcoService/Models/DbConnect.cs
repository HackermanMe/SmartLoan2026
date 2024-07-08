using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Web;
//using System.Data.OracleClient;
using System.Web.Configuration;
using EcoService.Models;
using System.Data;
using NLog;
using Oracle.ManagedDataAccess.Client;
using System.Data.SqlClient;

namespace EcoService.Models
{
    public class DbConnect
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static OracleConnection ConnectOracle()
        {
        
           string connectingString = "DATA SOURCE=ADC-FWAFC-SCAN:1521/SRVFCUBSFWA; PERSIST SECURITY INFO=True;USER ID=*******; password=******; Pooling= False; ";

         

            OracleConnection con = new OracleConnection();
            con.ConnectionString = connectingString;

            // OdbcConnection con = new OdbcConnection(connectingString);
            try
            {

                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                    con.Open();
                }
                else
                {
                   //con.Close();
                    con.Open();
                }
            }
            catch (Exception Error)
            {


                logger.Log(LogLevel.Info, Error);

            }

            logger.Fatal(con.State);

            return con;

        }

        public static SqlConnection ConnectSql()
        {

            string connectingString = WebConfigurationManager.ConnectionStrings["SqlDbconnexion"].ConnectionString;

            SqlConnection con = new SqlConnection();
            con.ConnectionString = connectingString;

            // OdbcConnection con = new OdbcConnection(connectingString);
            try
            {

                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                    con.Open();
                }
                else
                {
                    con.Close();
                    con.Open();
                }
            }
            catch (Exception Error)
            {


                logger.Log(LogLevel.Info, Error);

            }

            logger.Fatal(con.State);

            return con;

        }



 
        public static OdbcConnection ConnectOdbc()
        {
            //OdbcConnection con = new OdbcConnection();




            string connectingString = string.Format("dsn={0};UID={1};PWD={2};", "TNSNAME", "KAPEDOH", "PASSWORD");
           


            OdbcConnection con = new OdbcConnection(connectingString);
            try
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                    con.Open();
                }
                else
                {
                    con.Open();
                }
            }
            catch (Exception Error)
            {


                logger.Log(LogLevel.Info, Error);

            }
            logger.Fatal(con.State);
            return con;

        }

    }
}
