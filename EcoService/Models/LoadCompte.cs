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
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Text;
using NPOI.HPSF;
using System.Web.Configuration;

namespace EcoService.Models
{
    public class LoadCompte
    {

        public OracleDataReader TRESOProofDepotRetrait(String FirstDate, String FirstDate2)
        {

            String query = " select 'RETRAIT' as Depot_C_RetraitD, sum(lcy_amount)  from bofwa.acvw_all_ac_entries where module = 'RT' "
+ " and trn_dt=to_date('" + FirstDate2 + "','DD/MM/YYYY') and event='INIT' AND LENGTH(ac_no)=12 and drcr_ind = 'D' and trn_code in('H00', 'H56', 'H65') "
+ " and ac_branch in (select branch_code from fcubsfwa.sttm_branch where regional_office = 'ETG')  "
+ " UNION "
+ " select 'DEPOT' as DepotC_RetraitD, sum(lcy_amount)  from bofwa.acvw_all_ac_entries where module = 'RT' "
+ " and trn_dt=to_date('" + FirstDate2 + "','DD/MM/YYYY')  and event='INIT' AND LENGTH(ac_no)=12 and drcr_ind = 'C' and trn_code in('H01') "
+ " and ac_branch in (select branch_code from fcubsfwa.sttm_branch where regional_office = 'ETG') ";


            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }



        public OracleDataReader Clearing(String param, String batchno)
        {
            String query;

            switch (param)
            {

                case "BACH":
                     query = " SELECT * FROM fcubsfwa.acvw_all_ac_entries WHERE BATCH_NO='" + batchno + "' " 
                        + " AND AC_BRANCH in (select branch_code from fcubsfwa.sttm_branch where country_code='TG') ";
           break;

                case "BACHLOG":
                    query = " SELECT * FROM  fcubsfwa.DETB_LOG_DETAILS  WHERE BATCH_NO='" + batchno + "' "
                       + " AND BRANCH_CODE in (select branch_code from fcubsfwa.sttm_branch where country_code='TG') ";
                    break;

                case "DAY":
                    query = " SELECT * FROM fcubsfwa.DETB_UPLOAD_DETAIL WHERE BATCH_NO='" + batchno + "' " 
                        + " AND BRANCH_CODE in (select branch_code from fcubsfwa.sttm_branch where country_code = 'TG') ";
                    break;
                case "DAYLOG":
                    query = " SELECT * FROM fcubsfwa.DETB_LOG_DETAILS WHERE BATCH_NO='" + batchno + "' " 
                         + " AND BRANCH_CODE in (select branch_code from fcubsfwa.sttm_branch where country_code = 'TG') ";

                    break;
                case "RET":
                    query = " SELECT * FROM fcubsfwa.CSTB_IW_CLEARING_MASTER WHERE BATCH_NO='" + batchno + "' " 
                       + " AND TXN_BRANCH in (select branch_code from fcubsfwa.sttm_branch where country_code = 'TG') ";

                    break;
                case "RJA":
                    query = " SELECT * FROM fcubsfwa.DETB_UPLOAD_DETAIL WHERE BATCH_NO='" + batchno + "' " 
                       + " AND BRANCH_CODE in (select branch_code from fcubsfwa.sttm_branch where country_code = 'TG') ";

                    break;


                case "RJALOG":
                   query = " SELECT * FROM fcubsfwa.DETB_LOG_DETAILS WHERE BATCH_NO='" + batchno + "'" 
                     + " AND BRANCH_CODE in (select branch_code from fcubsfwa.sttm_branch where country_code = 'TG') ";
 
                    break;

                case "VRMT":
                     query = " SELECT * FROM fcubsfwa.DETB_UPLOAD_DETAIL WHERE BATCH_NO='" + batchno + "'" 
                      + " AND BRANCH_CODE in (select branch_code from fcubsfwa.sttm_branch where country_code = 'TG')";

                    break;
                case "VRMTLOG":
                    query = " SELECT * FROM fcubsfwa.DETB_LOG_DETAILS WHERE BATCH_NO='" + batchno + "'" 
                        + " AND BRANCH_CODE in (select branch_code from fcubsfwa.sttm_branch where country_code = 'TG')";

                    break;

                default:
                    query = "SELECT sysdate from dual";
                    break;

            }

   

            OracleCommand DbCmd = new OracleCommand();

            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }


