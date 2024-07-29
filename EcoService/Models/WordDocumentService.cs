using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;

namespace EcoService.Models
{
    public class WordDocumentService
    {
        public byte[] GenerateDocument(string templatePath, Dictionary<string, string> fieldValues)
        {
            using (var stream = new MemoryStream())
            {
                // Ouvrir le document modèle
                using (var wordDoc = WordprocessingDocument.Open(templatePath, true))
                {
                    var docText = File.ReadAllText(templatePath);

                    // Remplacer les champs de fusion par les valeurs fournies
                    foreach (var field in fieldValues)
                    {
                        docText = docText.Replace($"<<{field.Key}>>", field.Value);
                    }

                    File.WriteAllText(templatePath, docText);
                }

                return stream.ToArray();
            }
        }
    }
}