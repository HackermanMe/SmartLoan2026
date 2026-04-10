/*
 * =============================================================================
 * MODELES POUR LE TABLEAU D'AMORTISSEMENT - MODULE PRET BANCAIRE COMPLET
 * =============================================================================
 * Ce fichier contient tous les modeles de donnees necessaires pour le calcul
 * et l'affichage du tableau d'amortissement bancaire.
 *
 * Structure:
 * - AmortissementInput    : Parametres d'entree saisis par l'utilisateur
 * - LigneAmortissement    : Une ligne du tableau d'amortissement
 * - AmortissementResult   : Resultat complet avec resume et lignes detaillees
 * - ClientInfo            : Informations completes du client
 * - EncoursBancaire       : Credit existant du client
 * - AlerteCredit          : Alertes de conformite
 * - Periodicite           : Enum pour la frequence des echeances
 * =============================================================================
 */
using System.ComponentModel.DataAnnotations;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;


namespace EcoService.Models
{
    /* =========================================================================
     * ENUMERATION : TYPE D'ALERTE
     * ========================================================================= */
    public enum TypeAlerte
    {
        Information = 0,    // Information simple
        Avertissement = 1,  // Attention requise
        Critique = 2        // Blocage ou action obligatoire
    }

    /* =========================================================================
     * ENUMERATION : STATUT DE L'ENCOURS
     * ========================================================================= */
    public enum StatutEncours
    {
        Actif = 1,          // Credit en cours de remboursement
        ARacheter = 2,      // Credit selectionne pour rachat
        Rachete = 3         // Credit deja rachete/solde
    }

    /* =========================================================================
     * ENUMERATION : TYPE DE PROFIL CLIENT
     * ========================================================================= */
    public enum ProfilClient
    {
        Salarie = 1,        // Employe avec salaire >= 100 000 (50% endettement max)
        SalarieMoyen = 2,   // Employe avec salaire 75 000 - 100 000 (40% endettement max)
        SalarieFaible = 3,  // Employe avec salaire <= 75 000 (33% endettement max)
        Retraite = 4        // Retraite (33% endettement max)
    }

    /* =========================================================================
     * ENUMERATION : SITUATION MATRIMONIALE
     * ========================================================================= */
    public enum SituationMatrimoniale
    {
        Celibataire = 1,
        Marie = 2,
        Divorce = 3,
        Veuf = 4,
        UnionLibre = 5
    }

    /* =========================================================================
     * ENUMERATION : TYPE DE PIECE D'IDENTITE
     * ========================================================================= */
    public enum TypePieceIdentite
    {
        CNI = 1,            // Carte Nationale d'Identite
        Passeport = 2,
        CarteSejour = 3,
        PermisConduire = 4
    }
    /* =========================================================================
     * ENUMERATION : PERIODICITE DES ECHEANCES
     * ========================================================================= */
    public enum Periodicite
    {
        Mensuel = 1,        // 12 echeances par an
        Trimestriel = 3,    // 4 echeances par an
        Semestriel = 6,     // 2 echeances par an
        Annuel = 12         // 1 echeance par an
    }

    /* =========================================================================
     * ENUMERATION : MODE DE REMBOURSEMENT
     * ========================================================================= */
    public enum ModeRemboursement
    {
        EcheancesConstantes = 1,    // Mensualite fixe (amortissement progressif)
        AmortissementConstant = 2,  // Capital fixe (echeances degressives)
        InFine = 3                  // Interets uniquement, capital a la fin
    }

    /* =========================================================================
     * ENUMERATION : TYPE DE DIFFERE
     * ========================================================================= */
    public enum TypeDiffere
    {
        Aucun = 0,          // Pas de differe
        Partiel = 1,        // Paiement des interets uniquement
        Total = 2           // Aucun paiement (interets capitalises)
    }

    /* =========================================================================
     * ENUMERATION : CONVENTION DE JOURS
     * ========================================================================= */
    public enum ConventionJours
    {
        Jours360 = 1,       // 30/360 - Chaque mois = 30 jours, annee = 360 jours
        JoursExacts = 2     // Exact/365 - Jours reels entre dates, annee = 365 jours
    }

