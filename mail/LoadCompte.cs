using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Web;
using System.Data.Entity;
using DALE;
using Oracle.ManagedDataAccess.Client;

namespace EcoService.Models
{
    public class LoadCompte
    {
       
        public OdbcDataReader ListeClientPhysique(String FirstDate, String FirstDate2)
        {
              
        

            string querry = "   select distinct cust_no, cust_ac_no, ac_open_date, decode(first_name,'NAP',MIDDLE_NAME,first_name) First_Name   , SUBSTR2((middle_name||' '||decode(last_name,'NAP','',last_name)),0,30) last_name ," // 4
        + " SUBSTR2(field_val_4||' '||  field_val_5,0,30) Place_Birth, date_of_birth as Date_Nais, SUBSTR2(field_val_10||' ',0,30)  Mary," // 7
        + " SUBSTR2(field_val_7 || ' ' || field_val_8,0,30) Mother, passport_no,"  // 9
        + " NVL2(p_country,SUBSTR2(p_country, 0, 2),'V') Country_ID, sex, resident_status||' ' as residence, field_val_5 as Town_Residence" //13
        + " ,(Select Nationality from sttm_customer where  customer_no = cust_no) AS Nationality" // 14
        + " ,(Select NVL2(Country,Country,'V') from sttm_customer  where customer_no = cust_no) AS Country_Adress" //15
        + " ,(Select NVL2(p_address3,SUBSTR2(p_address3||' ',0,30),' ') from sttm_cust_personal  where  customer_no = cust_no) AS Town_Adress" //16
        + " ,(Select NVL2(p_address1,SUBSTR2(p_address1||' ',0,50),' ') from sttm_cust_personal  where customer_no = cust_no) AS Address"  //17                  
        + " ,(Select NVL2(passport_no,SUBSTR2(passport_no||' ',0,11),' ') from sttm_cust_personal  where customer_no = cust_no) As NumCNI" //18
        + " from fccetg.sttm_cust_account, fccetg.sttm_cust_personal, fccetg.CSTM_FUNCTION_USERDEF_FIELDS"
        + " where cust_no = customer_no"
        + " and substr(cust_ac_no,4, 3) in('010','011','026','024','081','083', '051', '085','086','097')"
        + "  and record_stat = 'O'"
        + "  and auth_stat = 'A'"
        + "  and substr(rec_key,1, 8)= customer_no(+)"
        + "  and ac_open_date between to_date('" + FirstDate + "','dd/mm/yyyy HH24:MI:SS') and to_date('" + FirstDate2 + "','dd/mm/yyyy HH24:MI:SS')"; 


            OdbcCommand DbCmd = new OdbcCommand();
                


                DbCmd.Connection = DbConnect.ConnectOdbc();
               
                DbCmd.CommandText = querry;
                OdbcDataReader DbReader = DbCmd.ExecuteReader();

            // int fCount = DbReader.FieldCount;
            return DbReader;
            
            }

        public  OracleDataReader ClientListe(String FirstDate, String FirstDate2)
        {
            string query = " select * from DEPARTMENTS";

            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.Connect();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }


        public OdbcDataReader ListeClientMorale(String FirstDate, String FirstDate2)
        {

       
            string querry = "select distinct cust_no, cust_ac_no, ac_open_date as open_date, decode(corporate_name,' ',' ',corporate_name) Corporate_Name" //3
         + " , 0 as Mendataire,1 as Responsable" //5
         + " , (Select NVL2(Country, Country, 'V') from sttm_customer  where customer_no = cust_no) AS Country" //6
         + " , (Select NVL2(r_address2, SUBSTR2(r_address3 || ' ', 0, 30), ' ') from sttm_customer  where customer_no = cust_no) AS ville" //7
         + " , (Select NVL2(r_address2, SUBSTR2(r_address1 || ' ', 0, 50), ' ') from sttm_customer  where customer_no = cust_no) AS address" //8
         + " , (select NVL2(field_val_1,SUBSTR2(field_val_1||' ',0,10),'9') from cstm_function_userdef_fields where function_id = 'STDCIF' AND substr(rec_key,1, 8)= cust_no) AS Categorie" // Catégorie 9
         + " , (select NVL2(field_val_2,SUBSTR2(field_val_2||' ',0,10),' ')from cstm_function_userdef_fields where function_id = 'STDCIF' AND substr(rec_key,1, 8)= cust_no) AS Code_Activite" // 10 Code activité BCEAO
          + ", SUBSTR2(c_national_id||' ',0,10) AS Registre_Commerce" //11 Registre du commerce
          + ", (SELECT SUBSTR2(short_name||' ',0,10) from sttm_customer  where customer_no=cust_no) as sigle" //12 Sigle
          + " from fccetg.sttm_cust_account, fccetg.sttm_cust_corporate, fccetg.CSTM_FUNCTION_USERDEF_FIELDS"
          + " where cust_no = customer_no and substr(cust_ac_no,4, 3) in('036','024','012','022','032','015','018','023','014','027')"
         + " and record_stat = 'O'  and auth_stat = 'A'  and substr(rec_key,1, 8)= customer_no(+)"
         + "  and ac_open_date between to_date('" + FirstDate + "','dd/mm/yyyy HH24:MI:SS') and to_date('" + FirstDate2 + "','dd/mm/yyyy HH24:MI:SS')";



            OdbcCommand DbCmd = new OdbcCommand();



            DbCmd.Connection = DbConnect.ConnectOdbc();

            DbCmd.CommandText = querry;
            OdbcDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;


        }



