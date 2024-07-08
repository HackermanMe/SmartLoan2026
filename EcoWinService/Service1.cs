using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
//using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Configuration.Install;
using System.Net;


namespace EcoWinService
{
    [RunInstaller(true)]
    public class ServiceInstall : Installer
    {
        public ServiceInstall() : base()
        {
            //définition du compte sous lequel le service sera lancé : compte système
            ServiceProcessInstaller process = new ServiceProcessInstaller();
            process.Account = ServiceAccount.LocalSystem;

            //définition du mode de lancement (manuel), le nom du service et sa description
            ServiceInstaller service = new ServiceInstaller();
            service.StartType = ServiceStartMode.Manual;
            service.ServiceName = "EcoWinService";
            service.DisplayName = "ECO Windows Service";
            service.Description = "ECO Windows Service IT ";

            //Ajout des installeurs à la collection
            Installers.Add(service);
            Installers.Add(process);
        }
    }
    public partial class Service1 : ServiceBase
    {
        private Timer t = null;

        public Service1()
        {
            InitializeComponent();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            t = new Timer(10000); //Timer de 10 secondes
            t.Elapsed += new ElapsedEventHandler(EricScript);
            t.Start();
        }

        private void segnoScript(object sender, ElapsedEventArgs e)
        {
            var link = @"D:\temp\test.txt";
            if (File.Exists(link))
            {
                StreamWriter sw = new StreamWriter(link);
                sw.WriteLine(DateTime.Now.ToString());
                sw.Close();
            }
            else
            {
                TextWriter file = File.CreateText(link);
                file.WriteLine(DateTime.Now.ToString());
                file.Close();
            }
        }

        private void EricScript(object sender, ElapsedEventArgs e)
        {
            //var link = @"D:\temp\test.txt"; 
            var url = @"http://localhost:63262/ClientListe/ExtraireExcel?beginDate=14-06-2020&endDate=14-06-2020";
            // string url = @"http://localhost:63262/Personne/ExportToExcel2?beginDate=06-10-2017&endDate=06-10-2017";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.GetResponse();
        }

        protected override void OnStop()
        {
            t.Stop();
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
        }
        protected override void OnPause()
        {
            base.OnPause();
        }

        protected override void OnContinue()
        {
            base.OnContinue();
        }


    }
}