    /* =========================================================================
     * ENUMERATION : TYPE DE TAUX
     * ========================================================================= */
    public enum TypeTaux
    {
        Proportionnel = 1,  // Taux periodique = Taux annuel / Nb periodes
        Actuariel = 2       // Taux periodique = (1 + Taux annuel)^(1/Nb periodes) - 1
    }

    /* =========================================================================
     * ENUMERATION : BASE DE CALCUL DE L'ASSURANCE
     * ========================================================================= */
    public enum BaseAssurance
    {
        CapitalInitial = 1, // Assurance calculee sur le capital initial (fixe)
        CapitalRestant = 2  // Assurance calculee sur le capital restant du (degressive)
    }

    /* =========================================================================
     * CLASSE : PARAMETRES D'ENTREE DU PRET
     * =========================================================================
     * Contient toutes les donnees saisies par l'utilisateur pour generer
     * le tableau d'amortissement. Les validations garantissent l'integrite.
     * ========================================================================= */
    public class AmortissementInput
    {
        // =====================================================================
        // INFORMATIONS DE BASE DU PRET
        // =====================================================================

        /// <summary>
        /// Montant total emprunte en unite monetaire locale
        /// </summary>
        [Required(ErrorMessage = "Le montant du pret est obligatoire")]
        [Range(1, double.MaxValue, ErrorMessage = "Le montant doit etre positif")]
        public decimal MontantPret { get; set; }

        /// <summary>
        /// Taux d'interet annuel HT en pourcentage (ex: 7.75 pour 7.75%)
        /// </summary>
        [Required(ErrorMessage = "Le taux est obligatoire")]
        [Range(0, 100, ErrorMessage = "Le taux doit etre entre 0 et 100")]
        public decimal TauxAnnuel { get; set; }

        /// <summary>
        /// Nombre total d'echeances pour rembourser le pret
        /// </summary>
        [Required(ErrorMessage = "Le nombre d'echeances est obligatoire")]
        [Range(1, 600, ErrorMessage = "Le nombre d'echeances doit etre entre 1 et 600")]
        public int NombreEcheances { get; set; }

        /// <summary>
        /// Date de la premiere echeance du pret
        /// </summary>
        [Required(ErrorMessage = "La date de debut est obligatoire")]
        public DateTime DateDebut { get; set; } = DateTime.Today;

        /// <summary>
        /// Date de deblocage des fonds (optionnelle)
        /// Si non renseignee, on considere que le deblocage a lieu a la date de debut
        /// </summary>
        public DateTime? DateDeblocage { get; set; }

        /// <summary>
        /// Frais de dossier (montant fixe preleve au deblocage)
        /// Inclus dans le calcul du TEG
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Les frais de dossier doivent etre positifs")]
        public decimal FraisDossier { get; set; } = 0;

        // =====================================================================
        // PARAMETRES D'ASSURANCE (OPTIONNELS)
        // =====================================================================