        public OracleDataReader SMuserliste(String FirstDate, String FirstDate2)
        {
            string query = " select substr(external_ref_no,4,3) FILIALE ,user_id, count(distinct(external_ref_no)) NOMBRE_TRANSACTIONS,to_char(trn_dt, 'DD/MM/RRRR') as DateOperation from fcubsfwa.ACVWS_ALL_AC_ENTRIES WHERE  "
+ " substr(trn_ref_no,1,3) in (select branch_code from fcubsfwa.sttm_branch where regional_office = 'ETG') "
+ " and trn_dt between to_date('" + FirstDate + "','dd/mm/yyyy') AND to_date('" + FirstDate2 + "','dd/mm/yyyy') "
+ " and external_ref_no like 'SMTETG%' group by substr(external_ref_no, 4, 3),to_char(trn_dt, 'DD/MM/RRRR'), user_id order by to_char(trn_dt, 'DD/MM/RRRR') DESC ";

            OracleCommand DbCmd = new OracleCommand();

            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }
        public OracleDataReader CRPReportCbank(String FirstDate, String FirstDate2)
        {
            string query = " select 'CRP' as Prefixe, NVL(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(user_ref_number, '-'),'/'),'_'),';'),':'),'.'),' '),a.contract_ref_no) as NumeroDossier, "
+ " event_seq_no NumeroSequence, LPAD(dense_rank() over(order by a.ROWID), 6, '0') as NumeroEnregistrement, "
+ " (CASE WHEN event_code in ('LIQD') or STATUS = 'P' THEN '1' WHEN STATUS not in ('P', 'V') THEN '2' "
+ " WHEN STATUS = 'V' or event_code = '' THEN '3' WHEN Substr(contract_ref_no,4,4)= 'SICA' THEN '4' END) as TypeCRP, '' as CodeIntermDeclarantEmetteurATR, '' as DateEmissionATR, '' as NumATR,  "
+ "  substr(contract_ref_no, 8, 9) || LPAD(dense_rank() over(order by a.ROWID), 5, '0') as Num_CRP, "
+ " 'ECOBANK TOGO ' as NomIntermDeclarant, 'TT0055T1' as CodeIntermDeclarant,  substr(DR_IBAN, 1, 2) PaysdeResidenceDuDonneurDordre, "
+ " to_char(accounting_date, 'DD/MM/RRRR') as DateOperation, (CASE WHEN event_code in ('LIQD') or STATUS = 'P' THEN '1' "
+ " WHEN STATUS = 'V' or event_code = '' THEN '3' else '3' END) as ReglementComptePropre, "
+ " by_order_of2 || by_order_of3 as NomDonneurOrdreouBeneficiaire,dr_account as CodeDonneurOrdreouBeneficiaire, "
+ " 'NC' as SecteurInstitutionnelDonneurOrdreouBeneficiaire, "
+ " (select Code_NAEMA from etguser.CRP_TABLE_NAEMA A, fcubsfwa.MITM_CUSTOMER_DEFAULT B where sect_act_code = cust_mis_6 and customer = substr(cr_account, 1, 9)) as CodeNAEMADonneurOrdreouBeneficiaire, "
+ " substr(DMY_RECEIVER, 5, 2) as PaysdeResidenceBeneficiaire, (CASE WHEN(select account_class from fcubsfwa.sttm_cust_account where cust_ac_no = dr_account) in ('NAOCOR', 'IANAUD') "
+ " THEN '1' ELSE '2' END) as NatureCompteMouvemente, (CASE WHEN b.product_type in ('O') THEN '1' WHEN b.product_type in ('I') THEN '2' END) as SensOperation , "
+ " dr_ccy as DeviseReglement, REPLACE(dr_amount, '.', ',') as MontantOperation, REPLACE(exchange_rate, '.', ',') as TauxChange, lcy_equiv as MontantFcfa, "
+ " '304' as CodeITRS, (CASE WHEN lcy_equiv>= 10000000 THEN '1' WHEN lcy_equiv<10000000 THEN '2' END) as Domiciliation, "
+ " (CASE WHEN b.product_code in ('INFT', 'CORT', 'RTGS', 'SICA', 'LBOT') and substr(DMY_RECEIVER,5,2) in ('BF', 'CI', 'BJ', 'TG', 'ML', 'SN', 'NE', 'GW')THEN '1' "
+ " WHEN b.product_code in ('CSTO', 'CSTF') THEN '2' ELSE '3' END) as ModaliteTransfert  "
+ " from fcubsfwa.FTTB_CONTRACT_MASTER a, fcubsfwa.FTTM_PRODUCT_DEFINITION b   "
+ " where a.product_code = b.product_code "
+ " and a.accounting_date between to_date('" + FirstDate + "', 'dd/mm/yyyy HH24:MI:SS') and to_date('" + FirstDate2 + "', 'dd/mm/yyyy HH24:MI:SS') "
+ " and by_order_of3 is not null and ult_beneficiary2 is not null and b.product_type in ('I', 'O') and length(dr_account)= 12 "
+ " and(a.dr_account_branch in (select branch_code from fcubsfwa.sttm_branch where country_code in ('TG')))  "; 

            OracleCommand DbCmd = new OracleCommand();

            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }
        public OracleDataReader ATPReportCbank(String FirstDate, String FirstDate2)
        {
            string query = " select 'ATR' as Prefixe, LPAD(dense_rank() over(order by a.ROWID), 6, '0') as CodeEnregistrement, "
+ " (CASE WHEN event_code in ('LIQD') or STATUS = 'P'   THEN '1' WHEN STATUS not in ('P', 'V') THEN '2'  WHEN STATUS = 'V' or event_code = '' THEN '3' "
+ " END) as TypeAtr,/*user_ref_number*/ cr_account as NumAtr,'TT0055T1' as CodeIDReceveur , "
+ " contract_ref_no as ReferenceInterne, to_char(accounting_date, 'DD/MM/RRRR') as DateOperation, "
+ " 'ECOBANK-TOGO' as NomIDTeneur,'KK0094R1' as CodeIDTeneur, by_order_of3 as NomDonneurOrdre, "
+ " (select country from fcubsfwa.sttm_customer where customer_no = substr(a.dr_account, 1, 9)) as PaysProvenance, "
+ " ult_beneficiary2 as NomBeneficiaire ,(select Code_NAEMA from etguser.CRP_TABLE_NAEMA A, fcubsfwa.MITM_CUSTOMER_DEFAULT B "
+ " where sect_act_code = cust_mis_6 and customer = substr(cr_account, 1, 9) )  "
+ " as CodeNaemaBeneficiaire, dr_ccy as DeviseReglement,dr_amount as MontantOperation,lcy_equiv as MontantFcfa "
+ " from fcubsfwa.FTTB_CONTRACT_MASTER a, fcubsfwa.FTTM_PRODUCT_DEFINITION b where a.product_code = b.product_code "
+ " and a.accounting_date between to_date('" + FirstDate + "', 'dd/mm/yyyy HH24:MI:SS') and to_date('" + FirstDate2 + "', 'dd/mm/yyyy HH24:MI:SS') "
+ " and by_order_of3 is not null and ult_beneficiary2 is not null and b.product_type = 'I'--and c.Filiale = 'TG' "
+ " and(a.dr_account_branch in (select branch_code from fcubsfwa.sttm_branch where country_code in ('TG')))  ";

            OracleCommand DbCmd = new OracleCommand();

            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }

