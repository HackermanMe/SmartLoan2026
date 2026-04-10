/*
 * =============================================================================
 * JAVASCRIPT/JQUERY - MODULE PRET BANCAIRE COMPLET
 * =============================================================================
 * Ce fichier gere toutes les interactions utilisateur et la communication
 * avec le serveur pour le module de pret bancaire complet.
 *
 * Fonctionnalites:
 * - Navigation par onglets
 * - Gestion des informations client
 * - Gestion des encours (jusqu'a 6)
 * - Calcul de la capacite d'endettement
 * - Affichage des alertes de conformite
 * - Generation du tableau d'amortissement
 * - Export vers Excel, PDF, Word
 * - Generation de documents contractuels
 *
 * Dependencies: jQuery
 * =============================================================================
 */

// =============================================================================
// VARIABLES GLOBALES
// =============================================================================
var encoursData = []; // Tableau des encours
var dernierResultat = null; // Dernier resultat de calcul

// =============================================================================
// INITIALISATION AU CHARGEMENT DU DOCUMENT
// =============================================================================
$(document).ready(function () {
    initDateDefaut();
    initTabs();
    initFormSubmit();
    initExportButtons();
    initResetButton();
    initTauxTTCSync();
    initAssuranceSync();
    initCapaciteEndettement();
    initEncours();
    initFraisDossier();
    initDocumentButtons();
});

// =============================================================================
// NAVIGATION PAR ONGLETS
// =============================================================================
function initTabs() {
    $('.tab-btn').on('click', function () {
        var tabId = $(this).data('tab');

        // Activer l'onglet clique
        $('.tab-btn').removeClass('active');
        $(this).addClass('active');

        // Afficher le contenu correspondant
        $('.tab-content').removeClass('active');
        $('#' + tabId).addClass('active');
    });
}

// =============================================================================
// INITIALISATION DE LA DATE PAR DEFAUT
// =============================================================================
function initDateDefaut() {
    var today = new Date();
    var nextMonth = new Date(today.getFullYear(), today.getMonth() + 1, 1);
    var formattedDate = nextMonth.toISOString().split('T')[0];
    $('#dateDebut').val(formattedDate);

    // Date de deblocage = aujourd'hui
    $('#dateDeblocage').val(today.toISOString().split('T')[0]);
}

// =============================================================================
// FRAIS DE DOSSIER AUTO-CALCUL
// =============================================================================
function initFraisDossier() {
    $('#btnCalcFrais').on('click', function () {
        var montant = parseFloat($('#montantPret').val()) || 0;
        var frais = Math.round(montant * 0.015); // 1.5%
        $('#fraisDossier').val(frais);
    });

    // Auto-calcul quand le montant change
    $('#montantPret').on('change', function () {
        var montant = parseFloat($(this).val()) || 0;
        var frais = Math.round(montant * 0.015);
        $('#fraisDossier').val(frais);
    });
}

// =============================================================================
// CAPACITE D'ENDETTEMENT
// =============================================================================
function initCapaciteEndettement() {
    // Calculer la capacite quand les champs changent
    $('#salaireMensuel, #profilClient').on('change input', function () {
        calculerCapaciteAffichage();
    });
}

function calculerCapaciteAffichage() {
    var salaire = parseFloat($('#salaireMensuel').val()) || 0;
    var profil = parseInt($('#profilClient').val()) || 1;

    if (salaire <= 0) {
        $('#capaciteMaxDisplay').text('-');
        $('#tauxEndettementMaxDisplay').text('(-)');
        return;
    }

    // Determiner le taux max selon le profil et le salaire
    var tauxMax;
    if (profil === 4) { // Retraite
        tauxMax = 33;
    } else if (salaire >= 100000) {
        tauxMax = 50;
    } else if (salaire >= 75000) {
        tauxMax = 40;
    } else {
        tauxMax = 33;
    }

    var capaciteMax = salaire * tauxMax / 100;

    $('#capaciteMaxDisplay').text(formatMontant(capaciteMax));
    $('#tauxEndettementMaxDisplay').text('(' + tauxMax + '%)');
}

// =============================================================================
// GESTION DES ENCOURS
// =============================================================================
function initEncours() {
    // Bouton ajouter encours
    $('#btnAjouterEncours').on('click', function () {
        if (encoursData.length >= 6) {
            alert('Maximum 6 encours autorises');
            return;
        }
        ajouterEncours();
    });

    // Delegation pour suppression et mise a jour
    $('#encoursList').on('click', '.btn-supprimer-encours', function () {
        var $card = $(this).closest('.encours-card');
        var index = $card.data('index');
        supprimerEncours(index);
    });

    $('#encoursList').on('change input', '.encours-solde, .encours-mensualite, .encours-racheter', function () {
        mettreAJourResumeEncours();
    });
}

function ajouterEncours() {
    var index = encoursData.length;

    // Cloner le template
    var $template = $('#encoursTemplate .encours-card').clone();
    $template.attr('data-index', index);
    $template.find('.encours-numero').text('Encours #' + (index + 1));

    // Ajouter au DOM
    $('#encoursList').append($template);

    // Ajouter aux donnees
    encoursData.push({
        id: index,
        banque: '',
        solde: 0,
        mensualite: 0,
        echeancesRestantes: 0,
        aRacheter: false
    });

    // Mettre a jour l'affichage
    mettreAJourBadgeEncours();
    $('#encoursResume').show();
    mettreAJourResumeEncours();
}