        /// <summary>
        /// Prime d'assurance CLIENT (montant total paye par le client)
        /// Ex: 575 326 FCFA pour un pret de 84 mois
        /// Cette valeur est utilisee pour le calcul du TEG (soustraite du capital net)
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "La prime d'assurance client doit etre positive")]
        public decimal PrimeAssuranceClient { get; set; } = 0;

        /// <summary>
        /// Commission ETG sur l'assurance
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "La commission ETG doit etre positive")]
        public decimal CommissionETG { get; set; } = 0;

        /// <summary>
        /// Prime d'assurance GTA C2A (calculee = PrimeAssuranceClient - CommissionETG)
        /// </summary>
        public decimal PrimeAssuranceGTAC2A => PrimeAssuranceClient - CommissionETG;

        /// <summary>
        /// Montant fixe d'assurance par echeance
        /// Note: Utilise pour l'affichage dans le tableau, pas pour le TEG
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "L'assurance fixe doit etre positive")]
        public decimal AssuranceFixe { get; set; } = 0;

        // =====================================================================
        // PARAMETRES FISCAUX
        // =====================================================================

        /// <summary>
        /// Taux Effectif Global - saisi manuellement
        /// Represente le cout total du credit incluant tous les frais
        /// </summary>
        [Range(0, 100, ErrorMessage = "Le TEG doit etre entre 0 et 100")]
        public decimal TEG { get; set; } = 0;

        /// <summary>
        /// Taux de la TAF (Taxe sur Activites Financieres) en % des interets
        /// Par defaut 10% des interets de chaque echeance
        /// </summary>
        [Range(0, 100, ErrorMessage = "Le taux TAF doit etre entre 0 et 100")]
        public decimal TauxTAF { get; set; } = 10;

        // =====================================================================
        // PARAMETRES AVANCES
        // =====================================================================

        /// <summary>
        /// Periodicite des remboursements
        /// </summary>
        public Periodicite Periodicite { get; set; } = Periodicite.Mensuel;

        /// <summary>
        /// Mode de remboursement du pret
        /// </summary>
        public ModeRemboursement ModeRemboursement { get; set; } = ModeRemboursement.EcheancesConstantes;

        /// <summary>
        /// Nombre d'echeances de differe
        /// </summary>
        [Range(0, 120, ErrorMessage = "Le differe doit etre entre 0 et 120")]
        public int DiffereEcheances { get; set; } = 0;

        /// <summary>
        /// Type de differe (partiel = interets payes, total = interets capitalises)
        /// </summary>
        public TypeDiffere TypeDiffere { get; set; } = TypeDiffere.Partiel;

        /// <summary>
        /// Convention de calcul des jours pour les interets
        /// 30/360: mois de 30 jours, annee de 360 jours
        /// Exact/365: jours reels entre dates, annee de 365 jours
        /// </summary>
        public ConventionJours ConventionJours { get; set; } = ConventionJours.Jours360;

        /// <summary>
        /// Type de taux pour le calcul du taux periodique
        /// Proportionnel: taux annuel / nb periodes
        /// Actuariel: (1 + taux annuel)^(1/nb periodes) - 1
        /// </summary>
        public TypeTaux TypeTaux { get; set; } = TypeTaux.Proportionnel;

        /// <summary>
        /// Base de calcul de l'assurance
        /// CapitalInitial: montant fixe sur le capital emprunte
        /// CapitalRestant: montant degressif sur le CRD
        /// </summary>
        public BaseAssurance BaseAssurance { get; set; } = BaseAssurance.CapitalInitial;

        // =====================================================================
        // REMBOURSEMENT ANTICIPE
        // =====================================================================

        /// <summary>
        /// Numero de l'echeance a laquelle effectuer le remboursement anticipe
        /// 0 = pas de remboursement anticipe
        /// </summary>
        [Range(0, 600, ErrorMessage = "Le numero d'echeance doit etre entre 0 et 600")]
        public int EcheanceRemboursementAnticipe { get; set; } = 0;

        /// <summary>
        /// Montant du remboursement anticipe
        /// 0 = remboursement total du capital restant
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Le montant doit etre positif")]
        public decimal MontantRemboursementAnticipe { get; set; } = 0;

        /// <summary>
        /// Taux de penalite pour remboursement anticipe (en % du capital rembourse)
        /// Typiquement 3% ou equivalent a 6 mois d'interets
        /// </summary>
        [Range(0, 100, ErrorMessage = "Le taux de penalite doit etre entre 0 et 100")]
        public decimal TauxPenaliteRA { get; set; } = 3;

        /// <summary>
        /// Nom ou reference du client (optionnel)
        /// </summary>
        public string? NomClient { get; set; }

        // =====================================================================
        // INFORMATIONS CLIENT COMPLETES
        // =====================================================================

        /// <summary>
        /// Informations detaillees du client
        /// </summary>
        public ClientInfo? Client { get; set; }

        // =====================================================================
        // GESTION DES ENCOURS (jusqu'a 6 credits existants)
        // =====================================================================

        /// <summary>
        /// Liste des credits existants du client (max 6)
        /// </summary>
        public List<EncoursBancaire> Encours { get; set; } = new List<EncoursBancaire>();

        /// <summary>
        /// Commission de rachat en pourcentage (defaut 2%)
        /// </summary>
        [Range(0, 10, ErrorMessage = "La commission de rachat doit etre entre 0 et 10%")]
        public decimal TauxCommissionRachat { get; set; } = 2;

        // =====================================================================
        // PARAMETRES DE CONFORMITE
        // =====================================================================

        /// <summary>
        /// Salaire mensuel du client pour calcul capacite d'endettement
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Le salaire doit etre positif")]
        public decimal SalaireMensuel { get; set; } = 0;

        /// <summary>
        /// Profil du client (determine le taux d'endettement max)
        /// </summary>
        public ProfilClient ProfilClient { get; set; } = ProfilClient.Salarie;

        /// <summary>
        /// Autres charges mensuelles du client (loyer, pensions, etc.)
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Les charges doivent etre positives")]
        public decimal AutresCharges { get; set; } = 0;

        /// <summary>
        /// Objet du credit (achat vehicule, travaux, tresorerie, etc.)
        /// </summary>
        public string? ObjetCredit { get; set; }
    }

    /* =========================================================================
     * CLASSE : INFORMATIONS CLIENT
     * =========================================================================
     * Contient toutes les informations personnelles du client pour les
     * documents contractuels.
     * ========================================================================= */
    public class ClientInfo
    {
        /// <summary>
        /// Numero de compte bancaire
        /// </summary>
        [Required(ErrorMessage = "Le numero de compte est obligatoire")]
        public string NumeroCompte { get; set; } = string.Empty;

        /// <summary>
        /// Intitule du compte (nom tel qu'enregistre en banque)
        /// </summary>
        public string? IntituleCompte { get; set; }

        /// <summary>
        /// Nom de famille
        /// </summary>
        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        /// <summary>
        /// Prenom(s)
        /// </summary>
        [Required(ErrorMessage = "Le prenom est obligatoire")]
        public string Prenom { get; set; } = string.Empty;

        /// <summary>
        /// Date de naissance
        /// </summary>
        public DateTime? DateNaissance { get; set; }

        /// <summary>
        /// Lieu de naissance
        /// </summary>
        public string? LieuNaissance { get; set; }

        /// <summary>
        /// Adresse complete
        /// </summary>
        public string? Adresse { get; set; }

        /// <summary>
        /// Ville
        /// </summary>
        public string? Ville { get; set; }

        /// <summary>
        /// Code postal
        /// </summary>
        public string? CodePostal { get; set; }

        /// <summary>
        /// Telephone
        /// </summary>
        public string? Telephone { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        [EmailAddress(ErrorMessage = "L'email n'est pas valide")]
        public string? Email { get; set; }

        /// <summary>
        /// Situation matrimoniale
        /// </summary>
        public SituationMatrimoniale SituationMatrimoniale { get; set; } = SituationMatrimoniale.Celibataire;

        /// <summary>
        /// Type de piece d'identite
        /// </summary>
        public TypePieceIdentite TypePieceIdentite { get; set; } = TypePieceIdentite.CNI;

        /// <summary>
        /// Numero de la piece d'identite
        /// </summary>
        public string? NumeroPieceIdentite { get; set; }

        /// <summary>
        /// Date de delivrance de la piece d'identite
        /// </summary>
        public DateTime? DateDelivrancePiece { get; set; }

        /// <summary>
        /// Lieu de delivrance de la piece d'identite
        /// </summary>
        public string? LieuDelivrancePiece { get; set; }

        /// <summary>
        /// Date d'expiration de la piece d'identite
        /// </summary>
        public DateTime? DateExpirationPiece { get; set; }

        /// <summary>
        /// Employeur actuel
        /// </summary>
        public string? Employeur { get; set; }

        /// <summary>
        /// Profession
        /// </summary>
        public string? Profession { get; set; }

        /// <summary>
        /// Nationalité
        /// </summary>
        public string? Nationalite { get; set; }

        /// <summary>
        /// Date d'embauche
        /// </summary>
        public DateTime? DateEmbauche { get; set; }

        /// <summary>
        /// Nom complet (propriete calculee)
        /// </summary>
        public string NomComplet => $"{Prenom} {Nom}".Trim();
    }

    /* =========================================================================
     * CLASSE : ENCOURS BANCAIRE (Credit existant)
     * =========================================================================
     * Represente un credit existant du client pouvant etre rachete.
     * ========================================================================= */
    public class EncoursBancaire
    {
        /// <summary>
        /// Identifiant unique de l'encours
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Reference ou numero du credit
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// Etablissement bancaire (si externe)
        /// </summary>
        public string? Banque { get; set; }

        /// <summary>
        /// Capital restant du (solde actuel)
        /// </summary>
        [Required(ErrorMessage = "Le solde est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "Le solde doit etre positif")]
        public decimal Solde { get; set; }

        /// <summary>
        /// Mensualite actuelle
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "La mensualite doit etre positive")]
        public decimal Mensualite { get; set; }

        /// <summary>
        /// Taux d'interet annuel du credit existant
        /// </summary>
        [Range(0, 100, ErrorMessage = "Le taux doit etre entre 0 et 100")]
        public decimal TauxInteret { get; set; }

        /// <summary>
        /// Date de fin prevue du credit
        /// </summary>
        public DateTime? DateEcheanceFinale { get; set; }

        /// <summary>
        /// Nombre d'echeances restantes
        /// </summary>
        [Range(0, 600, ErrorMessage = "Le nombre d'echeances doit etre entre 0 et 600")]
        public int EcheancesRestantes { get; set; }

        /// <summary>
        /// Statut de l'encours (Actif, A racheter, Rachete)
        /// </summary>
        public StatutEncours Statut { get; set; } = StatutEncours.Actif;

        /// <summary>
        /// Indique si ce credit doit etre rachete par le nouveau pret
        /// </summary>
        public bool ARacheter { get; set; } = false;

        /// <summary>
        /// Commission de rachat calculee (2% du solde)
        /// </summary>
        public decimal CommissionRachat { get; set; }

        /// <summary>
        /// Montant total du rachat (solde + commission)
        /// </summary>
        public decimal MontantTotalRachat => Solde + CommissionRachat;

        /// <summary>
        /// Description ou objet du credit existant
        /// </summary>
        public string? Description { get; set; }
    }

    /* =========================================================================
     * CLASSE : ALERTE CREDIT
     * =========================================================================
     * Represente une alerte de conformite ou d'information.
     * ========================================================================= */
    public class AlerteCredit
    {
        /// <summary>
        /// Code unique de l'alerte
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Type d'alerte (Information, Avertissement, Critique)
        /// </summary>
        public TypeAlerte Type { get; set; }

        /// <summary>
        /// Titre court de l'alerte
        /// </summary>
        public string Titre { get; set; } = string.Empty;

        /// <summary>
        /// Message detaille de l'alerte
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Valeur actuelle ayant declenche l'alerte
        /// </summary>
        public decimal? ValeurActuelle { get; set; }

        /// <summary>
        /// Seuil de declenchement
        /// </summary>
        public decimal? Seuil { get; set; }

        /// <summary>
        /// Indique si l'alerte est bloquante
        /// </summary>
        public bool EstBloquante { get; set; } = false;
    }

    /* =========================================================================
     * CLASSE : CONSTANTES REGLEMENTAIRES
     * =========================================================================
     * Contient les seuils et constantes reglementaires parametrables.
     * ========================================================================= */
    public static class ConstantesReglementaires
    {
        /// <summary>
        /// Plafond du TEG (14% depuis juin)
        /// </summary>
        public const decimal PLAFOND_TEG = 14.0m;

        /// <summary>
        /// Seuil de garantie hypothecaire obligatoire (25 millions)
        /// </summary>
        public const decimal SEUIL_GARANTIE_HYPOTHECAIRE = 25_000_000m;

        /// <summary>
        /// Quotite d'endettement max pour salaire >= 100 000 : 50%
        /// </summary>
        public const decimal TAUX_ENDETTEMENT_SALARIE_HAUT = 50.0m;

        /// <summary>
        /// Quotite d'endettement max pour salaire entre 75 001 et 99 999 : 40%
        /// </summary>
        public const decimal TAUX_ENDETTEMENT_SALARIE_MOYEN = 40.0m;

        /// <summary>
        /// Quotite d'endettement max pour salaire <= 75 000 OU retraites : 33%
        /// </summary>
        public const decimal TAUX_ENDETTEMENT_SALARIE_FAIBLE = 33.0m;

        /// <summary>
        /// Seuil salarial haut : >= 100 000 => quotite 50%
        /// </summary>
        public const decimal SEUIL_SALAIRE_HAUT = 100_000m;

        /// <summary>
        /// Seuil salarial moyen : > 75 000 et < 100 000 => quotite 40%
        /// Note: Si salaire = 75 000 exactement, quotite = 33%
        /// </summary>
        public const decimal SEUIL_SALAIRE_MOYEN = 75_000m;

        /// <summary>
        /// Commission de rachat par defaut (2%)
        /// </summary>
        public const decimal COMMISSION_RACHAT_DEFAUT = 2.0m;

        /// <summary>
        /// Frais de dossier par defaut (1.5%)
        /// </summary>
        public const decimal FRAIS_DOSSIER_DEFAUT = 1.5m;

        /// <summary>
        /// TAF par defaut (10%)
        /// </summary>
        public const decimal TAF_DEFAUT = 10.0m;

        /// <summary>
        /// Nombre maximum d'encours
        /// </summary>
        public const int MAX_ENCOURS = 6;
    }

    /* =========================================================================
     * CLASSE : LIGNE DU TABLEAU D'AMORTISSEMENT
     * =========================================================================
     * Represente une seule echeance dans le tableau d'amortissement.
     * Contient tous les montants detailles pour cette echeance.
     * ========================================================================= */
    public class LigneAmortissement
    {
        /// <summary>
        /// Numero de l'echeance (croissant de 1 vers le nombre total)
        /// </summary>
        public int NumeroEcheance { get; set; }

        /// <summary>
        /// Index de la ligne (alias pour compatibilite JavaScript)
        /// </summary>
        public int IndexLigne => NumeroEcheance;

        /// <summary>
        /// Date de l'echeance
        /// </summary>
        public DateTime DateEcheance { get; set; }

        /// <summary>
        /// Capital restant du au debut de cette echeance
        /// </summary>
        public decimal CapitalRestantDebut { get; set; }

        /// <summary>
        /// Montant des interets pour cette echeance
        /// </summary>
        public decimal Interets { get; set; }

        /// <summary>
        /// Montant de la TAF (Taxe sur Activites Financieres)
        /// Calculee comme pourcentage des interets
        /// </summary>
        public decimal TAF { get; set; }

        /// <summary>
        /// Part du capital rembourse dans cette echeance
        /// </summary>
        public decimal CapitalRembourse { get; set; }

        /// <summary>
        /// Montant de l'assurance pour cette echeance
        /// </summary>
        public decimal Assurance { get; set; }

        /// <summary>
        /// Montant total de l'echeance (Capital + Interets + TAF)
        /// Note: L'assurance peut etre incluse ou separee selon les conventions
        /// </summary>
        public decimal Mensualite { get; set; }

        /// <summary>
        /// Mensualite totale incluant l'assurance
        /// </summary>
        public decimal MensualiteAvecAssurance { get; set; }

        /// <summary>
        /// Capital restant du apres paiement de cette echeance
        /// </summary>
        public decimal CapitalRestantFin { get; set; }

        /// <summary>
        /// Indique si cette echeance est en periode de differe
        /// </summary>
        public bool EstEnDiffere { get; set; }

        /// <summary>
        /// Indique si cette ligne est un remboursement anticipe
        /// </summary>
        public bool EstRemboursementAnticipe { get; set; }

        /// <summary>
        /// Penalite de remboursement anticipe (IRA)
        /// </summary>
        public decimal PenaliteRA { get; set; }
    }

    /* =========================================================================
     * CLASSE : RESULTAT COMPLET DU CALCUL
     * =========================================================================
     * Contient le resume du pret et toutes les lignes du tableau.
     * Cette classe est serialisee en JSON pour l'affichage cote client.
     * ========================================================================= */
    public class AmortissementResult
    {
        // =====================================================================
        // RESUME DU PRET
        // =====================================================================

        /// <summary>
        /// Montant initial du pret
        /// </summary>
        public decimal MontantPret { get; set; }

        /// <summary>
        /// Taux d'interet annuel HT applique
        /// </summary>
        public decimal TauxAnnuel { get; set; }

        /// <summary>
        /// Taux TAF (TPS) en pourcentage
        /// </summary>
        public decimal TauxTAF { get; set; }

        /// <summary>
        /// Taux TTC calcule = TauxAnnuel * (1 + TauxTAF/100)
        /// </summary>
        public decimal TauxTTC { get; set; }

        /// <summary>
        /// Nombre total d'echeances
        /// </summary>
        public int NombreEcheances { get; set; }

        /// <summary>
        /// Mensualite calculee (hors assurance)
        /// </summary>
        public decimal Mensualite { get; set; }

        /// <summary>
        /// Mensualite avec assurance incluse
        /// </summary>
        public decimal MensualiteAvecAssurance { get; set; }

        /// <summary>
        /// Total des interets payes sur la duree du pret
        /// </summary>
        public decimal TotalInterets { get; set; }

        /// <summary>
        /// Total de la TAF payee
        /// </summary>
        public decimal TotalTAF { get; set; }

        /// <summary>
        /// Total de l'assurance payee
        /// </summary>
        public decimal TotalAssurance { get; set; }

        /// <summary>
        /// Total des mensualites (somme de toutes les echeances)
        /// </summary>
        public decimal TotalMensualites { get; set; }

        /// <summary>
        /// Total du capital rembourse (somme de toutes les lignes)
        /// </summary>
        public decimal TotalCapitalRembourse { get; set; }

        /// <summary>
        /// Cout total du credit (interets + TAF + assurance + frais + penalites)
        /// </summary>
        public decimal CoutTotalCredit { get; set; }

        /// <summary>
        /// Frais de dossier
        /// </summary>
        public decimal FraisDossier { get; set; }

        /// <summary>
        /// Penalite de remboursement anticipe (IRA)
        /// </summary>
        public decimal PenaliteRA { get; set; }

        /// <summary>
        /// Indique si un remboursement anticipe a ete effectue
        /// </summary>
        public bool ARemboursementAnticipe { get; set; }

        /// <summary>
        /// TEG saisi par l'utilisateur
        /// </summary>
        public decimal TEG { get; set; }

        /// <summary>
        /// TEG calcule automatiquement (TRI des flux)
        /// </summary>
        public decimal TEGCalcule { get; set; }

        /// <summary>
        /// Date de deblocage des fonds
        /// </summary>
        public DateTime? DateDeblocage { get; set; }

        /// <summary>
        /// Date de la premiere echeance
        /// </summary>
        public DateTime DateDebut { get; set; }

        /// <summary>
        /// Date de la derniere echeance
        /// </summary>
        public DateTime DateFin { get; set; }

        /// <summary>
        /// Periodicite des echeances
        /// </summary>
        public string PeriodiciteLibelle { get; set; } = string.Empty;

        /// <summary>
        /// Mode de remboursement utilise
        /// </summary>
        public string ModeRemboursementLibelle { get; set; } = string.Empty;

        /// <summary>
        /// Nombre d'echeances en differe
        /// </summary>
        public int DiffereEcheances { get; set; }

        /// <summary>
        /// Nom du client
        /// </summary>
        public string? NomClient { get; set; }

        // =====================================================================
        // INFORMATIONS CLIENT
        // =====================================================================

        /// <summary>
        /// Informations completes du client
        /// </summary>
        public ClientInfo? Client { get; set; }

        // =====================================================================
        // GESTION DES ENCOURS ET RACHATS
        // =====================================================================

        /// <summary>
        /// Total des encours rachetes
        /// </summary>
        public decimal TotalEncoursRachetes { get; set; }

        /// <summary>
        /// Total des commissions de rachat
        /// </summary>
        public decimal TotalCommissionsRachat { get; set; }

        /// <summary>
        /// Montant net mis a disposition (apres deduction rachats et frais)
        /// </summary>
        public decimal MontantNetDisponible { get; set; }

        /// <summary>
        /// Liste des encours rachetes
        /// </summary>
        public List<EncoursBancaire> EncoursRachetes { get; set; } = new List<EncoursBancaire>();

        /// <summary>
        /// Liste des encours non rachetes (restant actifs)
        /// </summary>
        public List<EncoursBancaire> EncoursActifs { get; set; } = new List<EncoursBancaire>();

        /// <summary>
        /// Total des mensualites des encours actifs (non rachetes)
        /// </summary>
        public decimal TotalMensualitesEncoursActifs { get; set; }

        // =====================================================================
        // CAPACITE D'ENDETTEMENT
        // =====================================================================

        /// <summary>
        /// Salaire mensuel du client
        /// </summary>
        public decimal SalaireMensuel { get; set; }

        /// <summary>
        /// Taux d'endettement maximum autorise selon le profil
        /// </summary>
        public decimal TauxEndettementMax { get; set; }

        /// <summary>
        /// Capacite de remboursement maximale (salaire * taux endettement)
        /// </summary>
        public decimal CapaciteRemboursementMax { get; set; }

        /// <summary>
        /// Total des charges mensuelles (nouvelle echeance + encours actifs + autres charges)
        /// </summary>
        public decimal TotalChargesMensuelles { get; set; }

        /// <summary>
        /// Taux d'endettement actuel calcule
        /// </summary>
        public decimal TauxEndettementActuel { get; set; }

        /// <summary>
        /// Reste a vivre apres paiement de toutes les charges
        /// </summary>
        public decimal ResteAVivre { get; set; }

        // =====================================================================
        // TOTAL ENCOURS GLOBAL
        // =====================================================================

        /// <summary>
        /// Total de tous les encours (nouveau pret + encours actifs)
        /// </summary>
        public decimal TotalEncoursGlobal { get; set; }

        /// <summary>
        /// Indique si une garantie hypothecaire est requise (> 25M)
        /// </summary>
        public bool GarantieHypothecaireRequise { get; set; }

        // =====================================================================
        // ALERTES DE CONFORMITE
        // =====================================================================

        /// <summary>
        /// Liste des alertes generees lors du calcul
        /// </summary>
        public List<AlerteCredit> Alertes { get; set; } = new List<AlerteCredit>();

        /// <summary>
        /// Indique si le dossier comporte des alertes bloquantes
        /// </summary>
        public bool AAlerteBloquante => Alertes.Any(a => a.EstBloquante);

        /// <summary>
        /// Nombre total d'alertes
        /// </summary>
        public int NombreAlertes => Alertes.Count;

        /// <summary>
        /// Nombre d'alertes critiques
        /// </summary>
        public int NombreAlertesCritiques => Alertes.Count(a => a.Type == TypeAlerte.Critique);

        // =====================================================================
        // LIGNES DETAILLEES
        // =====================================================================

        /// <summary>
        /// Liste de toutes les lignes du tableau d'amortissement
        /// </summary>
        public List<LigneAmortissement> Lignes { get; set; } = new List<LigneAmortissement>();
    }
}