        public OracleDataReader AchatVenteDevise(String FirstDate, String FirstDate2)
        {
            string query = " select distinct(trn_ref_no),ac_ccy,period_code,trn_code,trn_dt,fcy_amount,lcy_amount,exch_rate, user_id, auth_id,bofwa.get_stmt_acct_ecobank(trn_ref_no, event_sr_no, module,ac_entry_sr_no ) txn_narrations from BOFWA.acvw_all_ac_entries where trn_code in ('X01','X54') "
       + " and TRN_DT between to_date('" + FirstDate + "', 'dd/mm/yyyy HH24:MI:SS') and to_date('" + FirstDate2 + "', 'dd/mm/yyyy HH24:MI:SS')"
+ " and ac_branch in (select branch_code from fcubsfwa.sttm_branch where country_code in ('TG')) ";

            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }


        public OracleDataReader CanauxDigitaux(String listecompte)
        {
            string query = " SELECT AC_ENTRY_REF_NO, LCY_EQUIV_AMT, TXN_AMOUNT, DISPATCH_DT, CHARGE_BEARER, PRODUCT_CODE, payment_details_1, payment_details_2, payment_details_3, "
 + " payment_details_4, CUST_AC_NO, CUST_NAME, CUST_ADDRESS_1, CUST_ADDRESS_2, CUST_ADDRESS_3, CPTY_AC_NO, CPTY_NAME, "
 + " cpty_address_1, cpty_address_2, CPTY_BANKCODE, SUBSTR(UDF_2, 1, 5) as CPTY_CLRCODE, REMARKS, UDF_1 as UDF_BEN_ACC_NO, SUBSTR(UDF_2, 1, 5) as UDF_BEN_CLR_CODE,  CPTY_NAME as UDF_BEN_NAME "
 + "  FROM fcubsfwa.pctb_CONTRACT_master WHERE  AC_ENTRY_REF_NO in (" + listecompte + ") " ;

            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }
        public OracleDataReader SoldeCompte(String listecompte)
        {
            string query = "select branch_code,cust_no,cust_ac_no,account_class, ac_open_date,limit_ccy,ac_stat_no_dr,ac_stat_dormant,lcy_curr_balance,date_last_cr_activity, date_last_dr_activity"
+ " from fcubsfwa.sttm_cust_account a"
+ " where cust_ac_no in (" + listecompte + ")"
+ " and branch_code   in (select branch_code from fcubsfwa.sttm_branch where regional_office = 'ETG') ";

            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }


        public OracleDataReader ListeClientPhysique(String FirstDate, String FirstDate2)
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


            OracleCommand DbCmd = new OracleCommand();
           



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = querry;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            // int fCount = DbReader.FieldCount;
            return DbReader;

        }

        public OracleDataReader RibClient(String listecompte)
        {
            string query = "select distinct cust_ac_no,substr(ebjuser.get_iban_iso(cust_ac_no),1,22)||substr(ebjuser.get_iban_iso(cust_ac_no),23,2) as RIB, ac_open_date "
                           + "from fcubsfwa.sttm_cust_account where record_stat = 'O'  and auth_stat = 'A' "
                           + " and cust_ac_no in (" + listecompte + ")"
                           + " and branch_code   in (select branch_code from fcubsfwa.sttm_branch where regional_office = 'ETG') ";

            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }

        public OracleDataReader AgenceAoAcBu(String listecompte)
        {
            string query = "select branch_code, cust_no, cust_ac_no, ac_desc, a.account_class,a.ac_stat_dormant,a.AC_STAT_NO_DR,a.AC_STAT_NO_CR,ac_open_date,a.record_stat,ccy,acy_curr_balance,lcy_curr_balance,"
+ " (select cust_mis_4 from  fcubsfwa.mitm_customer_default where customer = a.cust_no) ETIBISEG2,"
+ " (select comp_mis_1 from  fcubsfwa.mitm_customer_default where customer = a.cust_no) ACC_OFCR,"
+ " (select p.telephone from fcubsfwa.sttm_cust_personal p where p.customer_no = a.cust_no) TELEPHONE,"
+ " (select p.e_mail from fcubsfwa.sttm_cust_personal p where p.customer_no = a.cust_no) e_mail "
+ " from fcubsfwa.sttm_cust_account a, fcubsfwa.sttm_account_class b "
+ " where a.record_stat <> 'C' "
+ " and a.account_class = b.account_class "
+ " and cust_ac_no in (" + listecompte + ") "
+ " and branch_code   in (select branch_code from fcubsfwa.sttm_branch where regional_office = 'ETG') ";

            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }

        public OracleDataReader AdresseClientPp(String listecompte)
        {
            string query = "select A.branch_code,A.cust_ac_no,A.ac_desc, B.first_name,A.account_type, A.account_class, B.middle_name,  B.last_name, passport_no, A.cust_no, A.record_stat, A.ac_open_date,p_national_id, sex,  p_country,date_of_birth,  B.telephone,C.address_line1, C.address_line2,C.address_line3,address1, address2, address3"  
+ " ,(select D.marital_status from fcubsfwa.sttm_cust_domestic D where D.customer_no = A.cust_no) as Marital_Status"
+ " ,(select E.designation from fcubsfwa.STTM_CUST_PROFESSIONAL E where E.customer_no = A.cust_no) as Fonction"
+ " ,(select p.e_mail from fcubsfwa.sttm_cust_personal p where p.customer_no = a.cust_no) as EMAIL"
+ " From fcubsfwa.sttm_cust_account A,fcubsfwa.STTM_CUST_PERSONAL B, fcubsfwa.sttm_customer C"
+ " where A.cust_no = B.customer_no(+) and A.cust_no = C.customer_no"
+ " and cust_ac_no in (" + listecompte + ")"
+ " and branch_code   in (select branch_code from fcubsfwa.sttm_branch where regional_office = 'ETG') ";

            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }

