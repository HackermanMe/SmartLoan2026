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
            // Créer un flux mémoire pour le document
            using (var stream = new MemoryStream())
            {
                // Ouvrir le modèle de document en lecture
                using (var wordDoc = WordprocessingDocument.Open(templatePath, false))
                {
                    // Créer une copie du document dans le flux mémoire
                    wordDoc.Clone(stream);

                    // Manipuler le document en utilisant le flux mémoire
                    using (var newDoc = WordprocessingDocument.Open(stream, true))
                    {
                        // Accéder au corps du document
                        var docText = newDoc.MainDocumentPart.Document.InnerText;

                        // Remplacer les champs de fusion par les valeurs fournies
                        foreach (var field in fieldValues)
                        {
                            docText = docText.Replace($"<<{field.Key}>>", field.Value);
                        }

                        // Enregistrer les modifications
                        newDoc.MainDocumentPart.Document.Save();
                    }
                }

                // Retourner le document généré sous forme de tableau d'octets
                return stream.ToArray();
            }
        }

    }
}