function supprimerEncours(index) {
    // Supprimer du DOM
    $('#encoursList .encours-card[data-index="' + index + '"]').remove();

    // Supprimer des donnees
    encoursData.splice(index, 1);

    // Reindicer les cartes restantes
    $('#encoursList .encours-card').each(function (i) {
        $(this).attr('data-index', i);
        $(this).find('.encours-numero').text('Encours #' + (i + 1));
    });

    // Mettre a jour l'affichage
    mettreAJourBadgeEncours();
    if (encoursData.length === 0) {
        $('#encoursResume').hide();
    }
    mettreAJourResumeEncours();
}

function mettreAJourBadgeEncours() {
    $('#encoursCount').text(encoursData.length);
}

function mettreAJourResumeEncours() {
    var totalSolde = 0;
    var totalMensualites = 0;
    var totalARacheter = 0;
    var tauxCommission = parseFloat($('#tauxCommissionRachat').val()) || 2;

    $('#encoursList .encours-card').each(function (i) {
        var solde = parseFloat($(this).find('.encours-solde').val()) || 0;
        var mensualite = parseFloat($(this).find('.encours-mensualite').val()) || 0;
        var aRacheter = $(this).find('.encours-racheter').is(':checked');

        if (aRacheter) {
            totalARacheter += solde;
        } else {
            totalSolde += solde;
            totalMensualites += mensualite;
        }
    });

    var commission = totalARacheter * tauxCommission / 100;
    var montantPret = parseFloat($('#montantPret').val()) || 0;
    var fraisDossier = parseFloat($('#fraisDossier').val()) || 0;
    var montantNet = montantPret - totalARacheter - commission - fraisDossier;

    $('#totalEncoursActifs').text(formatMontant(totalSolde));
    $('#totalMensualitesEncours').text(formatMontant(totalMensualites));
    $('#totalEncoursARacheter').text(formatMontant(totalARacheter));
    $('#totalCommissionRachat').text(formatMontant(commission));
    $('#montantNetDisponible').text(formatMontant(montantNet));
}

function collecterEncours() {
    var encours = [];

    $('#encoursList .encours-card').each(function () {
        encours.push({
            id: parseInt($(this).attr('data-index')),
            banque: $(this).find('.encours-banque').val() || '',
            solde: parseFloat($(this).find('.encours-solde').val()) || 0,
            mensualite: parseFloat($(this).find('.encours-mensualite').val()) || 0,
            echeancesRestantes: parseInt($(this).find('.encours-echeances').val()) || 0,
            aRacheter: $(this).find('.encours-racheter').is(':checked')
        });
    });

    return encours;
}

// =============================================================================
// SOUMISSION DU FORMULAIRE
// =============================================================================
function initFormSubmit() {
    $('#amortissementForm').on('submit', function (e) {
        e.preventDefault();

        if (!validerFormulaire()) {
            return;
        }

        var formData = collecterDonneesFormulaire();

        afficherLoader(true);
        masquerErreur();
        masquerAlertes();

        $.ajax({
            url: '/Amortissement/Calculer',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
             afficherLoader(false);
                //console pou verifiaction des donnees
                console.log("RESPONSE COMPLETE =", response);
                console.log("DATA =", response.data);
               

                if (response.success) {
                    dernierResultat = response.data;
                    afficherResultats(response.data);
                } else {
                    afficherErreur(response.errors.join('<br>'));
                }
            },
            error: function (xhr, status, error) {
                afficherLoader(false);
                afficherErreur('Une erreur est survenue lors de la communication avec le serveur.');
            }
        });
    });
}

// =============================================================================
// COLLECTE DES DONNEES DU FORMULAIRE
// =============================================================================
function collecterDonneesFormulaire() {
    // Informations client
    var client = null;
    var numeroCompte = $('#numeroCompte').val();
    if (numeroCompte) {
        client = {
            numeroCompte: numeroCompte,
            intituleCompte: $('#intituleCompte').val() || null,
            nom: $('#clientNom').val() || '',
            prenom: $('#clientPrenom').val() || '',
            dateNaissance: $('#clientDateNaissance').val() || null,
            lieuNaissance: $('#clientLieuNaissance').val() || null,
            adresse: $('#clientAdresse').val() || null,
            ville: $('#clientVille').val() || null,
            telephone: $('#clientTelephone').val() || null,
            situationMatrimoniale: parseInt($('#situationMatrimoniale').val()) || 1,
            profession: $('#clientProfession').val() || null,
            employeur: $('#clientEmployeur').val() || null,
            nationalite: $('#clientNationalite').val() || null,  // ← AJOUTER                                                          
            dateEmbauche: $('#clientDateEmbauche').val() || null,  // ← AJOUTER 
            typePieceIdentite: parseInt($('#typePieceIdentite').val()) || 1,
            numeroPieceIdentite: $('#numeroPieceIdentite').val() || null,
            dateDelivrancePiece: $('#dateDelivrancePiece').val() || null,
            lieuDelivrancePiece: $('#lieuDelivrancePiece').val() || null,
            dateExpirationPiece: $('#dateExpirationPiece').val() || null
        };
    }

    return {
        nomClient: $('#clientNom').val() && $('#clientPrenom').val()
            ? $('#clientPrenom').val() + ' ' + $('#clientNom').val()
            : null,
        montantPret: parseFloat($('#montantPret').val()) || 0,
        tauxAnnuel: parseFloat($('#tauxAnnuel').val()) || 0,
        nombreEcheances: parseInt($('#nombreEcheances').val()) || 0,
        dateDebut: $('#dateDebut').val(),
        dateDeblocage: $('#dateDeblocage').val() || null,
        fraisDossier: parseFloat($('#fraisDossier').val()) || 0,
        periodicite: parseInt($('#periodicite').val()) || 1,
        modeRemboursement: parseInt($('#modeRemboursement').val()) || 1,
        primeAssuranceClient: parseFloat($('#primeAssuranceClient').val()) || 0,
        commissionETG: parseFloat($('#commissionETG').val()) || 0,
        assuranceFixe: parseFloat($('#assuranceFixe').val()) || 0,
        teg: parseFloat($('#teg').val()) || 0,
        tauxTAF: parseFloat($('#tauxTAF').val()) || 10,
        differeEcheances: parseInt($('#differeEcheances').val()) || 0,
        typeDiffere: parseInt($('#typeDiffere').val()) || 1,
        conventionJours: parseInt($('#conventionJours').val()) || 1,
        typeTaux: parseInt($('#typeTaux').val()) || 1,
        baseAssurance: parseInt($('#baseAssurance').val()) || 1,
        echeanceRemboursementAnticipe: parseInt($('#echeanceRA').val()) || 0,
        montantRemboursementAnticipe: parseFloat($('#montantRA').val()) || 0,
        tauxPenaliteRA: parseFloat($('#tauxPenaliteRA').val()) || 3,
        // Nouvelles proprietes
        client: client,
        encours: collecterEncours(),
        tauxCommissionRachat: parseFloat($('#tauxCommissionRachat').val()) || 2,
        salaireMensuel: parseFloat($('#salaireMensuel').val()) || 0,
        profilClient: parseInt($('#profilClient').val()) || 1,
        autresCharges: parseFloat($('#autresCharges').val()) || 0,
        objetCredit: $('#objetCredit').val() || null
    };
}