        public OracleDataReader ClientListe(String FirstDate, String FirstDate2)
        {
            //string query = " select * from fcubsfwa.sttm_cust_account where cust_no='140160217'";



            string query = "SELECT branch_code, fcubsfwa.sttm_cust_account.maker_id, cust_no, account_class, "
+ " cust_ac_no, ac_desc, lcy_curr_balance,ac_open_date, sttm_customer.CUSTOMER_CATEGORY,sttm_customer.COUNTRY,ac_stat_dormant,  "
+ "cust_mis_1 AS CR_OFCER,cust_mis_2 AS CUST_CATE,cust_mis_3 AS IND_SEGM,cust_mis_4 AS ETIBISEG2,  "
+ "cust_mis_5 AS SUBSICCOD,cust_mis_6 AS SECT_ACTV, cust_mis_7 AS AFFILIATE, cust_mis_8 AS MONT_ZONE,  "
+ "comp_mis_1 AS ACC_OFCR, comp_mis_2 AS ACCT_INIT, comp_mis_3 AS IND_SEGMT, comp_mis_4 AS ETIBISEG,  "
+ "comp_mis_5 AS SUBSICCOD, comp_mis_6 AS AFFILIATE, comp_mis_7 AS REGION, comp_mis_8 AS AREA,  "
+ "comp_mis_9 AS VAL_CHAIN  "
+ "FROM  fcubsfwa.MITM_CUSTOMER_DEFAULT, fcubsfwa.sttm_cust_account, fcubsfwa.sttm_customer  "
+ "WHERE fcubsfwa.sttm_cust_account.cust_no = fcubsfwa.sttm_customer.customer_no  "
+ "and fcubsfwa.sttm_customer.customer_no = fcubsfwa.MITM_CUSTOMER_DEFAULT.customer(+)  "
+ "AND fcubsfwa.sttm_cust_account.record_stat <> 'C' and account_class in ('ECOMCA', 'ECOMOA')  "
+ "and branch_code in (select branch_code from fcubsfwa.sttm_branch where country_code in ('TG'))  "
+ "ORDER BY branch_code,ac_open_date";

            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }


        public OracleDataReader Pdo_Cad(String FirstDate, String FirstDate2)
        {
            string query = "select trn_ref_no,drcr_ind,trn_dt,lcy_amount,cust_ac_no,cust_no,a.trn_code, trn_desc,customer_name1,ac_desc ,account_class, bofwa.GET_STMT_ACCT_ECOBANK(TRN_REF_NO,a.event_sr_no,a.module,a.ac_entry_sr_no) Intitule_cpte"
+ ", cust_mis_2, cust_mis_1,(select  code_desc from fcubsfwa.GLTM_MIS_CODE  where mis_code = cust_mis_1 and mis_class = 'ACC_OFCR') as ACCOUNT_OFFICER ,cust_mis_4, (select  code_desc from bofwa.GLTM_MIS_CODE where mis_code = comp_mis_4  and mis_class = 'ETIBISEG2') as SEGMENT1"
+ " from fcubsfwa.acvw_all_ac_entries a, fcubsfwa.sttm_cust_account, fcubsfwa.STTM_CUSTomer c, fcubsfwa.sttm_trn_code e, bofwa.MITM_CUSTOMER_DEFAULT"
+ " where (cust_ac_no = ac_no And a.trn_code = e.trn_code)  and cust_no = customer"
  + " and TRN_DT between to_date('" + FirstDate + "', 'dd/mm/yyyy HH24:MI:SS') and to_date('" + FirstDate2 + "', 'dd/mm/yyyy HH24:MI:SS')"
 + "  and related_account  in (select account_number from fcubsfwa.cltb_account_comp_bal_breakup where"
  + " status_code = 'IMPY' AND BRANCH_CODE IN (SELECT BRANCH_CODE FROM fcubsfwa.STTM_BRANCH WHERE PARENT_BRANCH = 'ETG'))"  
  + " and cust_no = customer_no and ac_branch   in (select branch_code from fcubsfwa.sttm_branch where regional_office = 'ETG') ";

            OracleCommand DbCmd = new OracleCommand();
            DbCmd.CommandTimeout = 300;


            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;



            
        }

        public OracleDataReader MvtAffaireCM(String FirstDate, String FirstDate2)

