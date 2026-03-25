/*
 * =============================================================================
 * SERVICE DE CALCUL DU TABLEAU D'AMORTISSEMENT
 * =============================================================================
 * Service principal qui orchestre le calcul du tableau d'amortissement.
 * Utilise les services specialises:
 * - ITEGCalculator pour le calcul du TEG
 * - IAlerteService pour la generation des alertes
 * - ICapaciteEndettementService pour la capacite d'endettement
 * - ICalculHelper pour les calculs utilitaires
 * =============================================================================
 */

using EcoService.Models;
using EcoService.Services.Interfaces; 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EcoService.Services.Amortissement
{
    /// <summary>
    /// Implementation du service d'amortissement
    /// </summary>
    public class AmortissementService : IAmortissementService
    {
        private readonly ITEGCalculator _tegCalculator;
        private readonly IAlerteService _alerteService;
        private readonly ICapaciteEndettementService _capaciteService;
        private readonly ICalculHelper _calculHelper;

        public AmortissementService(
            ITEGCalculator tegCalculator,
            IAlerteService alerteService,
            ICapaciteEndettementService capaciteService,
            ICalculHelper calculHelper)
        {
            _tegCalculator = tegCalculator;
            _alerteService = alerteService;
            _capaciteService = capaciteService;
            _calculHelper = calculHelper;
        }

        /// <summary>
        /// Calcule le tableau d'amortissement complet
        /// </summary>
        public AmortissementResult CalculerTableau(AmortissementInput input)
        {
            // Date de deblocage (par defaut = date debut)
            DateTime dateDeblocage = input.DateDeblocage ?? input.DateDebut;

            // Traitement des encours et rachats
            var (totalEncoursRachetes, totalCommissionsRachat, totalMensualitesEncoursActifs,
                encoursRachetes, encoursActifs) = TraiterEncours(input);

            // Montant net disponible apres rachats et frais
            decimal montantNetDisponible = input.MontantPret - totalEncoursRachetes - totalCommissionsRachat - input.FraisDossier;

            // Initialisation du resultat
            var result = InitialiserResultat(input, totalEncoursRachetes, totalCommissionsRachat,
                montantNetDisponible, encoursRachetes, encoursActifs, totalMensualitesEncoursActifs);

            // Taux periodique et effectif (avec TAF)
            decimal tauxPeriodique = _calculHelper.CalculerTauxPeriodique(input.TauxAnnuel, input.Periodicite, input.TypeTaux);
            decimal tauxEffectif = tauxPeriodique * (1 + input.TauxTAF / 100);

            // Nombre d'echeances effectives (hors differe)
            int echeancesEffectives = input.NombreEcheances - input.DiffereEcheances;
            if (echeancesEffectives <= 0)
            {
                throw new ArgumentException("Le nombre d'echeances doit etre superieur au differe");
            }

            // Assurance par echeance (montant fixe uniquement)
            decimal assuranceParEcheance = _calculHelper.CalculerAssuranceParEcheance(input);

            // Calcul de la mensualite selon le mode
            var (mensualite, amortissementConstant) = CalculerMensualiteSelonMode(
                input, tauxEffectif, echeancesEffectives, result);

            result.MensualiteAvecAssurance = result.Mensualite + assuranceParEcheance;

            // Calcul du taux implicite a partir de la mensualite (methode Excel)
            // Cela garantit la coherence entre mensualite et calculs d'interets
            decimal tauxImplicite = tauxEffectif;
            if (input.ModeRemboursement == ModeRemboursement.EcheancesConstantes && mensualite > 0)
            {
                tauxImplicite = _calculHelper.CalculerTauxImplicite(
                    input.MontantPret, mensualite, echeancesEffectives, tauxEffectif);
            }

            // Generation des lignes avec le taux implicite
            var (totalInterets, totalTAF, totalAssurance, totalMensualites, totalCapitalRembourse) = GenererLignes(
                input, result, tauxImplicite, mensualite, amortissementConstant,
                assuranceParEcheance, dateDeblocage);

            // Finalisation des totaux
            FinaliserTotaux(result, input, totalInterets, totalTAF, totalAssurance, totalMensualites, totalCapitalRembourse,
                totalCommissionsRachat, totalMensualitesEncoursActifs, encoursActifs);

            // Calcul du TEG selon la formule Excel:
            // TEG = (1 + TAUX(duree; mensualiteHT; -(Montant - Frais - AssuranceClient); 0))^12 - 1
            // - Mensualite SANS assurance (result.Mensualite)
            // - Prime assurance CLIENT soustraite du capital (financee)
            // Si PrimeAssuranceClient est fournie, l'utiliser; sinon utiliser le total calcule
            decimal primeAssurancePourTEG = input.PrimeAssuranceClient > 0
                ? input.PrimeAssuranceClient
                : totalAssurance;

            result.TEGCalcule = _tegCalculator.CalculerTEG(
                input.NombreEcheances,
                result.Mensualite,          // Mensualite SANS assurance
                input.MontantPret,
                input.FraisDossier,
                primeAssurancePourTEG       // Prime d'assurance TOTALE sur la duree
            );

            // Generation des alertes
            result.Alertes = _alerteService.GenererAlertes(result, input);

            return result;
        }

        /// <summary>
        /// Calcule uniquement la mensualite (pour affichage rapide)
        /// </summary>
        public decimal CalculerMensualite(decimal montant, decimal tauxAnnuel, int nombreEcheances,
            Periodicite periodicite, decimal tauxTAF = 0, TypeTaux typeTaux = TypeTaux.Proportionnel)
        {
            decimal tauxPeriodique = _calculHelper.CalculerTauxPeriodique(tauxAnnuel, periodicite, typeTaux);
            decimal tauxEffectif = tauxPeriodique * (1 + tauxTAF / 100);
            return _calculHelper.CalculerMensualite(montant, tauxEffectif, nombreEcheances);
        }

        #region Methodes privees

        private (decimal totalRachetes, decimal totalCommissions, decimal totalMensualites,
            List<EncoursBancaire> rachetes, List<EncoursBancaire> actifs) TraiterEncours(AmortissementInput input)
        {
            decimal totalEncoursRachetes = 0;
            decimal totalCommissionsRachat = 0;
            decimal totalMensualitesEncoursActifs = 0;
            var encoursRachetes = new List<EncoursBancaire>();
            var encoursActifs = new List<EncoursBancaire>();

            foreach (var encours in input.Encours)
            {
                if (encours.ARacheter)
                {
                    encours.CommissionRachat = encours.Solde * input.TauxCommissionRachat / 100;
                    totalEncoursRachetes += encours.Solde;
                    totalCommissionsRachat += encours.CommissionRachat;
                    encours.Statut = StatutEncours.ARacheter;
                    encoursRachetes.Add(encours);
                }
                else
                {
                    encours.Statut = StatutEncours.Actif;
                    totalMensualitesEncoursActifs += encours.Mensualite;
                    encoursActifs.Add(encours);
                }
            }

            return (totalEncoursRachetes, totalCommissionsRachat, totalMensualitesEncoursActifs,
                encoursRachetes, encoursActifs);
        }

        private AmortissementResult InitialiserResultat(AmortissementInput input,
            decimal totalEncoursRachetes, decimal totalCommissionsRachat, decimal montantNetDisponible,
            List<EncoursBancaire> encoursRachetes, List<EncoursBancaire> encoursActifs,
            decimal totalMensualitesEncoursActifs)
        {
            return new AmortissementResult
            {
                MontantPret = input.MontantPret,
                TauxAnnuel = input.TauxAnnuel,
                TauxTAF = input.TauxTAF,
                TauxTTC = input.TauxAnnuel * (1m + input.TauxTAF / 100m),
                NombreEcheances = input.NombreEcheances,
                DateDebut = input.DateDebut,
                DateDeblocage = input.DateDeblocage,
                FraisDossier = input.FraisDossier,
                TEG = input.TEG,
                DiffereEcheances = input.DiffereEcheances,
                NomClient = input.NomClient,
                Client = input.Client,
                PeriodiciteLibelle = _calculHelper.GetPeriodiciteLibelle(input.Periodicite),
                ModeRemboursementLibelle = _calculHelper.GetModeRemboursementLibelle(input.ModeRemboursement),
                TotalEncoursRachetes = totalEncoursRachetes,
                TotalCommissionsRachat = totalCommissionsRachat,
                MontantNetDisponible = montantNetDisponible,
                EncoursRachetes = encoursRachetes,
                EncoursActifs = encoursActifs,
                TotalMensualitesEncoursActifs = totalMensualitesEncoursActifs,
                SalaireMensuel = input.SalaireMensuel
            };
        }

        private (decimal mensualite, decimal amortissementConstant) CalculerMensualiteSelonMode(
            AmortissementInput input, decimal tauxEffectif, int echeancesEffectives,
            AmortissementResult result)
        {
            decimal mensualite = 0;
            decimal amortissementConstant = 0;

            switch (input.ModeRemboursement)
            {
                case ModeRemboursement.EcheancesConstantes:
                    mensualite = _calculHelper.CalculerMensualite(input.MontantPret, tauxEffectif, echeancesEffectives);
                    result.Mensualite = mensualite;
                    break;

                case ModeRemboursement.AmortissementConstant:
                    amortissementConstant = input.MontantPret / echeancesEffectives;
                    decimal premiereEcheance = amortissementConstant + (input.MontantPret * tauxEffectif / 100);
                    result.Mensualite = premiereEcheance;
                    break;

                case ModeRemboursement.InFine:
                    mensualite = input.MontantPret * tauxEffectif / 100;
                    result.Mensualite = mensualite;
                    break;
            }

            return (mensualite, amortissementConstant);
        }

        private (decimal totalInterets, decimal totalTAF, decimal totalAssurance, decimal totalMensualites, decimal totalCapitalRembourse) GenererLignes(
            AmortissementInput input, AmortissementResult result, decimal tauxEffectif,
            decimal mensualite, decimal amortissementConstant, decimal assuranceParEcheance,
            DateTime dateDeblocage)
        {
            decimal capitalRestant = input.MontantPret;
            decimal totalInterets = 0;
            decimal totalTAF = 0;
            decimal totalAssurance = 0;
            decimal totalMensualites = 0;
            decimal totalCapitalRembourse = 0;
            DateTime dateEcheance = input.DateDebut;
            DateTime datePrecedente = dateDeblocage;

            // Taux periodique TTC pour le calcul Excel (par difference)
            decimal tauxPeriodiqueTTC = tauxEffectif / 100;

            for (int i = 1; i <= input.NombreEcheances; i++)
            {
                DateTime dateFinPeriode = _calculHelper.CalculerProchaineDate(datePrecedente, input.Periodicite);

                var ligne = new LigneAmortissement
                {
                    NumeroEcheance = i,
                    DateEcheance = dateEcheance,
                    CapitalRestantDebut = capitalRestant
                };

                // Gestion du differe et calcul de l'echeance selon methode Excel
                CalculerEcheanceMethodeExcel(input, ligne, i, mensualite, amortissementConstant,
                    tauxPeriodiqueTTC, ref capitalRestant);

                // Assurance (montant fixe par echeance)
                ligne.Assurance = assuranceParEcheance;
                ligne.MensualiteAvecAssurance = ligne.Mensualite + ligne.Assurance;

                // Capital restant apres cette echeance
                capitalRestant = ligne.CapitalRestantFin;

                // Cumul des totaux (somme des lignes comme Excel)
                totalInterets += ligne.Interets;
                totalTAF += ligne.TAF;
                totalAssurance += ligne.Assurance;
                totalMensualites += ligne.Mensualite;
                totalCapitalRembourse += ligne.CapitalRembourse;

                result.Lignes.Add(ligne);

                // Gestion du remboursement anticipe
                if (input.EcheanceRemboursementAnticipe > 0 &&
                    i == input.EcheanceRemboursementAnticipe && ligne.CapitalRestantFin > 0)
                {
                    if (!TraiterRemboursementAnticipe(input, result, ligne, dateEcheance, ref capitalRestant))
                        break;
                }

                datePrecedente = dateFinPeriode;
                dateEcheance = _calculHelper.CalculerProchaineDate(dateEcheance, input.Periodicite);
            }

            return (totalInterets, totalTAF, totalAssurance, totalMensualites, totalCapitalRembourse);
        }

        /// <summary>
        /// Calcule l'echeance - tous calculs en decimal (precision 3 decimales min)
        /// 1) Interet HT = Balance Debut × Taux_HT_Mensuel
        /// 2) TAF = Interet HT × Taux_TAF
        /// 3) Interet TTC = Interet HT + TAF
        /// 4) Principal = Echeance - Interet TTC
        /// 5) Balance Fin = Balance Debut - Principal
        /// </summary>
        private void CalculerEcheanceMethodeExcel(AmortissementInput input, LigneAmortissement ligne, int i,
            decimal mensualite, decimal amortissementConstant, decimal tauxPeriodiqueTTC, ref decimal capitalRestant)
        {
            decimal soldeDebut = capitalRestant;
            decimal tauxHTMensuel = input.TauxAnnuel / 100m / 12m;
            decimal tauxTAF = input.TauxTAF / 100m;

            if (i <= input.DiffereEcheances)
            {
                ligne.EstEnDiffere = true;
                decimal interetsHT = soldeDebut * tauxHTMensuel;
                decimal taf = interetsHT * tauxTAF;
                decimal interetsTTC = interetsHT + taf;

                ligne.Interets = interetsHT;
                ligne.TAF = taf;

                if (input.TypeDiffere == TypeDiffere.Total)
                {
                    ligne.CapitalRembourse = 0;
                    ligne.Mensualite = 0;
                    ligne.CapitalRestantFin = soldeDebut + interetsTTC;
                }
                else
                {
                    ligne.CapitalRembourse = 0;
                    ligne.Mensualite = interetsTTC;
                    ligne.CapitalRestantFin = capitalRestant;
                }
            }
            else
            {
                ligne.EstEnDiffere = false;

                switch (input.ModeRemboursement)
                {
                    case ModeRemboursement.EcheancesConstantes:
                        {
                            decimal interetsHT = soldeDebut * tauxHTMensuel;
                            decimal taf = interetsHT * tauxTAF;
                            decimal interetsTTC = interetsHT + taf;

                            if (i == input.NombreEcheances)
                            {
                                ligne.CapitalRembourse = capitalRestant;
                                ligne.Interets = interetsHT;
                                ligne.TAF = taf;
                                ligne.Mensualite = ligne.CapitalRembourse + interetsTTC;
                                ligne.CapitalRestantFin = 0;
                            }
                            else
                            {
                                decimal principal = mensualite - interetsTTC;
                                decimal soldeFin = soldeDebut - principal;
                                if (soldeFin < 0) soldeFin = 0;

                                ligne.Interets = interetsHT;
                                ligne.TAF = taf;
                                ligne.CapitalRembourse = principal;
                                ligne.CapitalRestantFin = soldeFin;
                                ligne.Mensualite = mensualite;
                            }
                        }
                        break;

                    case ModeRemboursement.AmortissementConstant:
                        {
                            decimal interetsHT = soldeDebut * tauxHTMensuel;
                            decimal taf = interetsHT * tauxTAF;
                            decimal interetsTTC = interetsHT + taf;
                            decimal principal = i == input.NombreEcheances ? soldeDebut : amortissementConstant;

                            ligne.Interets = interetsHT;
                            ligne.TAF = taf;
                            ligne.CapitalRembourse = principal;
                            ligne.Mensualite = principal + interetsTTC;
                            ligne.CapitalRestantFin = soldeDebut - principal;
                            if (ligne.CapitalRestantFin < 0) ligne.CapitalRestantFin = 0;
                        }
                        break;

                    case ModeRemboursement.InFine:
                        {
                            decimal interetsHT = soldeDebut * tauxHTMensuel;
                            decimal taf = interetsHT * tauxTAF;
                            decimal interetsTTC = interetsHT + taf;

                            ligne.Interets = interetsHT;
                            ligne.TAF = taf;

                            if (i == input.NombreEcheances)
                            {
                                ligne.CapitalRembourse = capitalRestant;
                                ligne.Mensualite = capitalRestant + interetsTTC;
                                ligne.CapitalRestantFin = 0;
                            }
                            else
                            {
                                ligne.CapitalRembourse = 0;
                                ligne.Mensualite = interetsTTC;
                                ligne.CapitalRestantFin = capitalRestant;
                            }
                        }
                        break;
                }
            }
        }

        private bool TraiterRemboursementAnticipe(AmortissementInput input, AmortissementResult result,
            LigneAmortissement ligne, DateTime dateEcheance, ref decimal capitalRestant)
        {
            decimal montantRA = input.MontantRemboursementAnticipe > 0
                ? Math.Min(input.MontantRemboursementAnticipe, ligne.CapitalRestantFin)
                : ligne.CapitalRestantFin;

            decimal penalite = montantRA * input.TauxPenaliteRA / 100;

            var ligneRA = new LigneAmortissement
            {
                NumeroEcheance = 0,
                DateEcheance = dateEcheance,
                CapitalRestantDebut = ligne.CapitalRestantFin,
                Interets = 0,
                TAF = 0,
                CapitalRembourse = montantRA,
                Assurance = 0,
                Mensualite = montantRA + penalite,
                MensualiteAvecAssurance = montantRA + penalite,
                CapitalRestantFin = ligne.CapitalRestantFin - montantRA,
                EstRemboursementAnticipe = true,
                PenaliteRA = penalite
            };

            result.Lignes.Add(ligneRA);
            result.PenaliteRA = penalite;
            result.ARemboursementAnticipe = true;

            if (ligneRA.CapitalRestantFin <= 0)
                return false;

            capitalRestant = ligneRA.CapitalRestantFin;
            return true;
        }

        private void FinaliserTotaux(AmortissementResult result, AmortissementInput input,
            decimal totalInterets, decimal totalTAF, decimal totalAssurance,
            decimal totalMensualites, decimal totalCapitalRembourse,
            decimal totalCommissionsRachat, decimal totalMensualitesEncoursActifs,
            List<EncoursBancaire> encoursActifs)
        {
            result.TotalInterets = totalInterets;
            result.TotalTAF = totalTAF;
            result.TotalAssurance = totalAssurance;
            result.TotalMensualites = totalMensualites;
            result.TotalCapitalRembourse = totalCapitalRembourse;
            result.CoutTotalCredit = totalInterets + totalTAF + totalAssurance +
                input.FraisDossier + result.PenaliteRA + totalCommissionsRachat;
            result.DateFin = result.Lignes.Last().DateEcheance;

            // Capacite d'endettement
            if (input.SalaireMensuel > 0)
            {
                var (tauxMax, capaciteMax) = _capaciteService.CalculerCapaciteEndettement(
                    input.SalaireMensuel, input.ProfilClient);
                result.TauxEndettementMax = tauxMax;
                result.CapaciteRemboursementMax = capaciteMax;

                result.TotalChargesMensuelles = result.MensualiteAvecAssurance +
                    totalMensualitesEncoursActifs + input.AutresCharges;

                result.TauxEndettementActuel = input.SalaireMensuel > 0
                    ? result.TotalChargesMensuelles / input.SalaireMensuel * 100
                    : 0;

                result.ResteAVivre = input.SalaireMensuel - result.TotalChargesMensuelles;
            }

            // Total encours global et garantie
            decimal totalEncoursActifs = encoursActifs.Sum(e => e.Solde);
            result.TotalEncoursGlobal = input.MontantPret + totalEncoursActifs;
            result.GarantieHypothecaireRequise = result.TotalEncoursGlobal >
                ConstantesReglementaires.SEUIL_GARANTIE_HYPOTHECAIRE;
        }

        #endregion
    }
}