// =============================================================================
// VALIDATION COTE CLIENT
// =============================================================================
function validerFormulaire() {
    var isValid = true;
    var errors = [];

    var montant = parseFloat($('#montantPret').val());
    if (!montant || montant <= 0) {
        errors.push('Le montant du pret doit etre positif');
        isValid = false;
    }

    var taux = parseFloat($('#tauxAnnuel').val());
    if (isNaN(taux) || taux < 0 || taux > 100) {
        errors.push('Le taux doit etre entre 0 et 100%');
        isValid = false;
    }

    var echeances = parseInt($('#nombreEcheances').val());
    if (!echeances || echeances < 1) {
        errors.push('Le nombre d\'echeances doit etre au moins 1');
        isValid = false;
    }

    if (!$('#dateDebut').val()) {
        errors.push('La date de debut est obligatoire');
        isValid = false;
    }

    var differe = parseInt($('#differeEcheances').val()) || 0;
    if (differe >= echeances) {
        errors.push('Le differe doit etre inferieur au nombre d\'echeances');
        isValid = false;
    }

    if (!isValid) {
        afficherErreur(errors.join('<br>'));
    }

    return isValid;
}

// =============================================================================
// AFFICHAGE DES RESULTATS
// =============================================================================
function afficherResultats(data) {
    $('#resultsSection').show();

    // Afficher les alertes
    if (data.alertes && data.alertes.length > 0) {
        afficherAlertes(data.alertes);
    }

    // Mise a jour du resume
    afficherResume(data);

    // Generation du tableau
    //genererTableauProgressif(data.lignes, data);
    //genererTableauProgressif(response.data.Lignes, response.data);
    genererTableauProgressif(data.Lignes || data.lignes || [], data);



    // Scroll vers les resultats
    $('html, body').animate({
        scrollTop: $('#resultsSection').offset().top - 20
    }, 500);
}

// =============================================================================
// AFFICHAGE DES ALERTES
// =============================================================================
function afficherAlertes(alertes) {
    var $container = $('#alertesContainer');
    var $list = $('#alertesList');

    $list.empty();

    var nbCritiques = 0;
    var nbAvertissements = 0;

    alertes.forEach(function (alerte) {
        var typeClass = '';
        var icon = '';

        switch (alerte.type) {
            case 2: // Critique
                typeClass = 'alerte-critique';
                icon = '&#9888;';
                nbCritiques++;
                break;
            case 1: // Avertissement
                typeClass = 'alerte-avertissement';
                icon = '&#9888;';
                nbAvertissements++;
                break;
            default: // Information
                typeClass = 'alerte-info';
                icon = '&#8505;';
                break;
        }

        var $alerte = $('<div>').addClass('alerte-item ' + typeClass);
        $alerte.html(
            '<span class="alerte-icon">' + icon + '</span>' +
            '<div class="alerte-content">' +
                '<strong>' + alerte.titre + '</strong>' +
                '<p>' + alerte.message + '</p>' +
            '</div>'
        );

        $list.append($alerte);
    });

    // Mettre a jour le compteur
    var countText = alertes.length + ' alerte(s)';
    if (nbCritiques > 0) {
        countText = nbCritiques + ' critique(s)';
    }
    $('#alertesCount').text(countText);

    $container.show();
}

function masquerAlertes() {
    $('#alertesContainer').hide();
    $('#alertesList').empty();
}

// =============================================================================
// AFFICHAGE DU RESUME
// =============================================================================
//function afficherResume(data) {
//    // Mensualite principale
//    var $mensualite = $('#mensualiteDisplay');
//    $mensualite.fadeOut(100, function () {
//        $(this).text(formatMontant(data.mensualiteAvecAssurance)).fadeIn(300);
//    });