        {
            //        

            string query = " select branch_code, cust_no, cust_ac_no, ac_desc, account_class, ac_open_date, limit_ccy, "
 + " atm_facility, date_last_cr_activity, date_last_dr_activity, b.cust_mis_4, "
+ " (select  r_address1 from fcubsfwa.sttm_cust_corporate where customer_no = a.cust_no) as r_address1, "
+ " (select  r_address2 from fcubsfwa.sttm_cust_corporate where customer_no = a.cust_no) as r_address2, "
+ " (select  r_address3 from fcubsfwa.sttm_cust_corporate where customer_no = a.cust_no) as r_address3, "
+ " (select  business_description from fcubsfwa.sttm_cust_corporate where customer_no = a.cust_no) as Business_description, "
+ " (Select code_desc from fcubsfwa.gltm_mis_code where mis_class = 'ACC_OFCR' and mis_code = b.cust_mis_1) as Account_officcer, "
+ " (select sum(decode(DRCR_IND, 'C', LCY_amount, 0)) from fcubsfwa.acvw_all_ac_entries where trn_dt  between to_date('" + FirstDate + "','DD/MM/YYYY') and to_date('" + FirstDate2 + "','DD/MM/YYYY') and ac_no = a.cust_ac_no and amount_tag!= 'PRINCIPAL')  MONTANT_CREDIT, "
+ " (select sum(decode(DRCR_IND, 'D', LCY_amount, 0)) from fcubsfwa.acvw_all_ac_entries where trn_dt  between to_date('" + FirstDate + "','DD/MM/YYYY') and to_date('" + FirstDate2 + "','DD/MM/YYYY') and ac_no = a.cust_ac_no)  MONTANT_DEBIT "
+ " from fcubsfwa.sttm_cust_account a, fcubsfwa.mitm_customer_default b "
+ " WHERE a.cust_no = b.customer  AND Account_type in ('U', 'S') "
+ " AND b.comp_mis_4 IN('CMSE_1500','CMSM_1400','CMSM_1300','CMSM_2100','CSMF_1100','CMLM_1600') "
+ " AND branch_code IN(select branch_code from fcubsfwa.sttm_branch where regional_office = 'ETG') ";
          
            
    //        string query = "select branch_code,cust_no,cust_ac_no,ac_desc,account_class, ac_open_date,limit_ccy,"
//+ " atm_facility,date_last_cr_activity, date_last_dr_activity, b.cust_mis_4,  "
//+ " (select  r_address1 from fcubsfwa.sttm_cust_corporate where customer_no = a.cust_no) as r_address1,  "
//+ " (select  r_address2 from fcubsfwa.sttm_cust_corporate where customer_no = a.cust_no) as r_address2,  "
//+ " (select  r_address3 from fcubsfwa.sttm_cust_corporate where customer_no = a.cust_no) as r_address3,  "
//+ " (select  business_description from fcubsfwa.sttm_cust_corporate where customer_no = a.cust_no) as Business_description,  "
//+ " (Select code_desc from fcubsfwa.gltm_mis_code where mis_class = 'ACC_OFCR' and mis_code = b.cust_mis_1) as Account_officcer,  "
//+ " (select sum(decode(DRCR_IND, 'C', LCY_amount, 0)) from fcubsfwa.acvw_all_ac_entries  "
//+ "  where trn_dt  between to_date('" + FirstDate + "','DD/MM/YYYY') and to_date('" + FirstDate2 + "','DD/MM/YYYY') and ac_no = a.cust_ac_no and amount_tag!= 'PRINCIPAL')  MONTANT_CREDIT,  "
//+ " (select sum(decode(DRCR_IND, 'D', LCY_amount, 0)) from fcubsfwa.acvw_all_ac_entries  "
//+ "  where trn_dt  between to_date('" + FirstDate + "','DD/MM/YYYY') and to_date('" + FirstDate2 + "','DD/MM/YYYY') and ac_no = a.cust_ac_no)  MONTANT_DEBIT  "
// + "   from fcubsfwa.sttm_cust_account a, fcubsfwa.mitm_customer_default b  "
// + " WHERE a.cust_no = b.customer AND LENGTH(a.cust_AC_NO)=v 12 AND Account_type in ('U', 'S')"
//+ " AND b.comp_mis_4 IN('CMSE_1500','CMSM_1400','CMSM_1300','CMSM_2100','CSMF_1100','CMLM_1600') "
//+ " AND branch_code IN(select branch_code from fcubsfwa.sttm_branch where regional_office = 'ETG') ";
          
OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }



        public OracleDataReader CpteDormantReactive(String FirstDate, String FirstDate2)

        {
            //        

            string query = " select a.cust_ac_no as NumeroDeCompte,ac_desc as IntituleDuCompte,a.cust_no as NumeroClientCIF, "
 + " (b.acy_curr_acc_bal)  as SoldesDisponibles, "
     + "   (b.acy_curr_acc_bal + b.acy_blocked_amt) as SoldesReel, "
     + "   nvl(b.acy_blocked_amt,0) as  MontantsBloques,b.AC_STAT_DORMANT as STATUTdEdORMANCE, "
     + "   C.dormancy_end_dt AS DATEfINdORMANCE, "
      + "  a.AC_STAT_NO_DR as StatutNOdEBIT,a.AC_STAT_NO_dr as SstatutNOCREDIT, "
     + " (  select maint_instr_1 from fcubsfwa.STTM_ACCOUNT_MAINT_INSTR d  "
      + "   where a.CUST_AC_NO=d.cust_ac_no and maint_instr_1 is not null)  InstructionSpeciales "
     + "   from fcubsfwa.sttm_cust_Account a,fcubsfwa.sttm_account_balance b ,fcubsfwa.sttm_cust_Account_dormancy c "
 + " where a.cust_ac_no=b.cust_ac_no and b.cust_ac_no=c.cust_ac_no AND "    
 + " a.record_stat='O' and  a.account_type in ('U','S') "
 + " and a.branch_code in (select branch_code from fcubsfwa.sttm_branch where regional_office='ETG') "
 + " AND C.dormancy_end_dt   between to_date('" + FirstDate + "','DD/MM/YYYY') and to_date('" + FirstDate2 + "','DD/MM/YYYY') ";


            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }

        public OracleDataReader CADComptesOpsOA(String FirstDate, String FirstDate2)
        {

            String query = "SELECT c.customer_no,c.customer_name1,a.cust_no,a.cust_ac_no,C.EXPOSURE_COUNTRY,C.CUSTOMER_TYPE,a.ac_desc,a.ACCOUNT_CLASS, a.dr_gl,a.cr_gl, " 
+ " b.acy_closing_bal,to_float(b.lcy_closing_bal) as lcy_closing_bal,to_char(a.date_last_Cr, 'dd/MM/yyyy') as date_last_Cr  ,to_char(a.overdraft_since, 'dd/MM/yyyy') as overdraft_since, b.ACC_CCY,d.ac_class_type, d.account_class,d.description "
+ " from fcubsfwa.sttm_cust_account a, fcubsfwa.actb_accbal_history b, fcubsfwa.sttm_customer c, fcubsfwa.sttm_account_class d "
+ " where a.cust_no = c.customer_no and b.account = a.cust_ac_no and b.ACY_CLOSING_BAL < 0 "
+ " AND b.bkg_date in (select max(bkg_date) from fcubsfwa.actb_accbal_history b where b.account = a.cust_ac_no "
+ " and b.bkg_date <= to_date('" + FirstDate2 + "','DD/MM/YYYY'))  "
+ " and c.local_branch = 'ETG' and a.ACCOUNT_CLASS = d.account_class "
+ " and d.ac_class_type = 'N' ";

            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }


        public OracleDataReader CadIFRS(String FirstDate, String FirstDate2)
        {


String query  = " SELECT distinct B.ACCOUNT_STATUS, B.ACCOUNT_NUMBER, B.VERSION_NO, B.CUSTOMER_ID, "
+ "  D.CUSTOMER_NAME1, to_char(B.BOOK_DATE, 'dd/MM/yyyy') as BOOK_DATE,to_char(B.VALUE_DATE, 'dd/MM/yyyy') as VALUE_DATE , to_char(B.MATURITY_DATE, 'dd/MM/yyyy') as MATURITY_DATE, "
+ "  (SELECT DECODE(to_char(min(SCHEDULE_DUE_DATE), 'dd/MM/yyyy'), '', '" + FirstDate2 + "' , to_char(min(SCHEDULE_DUE_DATE), 'dd/MM/yyyy')) "
+ "  FROM fcubsfwa.CLTB_ACCOUNT_SCHEDULES X "
+ "  WHERE X.ACCOUNT_NUMBER = B.ACCOUNT_NUMBER AND SCHEDULE_DUE_DATE > to_date('" + FirstDate2 + "', 'dd/MM/yyyy') "
+ "  AND SCH_STATUS = 'NORM' AND COMPONENT_NAME = 'PRINCIPAL') START_DATE, "
+ "  REPLACE((SELECT MAX(UNIT) FROM FCUBSFWA.CLTB_ACCOUNT_COMP_SCH Y WHERE  Y.ACCOUNT_NUMBER = B.ACCOUNT_NUMBER "
+ "  ),'D','B') FRENQUENCY ,(SELECT COUNT(*)  FROM fcubsfwa.CLTB_ACCOUNT_SCHEDULES ZZ WHERE ZZ.ACCOUNT_NUMBER = B.ACCOUNT_NUMBER AND SCHEDULE_DUE_DATE > to_date('" + FirstDate2 + "' , 'dd/MM/yyyy') "
+ "  AND SCH_STATUS = 'NORM' AND COMPONENT_NAME = 'PRINCIPAL') NBRE "
+ "  ,(SELECT COUNT(*)  FROM fcubsfwa.CLTB_ACCOUNT_SCHEDULES TT WHERE TT.ACCOUNT_NUMBER = B.ACCOUNT_NUMBER  AND SCH_STATUS = 'NORM' AND COMPONENT_NAME = 'PRINCIPAL' "
+ " ) NBRE_TOTAL_ECHEANCE ,(SELECT round(SUM(amount_due) / count(*))  FROM FCUBSFWA.CLTB_ACCOUNT_SCHEDULES XX WHERE XX.ACCOUNT_NUMBER = B.ACCOUNT_NUMBER "
+ " AND SCH_STATUS = 'NORM' AND COMPONENT_NAME = 'PRINCIPAL' ) AMOUNT "
+ " FROM FCUBSFWA.CLTB_ACCOUNT_APPS_MASTER B, FCUBSFWA.CLTB_ACCOUNT_SCHEDULES C, FCUBSFWA.STTM_CUSTOMER D "
+ " WHERE C.ACCOUNT_NUMBER = B.ACCOUNT_NUMBER "
+ " AND B.CUSTOMER_ID = D.CUSTOMER_NO "
+ " AND C.SCH_STATUS = 'NORM' "
+ " AND C.COMPONENT_NAME = 'PRINCIPAL' "
+ " AND B.ACCOUNT_STATUS = 'A' "
+ " and d.local_branch = 'ETG' ";
            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }


        public OracleDataReader CadTombesCS(String FirstDate, String FirstDate2)
        {

            String query = " select distinct nvl(LD.ALT_ACC_NO, LD.account_number) ContractCodeF7F12, nvl(LD.account_number, LD.ALT_ACC_NO) ContractCodeF12, LD.product_code, "
+ "  NVL(C.ext_ref_no, LD.customer_id) codesonsent, LD.customer_id codesonsent  "

+ "  ,(select distinct customer_name1 from fcubsfwa.sttm_customer where customer_no = LD.customer_id) as customer_name,  "

+ " LD.branch_code Branch, LD.ACCOUNT_STATUS contract_status, LD.user_defined_status, '' curr_event_code,   "

+ "  (select max(lcy_amount) from fcubsfwa.acvw_all_ac_entries where module = 'CL' and related_account = LD.account_number  "
+ " and trn_dt between to_date('" + FirstDate + "','dd/mm/yyyy') AND to_date('" + FirstDate2 + "','dd/mm/yyyy')   "
+ " and drcr_ind = 'D' and amount_tag = 'PRINCIPAL_LIQD' ) as PRINCIPAL_LIQD "

+ " ,(select max(lcy_amount) from fcubsfwa.acvw_all_ac_entries where module = 'CL' and related_account = LD.account_number  "
+ " and trn_dt between to_date('" + FirstDate + "','dd/mm/yyyy') AND to_date('" + FirstDate2 + "','dd/mm/yyyy')   "
+ " and drcr_ind = 'D' and amount_tag = 'MAIN_INT_LIQD' ) as MAIN_INT_LIQD "

+ " ,(select max(lcy_amount) from fcubsfwa.acvw_all_ac_entries where module = 'CL' and related_account = LD.account_number  "
+ " and trn_dt between to_date('" + FirstDate + "','dd/mm/yyyy') AND to_date('" + FirstDate2 + "','dd/mm/yyyy')  "
+ " and drcr_ind = 'D' and amount_tag = 'TAF_MAININT_LIQD' ) as TAF_MAININT_LIQD "

+ " FROM fcubsfwa.cltb_account_apps_master LD, fcubsfwa.STTM_CUSTOMER C, fcubsfwa.cLtm_product CC  "
+ " WHERE LD.customer_id = C.customer_no AND LD.product_code = CC.product_code-- - AND LD.ACCOUNT_STATUS = 'A' AND CC.product_type = 'L'  "
+ " and LD.book_date < trunc(sysdate, 'MM')  "
+ " and decode(field_date_1, null, value_date, field_date_1)  <= (trunc(sysdate, 'MM') - 1)  "
+ " AND LD.customer_id not in (SELECT customer_no from fcubsfwa.STTM_CUST_PERSONAL WHERE TRUNC(MONTHS_BETWEEN(TO_DATE('01/' || TO_CHAR(sysdate, 'mm/yyyy'), 'dd/mm/yyyy'), date_of_birth) / 12) < 18)  "
+ " AND LD.customer_id not in (SELECT customer_no from fcubsfwa.STTM_CUST_PERSONAL WHERE TRUNC(MONTHS_BETWEEN(TO_DATE('01/' || TO_CHAR(sysdate, 'mm/yyyy'), 'dd/mm/yyyy'), date_of_birth) / 12) > 99)  "
+ " and LD.amount_financed >= 5400 and LD.amount_financed <= 6000000000   "
+ " and LD.branch_code in (select branch_code from fcubsfwa.sttm_branch where regional_office = 'ETG')  "
+ " and LD.original_st_date <= TO_DATE('" + FirstDate2 + "', 'dd/mm/yyyy')   "
+ " AND LD.ACCOUNT_STATUS NOT IN('L', 'V')   "
+ " and LD.customer_id  in (select customer from  fcubsfwa.mitm_customer_default where cust_mis_4  in ('CSPP_1300', 'CSPA_1200', 'CSPC_1000', 'CSPY_1100') )  ";

            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }


        public OracleDataReader CadTombesCBCM(String FirstDate, String FirstDate2)
        {

            String query = " select distinct nvl(LD.ALT_ACC_NO ,LD.account_number) ContractCodeF7F12, LD.account_number ContractCodeF12, LD.product_code, "
 + " NVL(C.ext_ref_no, LD.customer_id) codesonsent, LD.customer_id codesonsent "
  + " ,(select distinct customer_name1 from fcubsfwa.sttm_customer where customer_no = LD.customer_id) as customer_name,  "
 + " LD.branch_code Branch, LD.ACCOUNT_STATUS contract_status, LD.user_defined_status, '' curr_event_code,  "
 + " (select sum(lcy_amount) from fcubsfwa.acvw_all_ac_entries where module = 'CL' and related_account = LD.account_number  "
 + " and trn_dt between to_date('" + FirstDate + "','dd/mm/yyyy') AND to_date('" + FirstDate2 + "','dd/mm/yyyy')  "
 + " and drcr_ind = 'D' and amount_tag = 'PRINCIPAL_LIQD' ) as PRINCIPAL_LIQD "
 + " ,(select sum(lcy_amount) from fcubsfwa.acvw_all_ac_entries where module = 'CL' and related_account = LD.account_number "
 + " and trn_dt between to_date('" + FirstDate + "','dd/mm/yyyy') AND to_date('" + FirstDate2 + "','dd/mm/yyyy') "
 + " and drcr_ind = 'D' and amount_tag = 'MAIN_INT_LIQD' ) as MAIN_INT_LIQD "
 + " ,(select sum(lcy_amount) from fcubsfwa.acvw_all_ac_entries where module = 'CL' and related_account = LD.account_number "
 + " and trn_dt between to_date('" + FirstDate + "','dd/mm/yyyy') AND to_date('" + FirstDate2 + "','dd/mm/yyyy') "
 + " and drcr_ind = 'D' and amount_tag = 'TAF_MAININT_LIQD' ) as TAF_MAININT_LIQD "
 + " FROM fcubsfwa.cltb_account_apps_master LD, fcubsfwa.STTM_CUSTOMER C, fcubsfwa.cLtm_product CC "
 + " WHERE LD.customer_id = C.customer_no AND LD.product_code = CC.product_code "
 + " and LD.book_date < trunc(sysdate, 'MM')  "
 + " and decode(field_date_1, null, value_date, field_date_1)  <= (trunc(sysdate, 'MM') - 1)  "
 + " AND LD.customer_id not in (SELECT customer_no from fcubsfwa.STTM_CUST_PERSONAL WHERE TRUNC(MONTHS_BETWEEN(TO_DATE('01/' || TO_CHAR(sysdate, 'mm/yyyy'), 'dd/mm/yyyy'), date_of_birth) / 12) < 18) "
 + " AND LD.customer_id not in (SELECT customer_no from fcubsfwa.STTM_CUST_PERSONAL WHERE TRUNC(MONTHS_BETWEEN(TO_DATE('01/' || TO_CHAR(sysdate, 'mm/yyyy'), 'dd/mm/yyyy'), date_of_birth) / 12) > 99)  "
 + " and LD.amount_financed >= 5400 and LD.amount_financed <= 6000000000 "
 + " and LD.branch_code in (select branch_code from fcubsfwa.sttm_branch where regional_office = 'ETG') "
 + " and LD.original_st_date <= TO_DATE('" + FirstDate2 + "', 'DD/MM/YYYY') "
 + " AND LD.ACCOUNT_STATUS NOT IN('L', 'V')  "
 + " and LD.customer_id  in (select customer from  fcubsfwa.mitm_customer_default where cust_mis_4  in ('CBFI_3400', "
 + " 'CBGC_2100', 'CBGC_3100', 'CBGC_4100', 'CBPC_2000', 'CBPC_4000', 'CBPS_1200', 'CBRC_2200', 'CBRC_3200', 'CBRC_4200', 'CMLM_1600', 'CMSE_1500', 'CMSM_1400', 'CMSM_2100', 'CSPA_1210', 'CBPS_1100', 'CMPS_4200', 'CMSM_1300') ) ";


            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }

        //Valeur des garantie reçu des clients 
        public OracleDataReader CadCashColl()
        {

            String query = " SELECT branch_code,cust_ac_no,ac_desc,cust_no,account_class,ac_stat_no_dr,ac_stat_no_dr,to_char(ac_open_date, 'dd/MM/yyyy'),alt_ac_no, "
+ " record_stat,acy_curr_balance,lcy_curr_balance FROM FCUBSFWA.STTM_CUST_ACCOUNT WHERE ACCOUNT_CLASS in ('BJECCO', 'BJLCCO')  "
+ " and lcy_curr_balance<>0 and branch_code in (select branch_code from FCUBSFWA.sttm_branch where regional_office = 'ETG' )  ";

            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }


