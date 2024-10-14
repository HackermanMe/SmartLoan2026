using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace EcoService.Models
{
    public class WordTableDocumentService
    {
        public byte[] GenerateDocument(string templatePath, Dictionary<string, string> fieldValues, List<List<string>> table1Data, List<List<string>> table2Data)
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
                        // Parcourir chaque paragraphe du document pour les champs de fusion
                        foreach (var paragraph in newDoc.MainDocumentPart.Document.Body.Descendants<Paragraph>())
                        {
                            // Concaténer tout le texte du paragraphe
                            string paragraphText = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));

                            // Remplacer les champs de fusion dans le texte du paragraphe
                            foreach (var field in fieldValues)
                            {
                                if (paragraphText.Contains($"<<{field.Key}>>"))
                                {
                                    paragraphText = paragraphText.Replace($"<<{field.Key}>>", field.Value);
                                }
                            }

                            // Supprimer tous les éléments `Text` existants du paragraphe
                            foreach (var text in paragraph.Descendants<Text>())
                            {
                                text.Remove();
                            }

                            // Ajouter le texte modifié au paragraphe
                            paragraph.AppendChild(new Run(new Text(paragraphText)));
                        }

                        // Remplir le premier tableau avec les données
                        var table1 = newDoc.MainDocumentPart.Document.Body.Descendants<Table>().ElementAt(0);
                        FillTableWithData(table1, table1Data);

                        // Remplir le deuxième tableau avec les données
                        var table2 = newDoc.MainDocumentPart.Document.Body.Descendants<Table>().ElementAt(1);
                        FillTableWithData(table2, table2Data);

                        // Enregistrer les modifications
                        newDoc.MainDocumentPart.Document.Save();
                    }
                }

                // Retourner le document généré sous forme de tableau d'octets
                return stream.ToArray();
            }
        }

        // Méthode pour remplir un tableau avec des données
        private void FillTableWithData(Table table, List<List<string>> tableData)
        {
            // Parcourir chaque ligne de données
            foreach (var rowData in tableData)
            {
                // Créer une nouvelle ligne dans le tableau
                var newRow = new TableRow();

                // Parcourir chaque cellule de la ligne
                foreach (var cellData in rowData)
                {
                    // Créer une nouvelle cellule et y ajouter le texte
                    var newCell = new TableCell(new Paragraph(new Run(new Text(cellData))));

                    // Ajouter la cellule à la ligne
                    newRow.Append(newCell);
                }

                // Ajouter la nouvelle ligne au tableau
                table.Append(newRow);
            }
        }
    }
}