//    // Grille du resume
//    $('#resumeMontant').text(formatMontant(data.montantPret));
//    $('#resumeTaux').text(data.tauxAnnuel.toFixed(2) + '%');
//    $('#resumeEcheances').text(data.nombreEcheances);
//    $('#resumePeriodicite').text(data.periodiciteLibelle);
//    $('#resumeMensualite').text(formatMontant(data.mensualite));

//    var assuranceEch = data.mensualiteAvecAssurance - data.mensualite;
//    $('#resumeAssuranceEch').text(formatMontant(assuranceEch));
//    $('#resumeMensualiteAssurance').text(formatMontant(data.mensualiteAvecAssurance));

//    $('#resumeTotalInterets').text(formatMontant(data.totalInterets));
//    $('#resumeTotalTAF').text(formatMontant(data.totalTAF));
//    $('#resumeTotalAssurance').text(formatMontant(data.totalAssurance));
//    $('#resumeFraisDossier').text(formatMontant(data.fraisDossier));

//    // Encours rachetes
//    if (data.totalEncoursRachetes > 0) {
//        $('#resumeEncoursRachetes').text(formatMontant(data.totalEncoursRachetes));
//        $('#resumeRachatContainer').show();

//        $('#resumeCommissionRachat').text(formatMontant(data.totalCommissionsRachat));
//        $('#resumeCommissionContainer').show();

//        $('#resumeMontantNet').text(formatMontant(data.montantNetDisponible));
//        $('#resumeNetContainer').show();
//    } else {
//        $('#resumeRachatContainer, #resumeCommissionContainer, #resumeNetContainer').hide();
//    }

//    $('#resumeCoutTotal').text(formatMontant(data.coutTotalCredit));

//    // TEG avec coloration
//    var tegText = data.tegCalcule.toFixed(2) + '%';
//    var $teg = $('#resumeTEGCalcule');
//    $teg.text(tegText);
//    if (data.tegCalcule > 14) {
//        $teg.addClass('teg-depasse');
//    } else if (data.tegCalcule > 13) {
//        $teg.addClass('teg-proche');
//    } else {
//        $teg.removeClass('teg-depasse teg-proche');
//    }

//    // Penalite RA
//    if (data.aRemboursementAnticipe && data.penaliteRA > 0) {
//        $('#resumePenaliteRA').text(formatMontant(data.penaliteRA));
//        $('#resumePenaliteRAContainer').show();
//    } else {
//        $('#resumePenaliteRAContainer').hide();
//    }

//    // Analyse d'endettement
//    if (data.salaireMensuel > 0) {
//        $('#resumeSalaire').text(formatMontant(data.salaireMensuel));
//        $('#resumeTauxMax').text(data.tauxEndettementMax.toFixed(0) + '%');
//        $('#resumeCapaciteMax').text(formatMontant(data.capaciteRemboursementMax));
//        $('#resumeTotalCharges').text(formatMontant(data.totalChargesMensuelles));

//        var $tauxEndettement = $('#resumeTauxEndettement');
//        $tauxEndettement.text(data.tauxEndettementActuel.toFixed(1) + '%');
//        if (data.tauxEndettementActuel > data.tauxEndettementMax) {
//            $tauxEndettement.addClass('endettement-depasse');
//        } else {
//            $tauxEndettement.removeClass('endettement-depasse');
//        }

//        var $resteAVivre = $('#resumeResteAVivre');
//        $resteAVivre.text(formatMontant(data.resteAVivre));
//        if (data.resteAVivre < 0) {
//            $resteAVivre.addClass('reste-negatif');
//        } else {
//            $resteAVivre.removeClass('reste-negatif');
//        }

//        $('#endettementResume').show();
//    } else {
//        $('#endettementResume').hide();
//    }

//    // Info client dans le tableau
//    if (data.nomClient) {
//        $('#tableauClientInfo').text('Client: ' + data.nomClient);
//    } else {
//        $('#tableauClientInfo').text('');
//    }
//}

