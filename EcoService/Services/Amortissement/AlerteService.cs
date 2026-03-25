using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/*
 * =============================================================================
 * SERVICE DE GENERATION DES ALERTES
 * =============================================================================
 * Genere les alertes de conformite pour un dossier de credit:
 * - Alerte TEG > 14%
 * - Alerte garantie hypothecaire > 25 millions
 * - Alerte capacite d'endettement depassee
 * - Alerte reste a vivre negatif
 * - Alerte nombre d'encours > 6
 * =============================================================================
 */

using EcoService.Models;

namespace EcoService.Services.Amortissement
{
    /// <summary>
    /// Interface pour la generation des alertes
    /// </summary>
    public interface IAlerteService
    {
        /// <summary>
        /// Genere les alertes de conformite pour un dossier
        /// </summary>
        List<AlerteCredit> GenererAlertes(AmortissementResult result, AmortissementInput input);
    }

    /// <summary>
    /// Implementation du service d'alertes
    /// </summary>
    public class AlerteService : IAlerteService
    {
        /// <summary>
        /// Genere les alertes de conformite pour un dossier
        /// </summary>
        public List<AlerteCredit> GenererAlertes(AmortissementResult result, AmortissementInput input)
        {
            var alertes = new List<AlerteCredit>();

            // Alerte TEG > 14%
            VerifierTEG(alertes, result);

            // Alerte Garantie Hypothecaire > 25 millions
            VerifierGarantieHypothecaire(alertes, result);

            // Alerte Capacite d'endettement
            VerifierCapaciteEndettement(alertes, result, input);

            // Alerte Reste a vivre faible
            VerifierResteAVivre(alertes, result, input);

            // Alerte Nombre d'encours > 6
            VerifierNombreEncours(alertes, input);

            // Information: Montant net disponible
            AjouterInfoRachat(alertes, result);

            return alertes;
        }

        private void VerifierTEG(List<AlerteCredit> alertes, AmortissementResult result)
        {
            if (result.TEGCalcule > ConstantesReglementaires.PLAFOND_TEG)
            {
                alertes.Add(new AlerteCredit
                {
                    Code = "TEG_DEPASSE",
                    Type = TypeAlerte.Critique,
                    Titre = "TEG superieur au plafond",
                    Message = $"Le TEG calcule ({result.TEGCalcule:F2}%) depasse le plafond reglementaire de {ConstantesReglementaires.PLAFOND_TEG}%. Ce credit ne peut pas etre accorde en l'etat.",
                    ValeurActuelle = result.TEGCalcule,
                    Seuil = ConstantesReglementaires.PLAFOND_TEG,
                    EstBloquante = true
                });
            }
            else if (result.TEGCalcule > ConstantesReglementaires.PLAFOND_TEG - 1)
            {
                // Avertissement si proche du plafond
                alertes.Add(new AlerteCredit
                {
                    Code = "TEG_PROCHE_PLAFOND",
                    Type = TypeAlerte.Avertissement,
                    Titre = "TEG proche du plafond",
                    Message = $"Le TEG calcule ({result.TEGCalcule:F2}%) est proche du plafond reglementaire de {ConstantesReglementaires.PLAFOND_TEG}%.",
                    ValeurActuelle = result.TEGCalcule,
                    Seuil = ConstantesReglementaires.PLAFOND_TEG,
                    EstBloquante = false
                });
            }
        }

        private void VerifierGarantieHypothecaire(List<AlerteCredit> alertes, AmortissementResult result)
        {
            if (result.GarantieHypothecaireRequise)
            {
                alertes.Add(new AlerteCredit
                {
                    Code = "GARANTIE_HYPOTHECAIRE",
                    Type = TypeAlerte.Critique,
                    Titre = "Garantie hypothecaire requise",
                    Message = $"Le total des encours ({result.TotalEncoursGlobal:N0}) depasse {ConstantesReglementaires.SEUIL_GARANTIE_HYPOTHECAIRE:N0}. Une garantie hypothecaire est obligatoire.",
                    ValeurActuelle = result.TotalEncoursGlobal,
                    Seuil = ConstantesReglementaires.SEUIL_GARANTIE_HYPOTHECAIRE,
                    EstBloquante = false // Pas bloquante mais requiert une action
                });
            }
        }