        public OdbcDataReader CompteInfo(String numcompte)
        {


            string querry = "select distinct cust_ac_no, cust_no, ac_desc"
                  + " from fcubsfwa.sttm_cust_account"
         + " where record_stat = 'O'  and auth_stat = 'A'"
         + "  and cust_ac_no ='" + numcompte + "'";



            OdbcCommand DbCmd = new OdbcCommand();



            DbCmd.Connection = DbConnect.ConnectOdbc();

            DbCmd.CommandText = querry;
            OdbcDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;


        }


        public  string CalCleRiB(string CompteNum)
        {
            string AgenceCode = GetAgenceCodeFromRIB(CompteNum);
            int codGichet = Convert.ToInt32(buildCodeGuichet(AgenceCode));
            int RIBPart1 = 17 * 37055 + 53 * codGichet;
            int Part2 = 81 * Convert.ToInt32(CompteNum.Substring(4, 6));
            int Part3 = 3 * Convert.ToInt32(CompteNum.Substring(10, 6));

            int PartFinal = 97 - ((RIBPart1 + Part2 + Part3) % 97);
            if (PartFinal < 10)
            {
                return "0" + PartFinal;
            }
            else
            {
                return PartFinal.ToString();
            }

        }



        public static string GetAgenceCodeFromRIB(string accountNum)
        {
            return accountNum.Substring(0, 3);
        }

        public  string Residence(string Reside)
        {

            String Resident;
            Resident = "0";


                switch(Reside.Trim(' '))
            {
                case "TG": Resident = "1"; break;
                case "BJ": Resident = "1"; break;
                case "CI": Resident = "1"; break;
                case "SN": Resident = "1"; break;
                case "BF": Resident = "1"; break;
                case "ML": Resident = "1"; break;
                case "NE": Resident = "1"; break;
                case "GW": Resident = "1"; break;
                


            }

            return Resident;

        }


        public string IdPMorale(string categorie)
        {

            
            String IdPMorale = "VALEUR NON SAISIE";


            switch (categorie.Trim(' '))
            {
                case "1": IdPMorale = "société commerçante"; break;
                case "2": IdPMorale = "société non commerçante"; break;
                case "3": IdPMorale = "profession libérale"; break;
                case "4": IdPMorale = "structure étatique"; break;
                case "5": IdPMorale = "Offshore"; break;
                case "6": IdPMorale = "Non résidents"; break;
                case "7": IdPMorale = "correspondants banquiers"; break;
             
            }

            return IdPMorale;

        }

        public string Sexe(string Sex)
        {

            String SexeP;
            SexeP = "Genre non défini";


            switch (Sex)
            {
                case "F": SexeP = "2"; break;
                case "M": SexeP = "1"; break;

            }

            return SexeP;

        }

        public string  VeriNAP(string c)
        {

            //int nbr = 29;
             
            if (c.Length<=0) { c = "NAP"; }
            else {
                //c.Replace('/', ' ');
                //c.Replace('\\', ' ');
                //c.Trim('\\');
                //c.Trim('/');
                if (c.Length >= 30) { c.Substring(0,29);}
                      

            }
            return c;
         }

    



        public static string buildCodeGuichet(string codAgence)
        {
            String CodeGuichet;
            CodeGuichet = "Impossible de definir le GUICHET";
     

            switch (codAgence) {
                case "701"  : CodeGuichet = "01701"; break;
                case "702"  : CodeGuichet = "01702"; break;
                case "703"  : CodeGuichet = "01703"; break;
                case "704"  : CodeGuichet = "01704"; break;
                case "705"  : CodeGuichet = "01705"; break;
                case "706"  : CodeGuichet = "01706"; break;
                case "707"  : CodeGuichet = "06707"; break;
                case "708"  : CodeGuichet = "01708"; break;
                case "709"  : CodeGuichet = "01709"; break;
                case "710"  : CodeGuichet = "01710"; break;
                case "711"  : CodeGuichet = "07711"; break;
                case "712"  : CodeGuichet = "05712"; break;
                case "713"  : CodeGuichet = "02713"; break;
                case "714"  : CodeGuichet = "04714"; break;
                case "715"  : CodeGuichet = "10715"; break;
                case "716"  : CodeGuichet = "01716"; break;
                case "717"  : CodeGuichet = "01717"; break;
                case "718"  : CodeGuichet = "01718"; break;
                case "719"  : CodeGuichet = "01719"; break;
                case "720"  : CodeGuichet = "01720"; break;
                case "721"  : CodeGuichet = "01721"; break;
                case "722"  : CodeGuichet = "01722"; break;
                case "723"  : CodeGuichet = "03723"; break;
                case "724"  : CodeGuichet = "01724"; break;
              
            }
            return CodeGuichet;
        }

        public  string CalRib(string CleRib, string NumeroCompte)
        {

            return "TG055" + buildCodeGuichet(GetAgenceCodeFromRIB(NumeroCompte)).Trim() + NumeroCompte.Substring(4, NumeroCompte.Length - 4);

        }


        public  string FileName(int version, string codeEtc)
        {
            string Fname = string.Empty;
            string DateFichier = DateTime.Now.ToString("ddMMyy");
            Fname = string.Format("{0}_{1}_{2}.txt", codeEtc, DateFichier, Versions(version));
            return Fname;
        }


        public string Versions(int Ver)
        {
            if (Ver > 9)
            {
                return Ver.ToString();
            }
            else
            {
                return "0" + Ver.ToString();
            }
        }
    }



}
    