function afficherResume(data) {

    // S�curisation des champs num�riques
    var montantPret = safeNumber(data.montantPret ?? data.MontantPret);
    var tauxAnnuel = safeNumber(data.tauxAnnuel ?? data.TauxAnnuel);
    var mensualite = safeNumber(data.mensualite ?? data.Mensualite);
    var mensualiteAvecAssurance = safeNumber(data.mensualiteAvecAssurance ?? data.MensualiteAvecAssurance);

    var totalInterets = safeNumber(data.totalInterets ?? data.TotalInterets);
    var totalTAF = safeNumber(data.totalTAF ?? data.TotalTAF);
    var totalAssurance = safeNumber(data.totalAssurance ?? data.TotalAssurance);
    var fraisDossier = safeNumber(data.fraisDossier ?? data.FraisDossier);

    var coutTotalCredit = safeNumber(data.coutTotalCredit ?? data.CoutTotalCredit);

    var tegCalcule = safeNumber(data.TEGCalcule ?? data.TEGCalcule);

    var totalEncoursRachetes = safeNumber(data.totalEncoursRachetes ?? data.TotalEncoursRachetes);
    var totalCommissionsRachat = safeNumber(data.totalCommissionsRachat ?? data.TotalCommissionsRachat);
    var montantNetDisponible = safeNumber(data.montantNetDisponible ?? data.MontantNetDisponible);

    var penaliteRA = safeNumber(data.penaliteRA ?? data.PenaliteRA);

    var salaireMensuel = safeNumber(data.salaireMensuel ?? data.SalaireMensuel);
    var tauxEndettementMax = safeNumber(data.tauxEndettementMax ?? data.TauxEndettementMax);
    var capaciteRemboursementMax = safeNumber(data.capaciteRemboursementMax ?? data.CapaciteRemboursementMax);
    var totalChargesMensuelles = safeNumber(data.totalChargesMensuelles ?? data.TotalChargesMensuelles);
    var tauxEndettementActuel = safeNumber(data.tauxEndettementActuel ?? data.TauxEndettementActuel);
    var resteAVivre = safeNumber(data.resteAVivre ?? data.ResteAVivre);

    // Mensualit� principale
    var $mensualite = $('#mensualiteDisplay');
    $mensualite.fadeOut(100, function () {
        $(this).text(formatMontant(mensualiteAvecAssurance)).fadeIn(300);
    });

    // Grille du r�sum�
    $('#resumeMontant').text(formatMontant(montantPret));
    $('#resumeTaux').text(tauxAnnuel.toFixed(2) + '%');
    $('#resumeEcheances').text(data.nombreEcheances ?? data.NombreEcheances ?? 0);
    $('#resumePeriodicite').text(data.periodiciteLibelle ?? data.PeriodiciteLibelle ?? '-');
    $('#resumeMensualite').text(formatMontant(mensualite));

    var assuranceEch = mensualiteAvecAssurance - mensualite;
    $('#resumeAssuranceEch').text(formatMontant(assuranceEch));
    $('#resumeMensualiteAssurance').text(formatMontant(mensualiteAvecAssurance));

    $('#resumeTotalInterets').text(formatMontant(totalInterets));
    $('#resumeTotalTAF').text(formatMontant(totalTAF));
    $('#resumeTotalAssurance').text(formatMontant(totalAssurance));
    $('#resumeFraisDossier').text(formatMontant(fraisDossier));

    // Encours rachet�s
    if (totalEncoursRachetes > 0) {
        $('#resumeEncoursRachetes').text(formatMontant(totalEncoursRachetes));
        $('#resumeRachatContainer').show();

        $('#resumeCommissionRachat').text(formatMontant(totalCommissionsRachat));
        $('#resumeCommissionContainer').show();

        $('#resumeMontantNet').text(formatMontant(montantNetDisponible));
        $('#resumeNetContainer').show();
    } else {
        $('#resumeRachatContainer, #resumeCommissionContainer, #resumeNetContainer').hide();
    }

    $('#resumeCoutTotal').text(formatMontant(coutTotalCredit));

    // TEG avec coloration
    var tegText = tegCalcule.toFixed(2) + '%';
    var $teg = $('#resumeTEGCalcule');
    $teg.text(tegText);

    $teg.removeClass('teg-depasse teg-proche');
    if (tegCalcule > 14) {
        $teg.addClass('teg-depasse');
    } else if (tegCalcule > 13) {
        $teg.addClass('teg-proche');
    }

    // P�nalit� RA
    if ((data.aRemboursementAnticipe ?? data.ARemboursementAnticipe) && penaliteRA > 0) {
        $('#resumePenaliteRA').text(formatMontant(penaliteRA));
        $('#resumePenaliteRAContainer').show();
    } else {
        $('#resumePenaliteRAContainer').hide();
    }

    // Analyse d'endettement
    if (salaireMensuel > 0) {
        $('#resumeSalaire').text(formatMontant(salaireMensuel));
        $('#resumeTauxMax').text(tauxEndettementMax.toFixed(0) + '%');
        $('#resumeCapaciteMax').text(formatMontant(capaciteRemboursementMax));
        $('#resumeTotalCharges').text(formatMontant(totalChargesMensuelles));

        var $tauxEndettement = $('#resumeTauxEndettement');
        $tauxEndettement.text(tauxEndettementActuel.toFixed(1) + '%');

        if (tauxEndettementActuel > tauxEndettementMax) {
            $tauxEndettement.addClass('endettement-depasse');
        } else {
            $tauxEndettement.removeClass('endettement-depasse');
        }

        var $resteAVivre = $('#resumeResteAVivre');
        $resteAVivre.text(formatMontant(resteAVivre));

        if (resteAVivre < 0) {
            $resteAVivre.addClass('reste-negatif');
        } else {
            $resteAVivre.removeClass('reste-negatif');
        }

        $('#endettementResume').show();
    } else {
        $('#endettementResume').hide();
    }

    // Info client
    var nomClient = data.nomClient ?? data.NomClient;
    if (nomClient) {
        $('#tableauClientInfo').text('Client: ' + nomClient);
    } else {
        $('#tableauClientInfo').text('');
    }
}