        private void VerifierCapaciteEndettement(List<AlerteCredit> alertes, AmortissementResult result, AmortissementInput input)
        {
            if (input.SalaireMensuel > 0 && result.TauxEndettementActuel > result.TauxEndettementMax)
            {
                alertes.Add(new AlerteCredit
                {
                    Code = "ENDETTEMENT_DEPASSE",
                    Type = TypeAlerte.Critique,
                    Titre = "Capacite d'endettement depassee",
                    Message = $"Le taux d'endettement ({result.TauxEndettementActuel:F1}%) depasse le maximum autorise ({result.TauxEndettementMax:F0}%) pour ce profil client.",
                    ValeurActuelle = result.TauxEndettementActuel,
                    Seuil = result.TauxEndettementMax,
                    EstBloquante = true
                });
            }
            else if (input.SalaireMensuel > 0 && result.TauxEndettementActuel > result.TauxEndettementMax * 0.9m)
            {
                // Avertissement si proche de la limite
                alertes.Add(new AlerteCredit
                {
                    Code = "ENDETTEMENT_PROCHE_LIMITE",
                    Type = TypeAlerte.Avertissement,
                    Titre = "Endettement proche de la limite",
                    Message = $"Le taux d'endettement ({result.TauxEndettementActuel:F1}%) approche le maximum autorise ({result.TauxEndettementMax:F0}%).",
                    ValeurActuelle = result.TauxEndettementActuel,
                    Seuil = result.TauxEndettementMax,
                    EstBloquante = false
                });
            }
        }

        private void VerifierResteAVivre(List<AlerteCredit> alertes, AmortissementResult result, AmortissementInput input)
        {
            if (input.SalaireMensuel > 0 && result.ResteAVivre < 0)
            {
                alertes.Add(new AlerteCredit
                {
                    Code = "RESTE_A_VIVRE_NEGATIF",
                    Type = TypeAlerte.Critique,
                    Titre = "Reste a vivre negatif",
                    Message = $"Le reste a vivre est negatif ({result.ResteAVivre:N0}). Le client ne peut pas supporter cette charge.",
                    ValeurActuelle = result.ResteAVivre,
                    Seuil = 0,
                    EstBloquante = true
                });
            }
        }

        private void VerifierNombreEncours(List<AlerteCredit> alertes, AmortissementInput input)
        {
            if (input.Encours.Count > ConstantesReglementaires.MAX_ENCOURS)
            {
                alertes.Add(new AlerteCredit
                {
                    Code = "TROP_ENCOURS",
                    Type = TypeAlerte.Avertissement,
                    Titre = "Nombre d'encours eleve",
                    Message = $"Le client a {input.Encours.Count} encours, ce qui depasse la limite recommandee de {ConstantesReglementaires.MAX_ENCOURS}.",
                    ValeurActuelle = input.Encours.Count,
                    Seuil = ConstantesReglementaires.MAX_ENCOURS,
                    EstBloquante = false
                });
            }
        }

        private void AjouterInfoRachat(List<AlerteCredit> alertes, AmortissementResult result)
        {
            if (result.TotalEncoursRachetes > 0)
            {
                alertes.Add(new AlerteCredit
                {
                    Code = "INFO_RACHAT",
                    Type = TypeAlerte.Information,
                    Titre = "Rachat de credits",
                    Message = $"Rachat de {result.EncoursRachetes.Count} credit(s) pour un total de {result.TotalEncoursRachetes:N0} + {result.TotalCommissionsRachat:N0} de commission. Montant net disponible: {result.MontantNetDisponible:N0}.",
                    ValeurActuelle = result.MontantNetDisponible,
                    EstBloquante = false
                });
            }
        }
    }
}
