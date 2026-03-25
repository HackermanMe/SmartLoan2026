/*
 * =============================================================================
 * SERVICE DE CALCUL DE LA CAPACITE D'ENDETTEMENT
 * =============================================================================
 * Calcule la capacite d'endettement (quotite) selon le profil client:
 * - Salarie avec salaire >= 100 000 : 50% max
 * - Salarie avec salaire entre 75 001 et 99 999 : 40% max
 * - Salarie avec salaire <= 75 000 : 33% max
 * - Retraite (quel que soit le salaire) : 33% max
 * =============================================================================
 */

using EcoService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EcoService.Services.Amortissement
{
    /// <summary>
    /// Interface pour le calcul de la capacite d'endettement
    /// </summary>
    public interface ICapaciteEndettementService
    {
        /// <summary>
        /// Calcule la capacite d'endettement selon le profil client
        /// </summary>
        /// <param name="salaire">Salaire mensuel du client</param>
        /// <param name="profil">Profil du client</param>
        /// <returns>Tuple (taux max autorise, capacite max en montant)</returns>
        (decimal tauxMax, decimal capaciteMax) CalculerCapaciteEndettement(decimal salaire, ProfilClient profil);
    }

    /// <summary>
    /// Implementation du service de capacite d'endettement
    /// </summary>
    public class CapaciteEndettementService : ICapaciteEndettementService
    {
        /// <summary>
        /// Calcule la capacite d'endettement selon le profil client
        /// Quotites:
        /// - 50% si salaire >= 100 000
        /// - 40% si salaire entre 75 001 et 99 999
        /// - 33% si salaire <= 75 000 OU retraite
        /// </summary>
        public (decimal tauxMax, decimal capaciteMax) CalculerCapaciteEndettement(decimal salaire, ProfilClient profil)
        {
            decimal tauxMax;

            // Les retraites ont toujours 33% max, quel que soit le salaire
            if (profil == ProfilClient.Retraite)
            {
                tauxMax = ConstantesReglementaires.TAUX_ENDETTEMENT_SALARIE_FAIBLE; // 33%
            }
            // Salaire >= 100 000 : 50%
            else if (salaire >= ConstantesReglementaires.SEUIL_SALAIRE_HAUT)
            {
                tauxMax = ConstantesReglementaires.TAUX_ENDETTEMENT_SALARIE_HAUT; // 50%
            }
            // Salaire entre 75 001 et 99 999 : 40%
            else if (salaire > ConstantesReglementaires.SEUIL_SALAIRE_MOYEN)
            {
                tauxMax = ConstantesReglementaires.TAUX_ENDETTEMENT_SALARIE_MOYEN; // 40%
            }
            // Salaire <= 75 000 : 33%
            else
            {
                tauxMax = ConstantesReglementaires.TAUX_ENDETTEMENT_SALARIE_FAIBLE; // 33%
            }

            decimal capaciteMax = salaire * tauxMax / 100;

            return (tauxMax, capaciteMax);
        }
    }
}