// =============================================================================
// GENERATION DU TABLEAU
// =============================================================================
function genererTableauProgressif(lignes, data) {

    // S�curiser data
    data = data || {};

    // Si lignes est vide ou undefined, essayer de le retrouver dans data
    if (!lignes) {
        lignes = data.lignes || data.Lignes || data.tableau || data.Tableau || data.echeances || data.Echeances;
    }

    // V�rification finale
    if (!lignes || !Array.isArray(lignes)) {
        console.error("Erreur : lignes introuvable ou invalide", lignes, data);
        afficherErreur("Impossible d'afficher le tableau : aucune ligne d'amortissement retourn�e.");
        return;
    }

    var $tbody = $('#tableauBody');
    var $tfoot = $('#tableauFooter');

    $tbody.empty();
    $tfoot.empty();

    // Si tableau vide
    if (lignes.length === 0) {
        afficherErreur("Aucune �ch�ance trouv�e pour ce calcul.");
        return;
    }

    var batchSize = lignes.length > 100 ? 10 : 5;
    var delai = lignes.length > 100 ? 10 : 30;
    var index = 0;

    // S�curiser les totaux
    var totalCapitalRembourse = parseFloat(data.totalCapitalRembourse || data.TotalCapitalRembourse || 0);
    var totalInterets = parseFloat(data.totalInterets || data.TotalInterets || 0);
    var totalTAF = parseFloat(data.totalTAF || data.TotalTAF || 0);
    var totalMensualites = parseFloat(data.totalMensualites || data.TotalMensualites || 0);

    function ajouterLot() {

        if (index >= lignes.length) {

            // Ajouter la ligne des totaux
            var $totalRow = $('<tr>').addClass('total-row');
            $totalRow.append($('<td>').text('TOTAL'));
            $totalRow.append($('<td>').text('-')); // Date
            $totalRow.append($('<td>').text('-')); // Balance Debut
            $totalRow.append($('<td>').text('-')); // Balance Fin
            $totalRow.append($('<td>').text(formatMontant(totalCapitalRembourse))); // Principal
            $totalRow.append($('<td>').text(formatMontant(totalInterets + totalTAF))); // Interet TTC
            $totalRow.append($('<td>').text(formatMontant(totalInterets))); // Interet HT
            $totalRow.append($('<td>').text(formatMontant(totalTAF))); // TPS
            $totalRow.append($('<td>').text(formatMontant(totalMensualites))); // Echeance

            $tfoot.append($totalRow);
            return;
        }

        var finLot = Math.min(index + batchSize, lignes.length);

        for (var i = index; i < finLot; i++) {

            var ligne = lignes[i];

            if (!ligne) continue;
            console.log("LIGNE ENVOYEE POUR CREATION TABLEAU =", ligne);
            var $row = creerLigneTableau(ligne);
            if ($row) {
                $row.hide().appendTo($tbody).fadeIn(150);
            }
        }

        index = finLot;
        setTimeout(ajouterLot, delai);
    }

    ajouterLot();
}


//function genererTableauProgressif(lignes, data) {
//    //Ajout d'une condition pour le controle du tableau

//    if (!lignes || !Array.isArray(lignes)) {
//        afficherErreur("Erreur : le serveur n'a pas retourn� les lignes du tableau d'amortissement.");
//        return;
//    }

//    var $tbody = $('#tableauBody');
//    var $tfoot = $('#tableauFooter');

//    $tbody.empty();
//    $tfoot.empty();

//    var batchSize = lignes.length > 100 ? 10 : 5;
//    var delai = lignes.length > 100 ? 10 : 30;
//    var index = 0;

//    function ajouterLot() {
//        if (index >= lignes.length) {
//            // Ajouter la ligne des totaux (somme des lignes comme Excel)
//            var $totalRow = $('<tr>').addClass('total-row');
//            $totalRow.append($('<td>').text('TOTAL'));
//            $totalRow.append($('<td>').text('-')); // Date
//            $totalRow.append($('<td>').text('-')); // Balance Debut
//            $totalRow.append($('<td>').text('-')); // Balance Fin
//            $totalRow.append($('<td>').text(formatMontant(data.totalCapitalRembourse))); // Principal
//            $totalRow.append($('<td>').text(formatMontant(data.totalInterets + data.totalTAF))); // Interet TTC
//            $totalRow.append($('<td>').text(formatMontant(data.totalInterets))); // Interet HT
//            $totalRow.append($('<td>').text(formatMontant(data.totalTAF))); // TPS
//            $totalRow.append($('<td>').text(formatMontant(data.totalMensualites))); // Echeance
//            $tfoot.append($totalRow);
//            return;
//        }

//        var finLot = Math.min(index + batchSize, lignes.length);

//        for (var i = index; i < finLot; i++) {
//            var ligne = lignes[i];
//            var $row = creerLigneTableau(ligne);
//            $row.hide().appendTo($tbody).fadeIn(150);
//        }

//        index = finLot;
//        setTimeout(ajouterLot, delai);
//    }

//    ajouterLot();
//}

//function creerLigneTableau(ligne) {
//    var $row = $('<tr>');

//    if (ligne.estEnDiffere) {
//        $row.addClass('differe');
//    }

//    if (ligne.estRemboursementAnticipe) {
//        $row.addClass('remboursement-anticipe');
//    }

//    // Pmt (numero echeance - ordre croissant)
//    if (ligne.estRemboursementAnticipe) {
//        $row.append($('<td>').text('RA').addClass('ra-label'));
//    } else {
//        $row.append($('<td>').text(ligne.indexLigne));
//    }

//    // Date
//    $row.append($('<td>').text(formatDate(ligne.dateEcheance)));
//    // Balance Debut Periode
//    $row.append($('<td>').text(formatMontant(ligne.capitalRestantDebut)));
//    // Balance Fin Periode
//    $row.append($('<td>').text(formatMontant(ligne.capitalRestantFin)));
//    // Principal Paye
//    $row.append($('<td>').text(formatMontant(ligne.capitalRembourse)));
//    // Interet Paye TTC
//    var interetTTC = ligne.interets + ligne.taf;
//    $row.append($('<td>').text(formatMontant(interetTTC)));
//    // Interet Paye HT
//    $row.append($('<td>').text(formatMontant(ligne.interets)));
//    // TPS sur Interet
//    $row.append($('<td>').text(ligne.estRemboursementAnticipe ? formatMontant(ligne.penaliteRA) : formatMontant(ligne.taf)));
//    // Montant de l'Echeance
//    $row.append($('<td>').text(formatMontant(ligne.mensualite)));