        public OracleDataReader ATMExtractions(String FirstDate, String FirstDate2)
        {
            string query = "SELECT ACVW_ALL_AC_ENTRIES.TRN_REF_NO,AC_BRANCH,FROM_ACC AS CUSTOMER_ACCOUNT,DRCR_IND,LCY_AMOUNT, "
+ " TO_CHAR(TRN_DT, 'DD-MM-YYYY') AS TRN_DT, TERM_ID AS CA_TERM_ID, "
+  " AS CA_ID_CODE, TERM_ADDR AS CA_NAME, AC_NO, BRANCH_NAME, PAN AS CARD_NO, PAN AS POS_CARD_NO, SWVW_TXN_DETAIL.RRN AS RETRIEVAL_REFERNCE_NO,SWVW_TXN_DETAIL.FROM_ACC "
+  " ,DB_IN_TIME,DB_OUT_TIME "
+ " FROM FCUBSFWA.ACVW_ALL_AC_ENTRIES, FCUBSFWA.SWVW_TXN_DETAIL, FCUBSFWA.STTM_BRANCH "
+ " WHERE ACVW_ALL_AC_ENTRIES.TRN_REF_NO = SWVW_TXN_DETAIL.TRN_REF_NO "
+ " AND STTM_BRANCH.BRANCH_CODE = ACVW_ALL_AC_ENTRIES.AC_BRANCH "
+ " AND TRN_DT BETWEEN to_date('17092020', 'ddmmyyyy') and to_date('21092020', 'ddmmyyyy') "
+ " AND((ACVW_ALL_AC_ENTRIES.AC_BRANCH = 'ETG') "
+ " OR(ACVW_ALL_AC_ENTRIES.AC_BRANCH IN(SELECT BRANCH_CODE FROM FCUBSFWA.STTM_BRANCH WHERE REGIONAL_OFFICE = 'ETG'))) "
+ " AND AC_NO IN ('351200823', --'352200905', --'352200904', '351200822', "
+ " '101000301', '101000302', '101000303', '101000304', '101000305', '101000306', '101000307', '101000308', "
+ " '101000309', '101000310', '101000311', '101000312', '101000313', '101000314', "
+ " '101000315', '101000316', '101000317', '101000318', '101000319', '101000320', '101000321', '101000322', '101000323', '101000324', '101000325', '101000326', '101000327', "
+ " '101000328', '101000329', '101000330', '101000331', '101000332', '101000333', '101000334', '101000335', '101000337', '101000338', '101000339', '101000340', "
+ " '101000341', '101000342', '101000346', '101000345', '101000344', '101000343' "
+ ") ORDER BY AC_BRANCH,PAN ";

            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = query;
            // DbCmd.CommandType = CommandType.Text;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;

        }


        public OracleDataReader ListeClientMorale(String FirstDate, String FirstDate2)
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



            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = querry;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;


        }



        public OracleDataReader CompteInfo(String numcompte)
        {
            
            string querry = "select distinct cust_ac_no, cust_no, ac_desc, alt_ac_no"
                     + " , ebjuser.get_iban_iso(cust_ac_no) as RIB"
                     + " from fcubsfwa.sttm_cust_account"
                     + " where record_stat = 'O'  and auth_stat = 'A'"
                     + "  and cust_ac_no ='" + numcompte + "' OR alt_ac_no='" + numcompte + "'";


            OracleCommand DbCmd = new OracleCommand();



            DbCmd.Connection = DbConnect.ConnectOracle();

            DbCmd.CommandText = querry;
            OracleDataReader DbReader = DbCmd.ExecuteReader();

            return DbReader;


        }


        public string CalCleRiB(string CompteNum)
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

        public string Residence(string Reside)
        {

            String Resident;
            Resident = "0";


            switch (Reside.Trim(' '))
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

        public string VeriNAP(string c)
        {

            //int nbr = 29;

            if (c.Length <= 0) { c = "NAP"; }
            else
            {
                //c.Replace('/', ' ');
                //c.Replace('\\', ' ');
                //c.Trim('\\');
                //c.Trim('/');
                if (c.Length >= 30) { c.Substring(0, 29); }


            }
            return c;
        }





        public static string buildCodeGuichet(string codAgence)
        {
            String CodeGuichet;
            CodeGuichet = "Impossible de definir le GUICHET";


            switch (codAgence)
            {
                case "701": CodeGuichet = "01701"; break;
                case "702": CodeGuichet = "01702"; break;
                case "703": CodeGuichet = "01703"; break;
                case "704": CodeGuichet = "01704"; break;
                case "705": CodeGuichet = "01705"; break;
                case "706": CodeGuichet = "01706"; break;
                case "707": CodeGuichet = "06707"; break;
                case "708": CodeGuichet = "01708"; break;
                case "709": CodeGuichet = "01709"; break;
                case "710": CodeGuichet = "01710"; break;
                case "711": CodeGuichet = "07711"; break;
                case "712": CodeGuichet = "05712"; break;
                case "713": CodeGuichet = "02713"; break;
                case "714": CodeGuichet = "04714"; break;
                case "715": CodeGuichet = "10715"; break;
                case "716": CodeGuichet = "01716"; break;
                case "717": CodeGuichet = "01717"; break;
                case "718": CodeGuichet = "01718"; break;
                case "719": CodeGuichet = "01719"; break;
                case "720": CodeGuichet = "01720"; break;
                case "721": CodeGuichet = "01721"; break;
                case "722": CodeGuichet = "01722"; break;
                case "723": CodeGuichet = "03723"; break;
                case "724": CodeGuichet = "01724"; break;

            }
            return CodeGuichet;
        }

        public string CalRib(string CleRib, string NumeroCompte)
        {

            return "TG055" + buildCodeGuichet(GetAgenceCodeFromRIB(NumeroCompte)).Trim() + NumeroCompte.Substring(4, NumeroCompte.Length - 4);

        }


        public string FileName(int version, string codeEtc)
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

