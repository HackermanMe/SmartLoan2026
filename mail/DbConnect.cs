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
using Oracle.ManagedDataAccess.Types;
using Oracle.ManagedDataAccess.Client;

namespace EcoService.Models
{
    public class DbConnect
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static OracleConnection Connect()
        {
            
            //string connectingString = string.Format("dsn={0};UID={1};PWD={2};", "TNSNAME", "KAPEDOH", "PASSWORD");
            string connectingString = "Data Source =192.168.1.29:1521/WINNER;User Id=hr; Password=hr;Pooling=false; ";

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




            //string connectingString = string.Format("dsn={0};UID={1};PWD={2};", "TNSNAME", "KAPEDOH", "PASSWORD");
            string connectingString = string.Format("dsn={0};UID={1};PWD={2};", "WINNER", "GEDO", "gdosystem");


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