//    return $row;
//}

function creerLigneTableau(ligne) {

    // Compatibilit� camelCase / PascalCase
    var indexLigne = ligne.indexLigne ?? ligne.IndexLigne;
    var dateEcheance = ligne.dateEcheance ?? ligne.DateEcheance;
    var capitalRestantDebut = ligne.capitalRestantDebut ?? ligne.CapitalRestantDebut;
    var capitalRestantFin = ligne.capitalRestantFin ?? ligne.CapitalRestantFin;
    var capitalRembourse = ligne.capitalRembourse ?? ligne.CapitalRembourse;
    var interets = ligne.interets ?? ligne.Interets;
    var taf = ligne.taf ?? ligne.TAF;
    var mensualite = ligne.mensualite ?? ligne.Mensualite;
    var penaliteRA = ligne.penaliteRA ?? ligne.PenaliteRA;

    var estEnDiffere = ligne.estEnDiffere ?? ligne.EstEnDiffere;
    var estRemboursementAnticipe = ligne.estRemboursementAnticipe ?? ligne.EstRemboursementAnticipe;

    var $row = $('<tr>');

    if (estEnDiffere) {
        $row.addClass('differe');
    }

    if (estRemboursementAnticipe) {
        $row.addClass('remboursement-anticipe');
    }

    // Pmt
    if (estRemboursementAnticipe) {
        $row.append($('<td>').text('RA').addClass('ra-label'));
    } else {
        $row.append($('<td>').text(indexLigne));
    }

    // Date
    $row.append($('<td>').text(formatDate(dateEcheance)));

    // Balance D�but
    $row.append($('<td>').text(formatMontant(capitalRestantDebut)));

    // Balance Fin
    $row.append($('<td>').text(formatMontant(capitalRestantFin)));

    // Principal
    $row.append($('<td>').text(formatMontant(capitalRembourse)));

    // Int�r�ts TTC
    var interetTTC = safeNumber(interets) + safeNumber(taf);
    $row.append($('<td>').text(formatMontant(interetTTC)));

    // Int�r�ts HT
    $row.append($('<td>').text(formatMontant(interets)));

    // TPS/TAF ou p�nalit�
    $row.append($('<td>').text(estRemboursementAnticipe ? formatMontant(penaliteRA) : formatMontant(taf)));

    // Mensualit�
    $row.append($('<td>').text(formatMontant(mensualite)));

    return $row;
}


// =============================================================================
// BOUTONS D'EXPORT
// =============================================================================
function initExportButtons() {
    // Bouton Dossier Complet Word
    $('#btnDossierCompletWord').on('click', function () {
        exporterDossierCompletWord($(this));
    });
}

function exporterTableau(url, $button) {
    var formData = collecterDonneesFormulaire();
    var originalHtml = $button.html();

    activerSpinnerBouton($button);

    fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        body: JSON.stringify(formData)
    })
    .then(function (response) {
        if (!response.ok) throw new Error('Erreur lors de l\'export');
        return response.blob();
    })
    .then(function (blob) {
        var fileName = getFileNameFromUrl(url);
        var downloadUrl = window.URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = downloadUrl;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(downloadUrl);
        a.remove();
        desactiverSpinnerBouton($button, originalHtml);
    })
    .catch(function (error) {
        desactiverSpinnerBouton($button, originalHtml);
        afficherErreur('Erreur lors de l\'export: ' + error.message);
    });
}

function activerSpinnerBouton($button) {
    var width = $button.outerWidth();
    $button.css('min-width', width + 'px');
    $button.prop('disabled', true).addClass('btn-loading');
    $button.html('<span class="spinner"></span> Generation...');
}

function desactiverSpinnerBouton($button, originalHtml) {
    $button.prop('disabled', false).removeClass('btn-loading');
    $button.html(originalHtml);
    $button.css('min-width', '');
}

function getFileNameFromUrl(url) {
    var timestamp = new Date().toISOString().slice(0, 10).replace(/-/g, '');
    if (url.includes('Excel')) return 'Tableau_Amortissement_' + timestamp + '.xlsx';
    if (url.includes('Pdf')) return 'Tableau_Amortissement_' + timestamp + '.pdf';
    if (url.includes('Word')) return 'Tableau_Amortissement_' + timestamp + '.docx';
    return 'export.dat';
}

// Fonction spécifique pour l'export du Dossier Complet Word
function exporterDossierCompletWord($button) {
    var formData = collecterDonneesFormulaire();
    var originalHtml = $button.html();

    activerSpinnerBouton($button);

    fetch('/Amortissement/GenererDossierCompletWord', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        body: JSON.stringify(formData)
    })
    .then(function (response) {
        if (!response.ok) throw new Error('Erreur lors de la génération du dossier complet');
        return response.blob();
    })
    .then(function (blob) {
        var timestamp = new Date().toISOString().slice(0, 10).replace(/-/g, '');
        var clientNom = $('#clientNom').val() || 'Client';
        var fileName = 'Dossier_Complet_' + clientNom + '_' + timestamp + '.docx';

        var downloadUrl = window.URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = downloadUrl;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(downloadUrl);
        a.remove();
        desactiverSpinnerBouton($button, originalHtml);
    })
    .catch(function (error) {
        desactiverSpinnerBouton($button, originalHtml);
        afficherErreur('Erreur lors de la génération du dossier complet: ' + error.message);
    });
}

