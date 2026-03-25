using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;

namespace EcoService.Models
{
    public class EcoCerLogger
    {
        private readonly string _logFilePath;

      
        public EcoCerLogger() { } // Constructeur sans paramètre requis

        public EcoCerLogger(string logFilePath = "C:\\Logs\\EcoCerLog.xml")
        {
            _logFilePath = logFilePath;

            string logDirectory = Path.GetDirectoryName(_logFilePath);

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            if (!File.Exists(_logFilePath))
            {
                using (var writer = XmlWriter.Create(_logFilePath, new XmlWriterSettings { Indent = true }))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Logs");
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
        }

        public void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogError(string message, Exception ex = null)
        {
            string errorDetails = ex != null ? $"{message}\nException: {ex}" : message;
            WriteLog("ERROR", errorDetails);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARNING", message);
        }

        private void WriteLog(string logLevel, string message)
        {
            try
            {
                const long maxFileSize = 5 * 1024 * 1024;
                FileInfo logFile = new FileInfo(_logFilePath);

                if (logFile.Length > maxFileSize)
                {
                    using (var writer = XmlWriter.Create(_logFilePath, new XmlWriterSettings { Indent = true }))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("Logs");
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                }

                var doc = new XmlDocument();
                doc.Load(_logFilePath);

                XmlNode root = doc.DocumentElement;

                XmlElement logEntry = doc.CreateElement("EcoCerLog");

                XmlElement level = doc.CreateElement("Level");
                level.InnerText = logLevel;

                XmlElement time = doc.CreateElement("TimesTamp");
                time.InnerText = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

                XmlElement details = doc.CreateElement("Message");
                details.InnerText = message;

                logEntry.AppendChild(level);
                logEntry.AppendChild(time);
                logEntry.AppendChild(details);

                root.AppendChild(logEntry);

                doc.Save(_logFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write log to EcoCerLog: {ex.Message}");
            }
        }
    }
}