// =============================================================================
// BOUTONS DE DOCUMENTS CONTRACTUELS
// =============================================================================
function initDocumentButtons() {
    // Bouton Document Complet (remplace les boutons individuels)
    $('#btnGenererContrat, #btnGenererFiche').on('click', function () {
        exporterDocumentComplet($(this));
    });
}

function exporterDocumentComplet($button) {
    var formData = collecterDonneesFormulaire();
    var originalHtml = $button.html();

    activerSpinnerBouton($button);

    fetch('/Amortissement/ExportComplet', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        body: JSON.stringify(formData)
    })
    .then(function (response) {
        if (!response.ok) throw new Error('Erreur lors de la generation du document');
        return response.blob();
    })
    .then(function (blob) {
        var timestamp = new Date().toISOString().slice(0, 10).replace(/-/g, '');
        var clientNom = $('#clientNom').val() || 'Client';
        var fileName = 'Dossier_Credit_' + clientNom + '_' + timestamp + '.pdf';
        var downloadUrl = window.URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = downloadUrl;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(downloadUrl);
        a.remove();
        desactiverSpinnerBouton($button, originalHtml);
    })
    .catch(function (error) {
        desactiverSpinnerBouton($button, originalHtml);
        afficherErreur('Erreur lors de la generation du document: ' + error.message);
    });
}

// =============================================================================
// BOUTON REINITIALISER
// =============================================================================
function initResetButton() {
    $('#btnReset').on('click', function () {
        $('#amortissementForm')[0].reset();

        initDateDefaut();

        // Valeurs par defaut
        $('#tauxTAF').val(10);
        $('#tauxCommissionRachat').val(2);
        $('#tauxPenaliteRA').val(3);

        // Vider les encours
        $('#encoursList').empty();
        encoursData = [];
        mettreAJourBadgeEncours();
        $('#encoursResume').hide();

        // Masquer les resultats et alertes
        $('#resultsSection').hide();
        masquerErreur();
        masquerAlertes();

        // Reset capacite
        $('#capaciteMaxDisplay').text('-');
        $('#tauxEndettementMaxDisplay').text('(-)');

        // Retourner au premier onglet
        $('.tab-btn').first().click();
    });
}

// =============================================================================
// SYNCHRONISATION TAUX TTC
// =============================================================================
function initTauxTTCSync() {
    function calculerTauxTTC() {
        var tauxHT = parseFloat($('#tauxAnnuel').val()) || 0;
        var taf = parseFloat($('#tauxTAF').val()) || 0;
        var tauxTTC = tauxHT * (1 + taf / 100);
        $('#tauxTTC').val(tauxTTC.toFixed(2) + '%');
    }

    $('#tauxAnnuel, #tauxTAF').on('input', function () {
        calculerTauxTTC();
    });

    calculerTauxTTC();
}

// =============================================================================
// SYNCHRONISATION ASSURANCE
// =============================================================================
function initAssuranceSync() {
    // Calcul automatique de Prime Assurance GTA C2A
    function calculerPrimeGTAC2A() {
        var primeClient = parseFloat($('#primeAssuranceClient').val()) || 0;
        var commissionETG = parseFloat($('#commissionETG').val()) || 0;
        var primeGTAC2A = primeClient - commissionETG;
        $('#primeAssuranceGTAC2A').val(formatMontant(primeGTAC2A));
    }

    // Mise a jour automatique lors de la saisie
    $('#primeAssuranceClient, #commissionETG').on('input', function () {
        calculerPrimeGTAC2A();
    });

    // Calcul initial
    calculerPrimeGTAC2A();
}

// =============================================================================
// FONCTIONS UTILITAIRES
// =============================================================================
function formatMontant(montant) {
    if (montant === null || montant === undefined || isNaN(montant)) return '0';
    return Math.round(montant).toLocaleString('fr-FR');
}

//function formatDate(dateStr) {
//    if (!dateStr) return '-';
//    var date = new Date(dateStr);
//    var jour = ('0' + date.getDate()).slice(-2);
//    var mois = ('0' + (date.getMonth() + 1)).slice(-2);
//    var annee = date.getFullYear();
//    return jour + '/' + mois + '/' + annee;
//}
function formatDate(dateStr) {
    if (!dateStr) return '-';

    // Support du format .NET "/Date(1770681600000)/"
    if (typeof dateStr === "string" && dateStr.startsWith("/Date(")) {
        var timestamp = parseInt(dateStr.replace("/Date(", "").replace(")/", ""));
        var date = new Date(timestamp);
    } else {
        var date = new Date(dateStr);
    }

    if (isNaN(date.getTime())) return '-';

    var jour = ('0' + date.getDate()).slice(-2);
    var mois = ('0' + (date.getMonth() + 1)).slice(-2);
    var annee = date.getFullYear();
    return jour + '/' + mois + '/' + annee;
}


function afficherLoader(show) {
    var $btn = $('#btnCalculer');
    var $text = $btn.find('.btn-text');
    var $loader = $btn.find('.btn-loader');

    if (show) {
        $text.hide();
        $loader.show();
        $btn.prop('disabled', true);
    } else {
        $text.show();
        $loader.hide();
        $btn.prop('disabled', false);
    }
}

function afficherErreur(message) {
    $('#errorMessage').html(message);
    $('#errorContainer').show();
    $('html, body').animate({ scrollTop: $('#errorContainer').offset().top - 20 }, 300);
}

function masquerErreur() {
    $('#errorContainer').hide();
    $('#errorMessage').empty();
}

//Ajout de fonction 
function safeNumber(value) {
    var n = parseFloat(value);
    return isNaN(n) ? 0 : n;
}
