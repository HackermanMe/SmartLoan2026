/*
 * =============================================================================
 * JAVASCRIPT/JQUERY - TABLEAU D'AMORTISSEMENT
 * =============================================================================
 * Ce fichier gere toutes les interactions utilisateur et la communication
 * avec le serveur pour le module de tableau d'amortissement.
 *
 * Fonctionnalites:
 * - Soumission du formulaire via AJAX
 * - Generation dynamique du tableau avec jQuery (ligne par ligne animee)
 * - Export vers Excel, PDF, Word avec spinners
 * - Validation cote client
 * - Gestion des parametres avances
 *
 * Dependencies: jQuery (deja inclus dans le layout)
 * =============================================================================
 */

// =============================================================================
// INITIALISATION AU CHARGEMENT DU DOCUMENT
// =============================================================================
$(document).ready(function () {
    // -------------------------------------------------------------------------
    // Initialisation des composants
    // -------------------------------------------------------------------------
    initDateDefaut();
    initFormSubmit();
    initExportButtons();
    initResetButton();
});

// =============================================================================
// INITIALISATION DE LA DATE PAR DEFAUT
// =============================================================================
/**
 * Definit la date du jour comme valeur par defaut pour le champ dateDebut
 */
function initDateDefaut() {
    var today = new Date().toISOString().split('T')[0];
    $('#dateDebut').val(today);
}

// =============================================================================
// SOUMISSION DU FORMULAIRE
// =============================================================================
/**
 * Gere la soumission du formulaire et l'appel AJAX pour calculer le tableau
 */
function initFormSubmit() {
    $('#amortissementForm').on('submit', function (e) {
        e.preventDefault();

        // Validation cote client
        if (!validerFormulaire()) {
            return;
        }

        // Preparation des donnees
        var formData = collecterDonneesFormulaire();

        // Affichage du loader
        afficherLoader(true);
        masquerErreur();

        // Appel AJAX au serveur
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

                if (response.success) {
                    // Affichage des resultats
                    afficherResultats(response.data);
                } else {
                    // Affichage des erreurs
                    afficherErreur(response.errors.join('<br>'));
                }
            },
            //error: function (xhr, status, error) {
              //  afficherLoader(false);
               // afficherErreur('Une erreur est survenue lors de la communication avec le serveur.');
            //}
    error: function (xhr, status, error) {
    afficherLoader(false);
    console.log("STATUS:", xhr.status);
    console.log("ERROR:", error);
    console.log("RESPONSE TEXT:", xhr.responseText);
    afficherErreur("Erreur serveur: " + xhr.status + " - " + xhr.statusText);
        }
        });
    });
}

// =============================================================================
// COLLECTE DES DONNEES DU FORMULAIRE
// =============================================================================
/**
 * Collecte toutes les valeurs du formulaire et les structure pour l'API
 * @returns {Object} Objet contenant les parametres du pret
 */
function collecterDonneesFormulaire() {
    return {
        nomClient: $('#nomClient').val() || null,
        montantPret: parseFloat($('#montantPret').val()) || 0,
        tauxAnnuel: parseFloat($('#tauxAnnuel').val()) || 0,
        nombreEcheances: parseInt($('#nombreEcheances').val()) || 0,
        dateDebut: $('#dateDebut').val(),
        periodicite: parseInt($('#periodicite').val()) || 1,
        assurancePourcentage: parseFloat($('#assurancePourcentage').val()) || 0,
        assuranceFixe: parseFloat($('#assuranceFixe').val()) || 0,
        teg: parseFloat($('#teg').val()) || 0,
        tauxTAF: parseFloat($('#tauxTAF').val()) || 10,
        differeEcheances: parseInt($('#differeEcheances').val()) || 0
    };
}

// =============================================================================
// VALIDATION COTE CLIENT
// =============================================================================
/**
 * Valide les champs obligatoires avant soumission
 * @returns {boolean} true si le formulaire est valide
 */
function validerFormulaire() {
    var isValid = true;
    var errors = [];

    // Montant du pret
    var montant = parseFloat($('#montantPret').val());
    if (!montant || montant <= 0) {
        errors.push('Le montant du pret doit etre positif');
        isValid = false;
    }

    // Taux annuel
    var taux = parseFloat($('#tauxAnnuel').val());
    if (isNaN(taux) || taux < 0 || taux > 100) {
        errors.push('Le taux doit etre entre 0 et 100%');
        isValid = false;
    }

    // Nombre d'echeances
    var echeances = parseInt($('#nombreEcheances').val());
    if (!echeances || echeances < 1) {
        errors.push('Le nombre d\'echeances doit etre au moins 1');
        isValid = false;
    }

    // Date de debut
    if (!$('#dateDebut').val()) {
        errors.push('La date de debut est obligatoire');
        isValid = false;
    }

    // Differe
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
/**
 * Affiche le resume et genere le tableau d'amortissement
 * @param {Object} data - Donnees du tableau d'amortissement
 */
function afficherResultats(data) {
    // Afficher la section des resultats
    $('#resultsSection').show();

    // Mise a jour du resume
    afficherResume(data);

    // Generation du tableau avec animation ligne par ligne
    genererTableauProgressif(data.lignes);

    // Scroll vers les resultats
    $('html, body').animate({
        scrollTop: $('#resultsSection').offset().top - 20
    }, 500);
}

/**
 * Met a jour les champs du resume avec les donnees calculees
 * @param {Object} data - Donnees du resume
 */
function afficherResume(data) {
    // Mensualite mise en avant avec animation
    var $mensualite = $('#mensualiteDisplay');
    $mensualite.fadeOut(100, function() {
        $(this).text(formatMontant(data.mensualite)).fadeIn(300);
    });

    // Grille du resume
    $('#resumeMontant').text(formatMontant(data.montantPret));
    $('#resumeTaux').text(data.tauxAnnuel.toFixed(2) + '%');
    $('#resumeEcheances').text(data.nombreEcheances);
    $('#resumeMensualite').text(formatMontant(data.mensualite));
    $('#resumeAssurancePct').text($('#assurancePourcentage').val() + '%');
    $('#resumeAssuranceFixe').text(formatMontant(parseFloat($('#assuranceFixe').val()) || 0));
    $('#resumeMensualiteAssurance').text(formatMontant(data.mensualiteAvecAssurance));
    $('#resumeTEG').text(data.teg.toFixed(2) + '%');
    $('#resumeCoutTotal').text(formatMontant(data.coutTotalCredit));
    $('#resumeDateDebut').text(formatDate(data.dateDebut));
}

// =============================================================================
// GENERATION PROGRESSIVE DU TABLEAU (LIGNE PAR LIGNE)
// =============================================================================
/**
 * Genere le tableau d'amortissement avec animation ligne par ligne
 * @param {Array} lignes - Tableau des lignes d'amortissement
 */
function genererTableauProgressif(lignes) {
    var $tbody = $('#tableauBody');

    // Vider le tableau existant
    $tbody.empty();

    // Calculer les totaux pour la ligne TOTAL
    var totaux = calculerTotaux(lignes);

    // Compteur pour l'index
    var index = 0;

    // Nombre de lignes a afficher simultanement pour une meilleure performance
    // Si beaucoup de lignes, on les groupe par lots
    var batchSize = lignes.length > 100 ? 10 : 5;

    // Delai entre chaque lot (en ms) - plus rapide si beaucoup de lignes
    var delai = lignes.length > 100 ? 10 : 30;

    /**
     * Fonction recursive pour ajouter les lignes par lots
     */
    function ajouterLot() {
        // Verifier s'il reste des lignes a ajouter
        if (index >= lignes.length) {
            // Ajouter la ligne TOTAL a la fin
            var $totalRow = creerLigneTotaux(totaux);
            $totalRow.hide().appendTo($tbody).fadeIn(300);
            return; // Termine
        }

        // Determiner la fin du lot actuel
        var finLot = Math.min(index + batchSize, lignes.length);

        // Ajouter les lignes du lot
        for (var i = index; i < finLot; i++) {
            var ligne = lignes[i];
            var $row = creerLigneTableau(ligne);

            // Ajouter la ligne avec une animation de fade-in
            $row.hide().appendTo($tbody).fadeIn(150);
        }

        // Mettre a jour l'index
        index = finLot;

        // Programmer le prochain lot
        setTimeout(ajouterLot, delai);
    }

    // Demarrer la generation progressive
    ajouterLot();
}

/**
 * Calcule les totaux des colonnes pour la ligne TOTAL
 * @param {Array} lignes - Tableau des lignes d'amortissement
 * @returns {Object} Objet contenant les totaux
 */
function calculerTotaux(lignes) {
    var totaux = {
        interets: 0,
        taf: 0,
        capitalRembourse: 0,
        mensualite: 0
    };

    for (var i = 0; i < lignes.length; i++) {
        totaux.interets += lignes[i].interets || 0;
        totaux.taf += lignes[i].taf || 0;
        totaux.capitalRembourse += lignes[i].capitalRembourse || 0;
        totaux.mensualite += lignes[i].mensualite || 0;
    }

    return totaux;
}

/**
 * Cree la ligne TOTAL du tableau
 * @param {Object} totaux - Objet contenant les totaux
 * @returns {jQuery} Element TR jQuery
 */
function creerLigneTotaux(totaux) {
    var $row = $('<tr>').addClass('total-row');

    $row.append($('<td>').text('TOTAL').addClass('total-label'));
    $row.append($('<td>').text('')); // Date - vide
    $row.append($('<td>').text('')); // Montant - vide
    $row.append($('<td>').text(formatMontant(totaux.interets)));
    $row.append($('<td>').text(formatMontant(totaux.taf)));
    $row.append($('<td>').text(formatMontant(totaux.capitalRembourse)));
    $row.append($('<td>').text(formatMontant(totaux.mensualite)));
    $row.append($('<td>').text('')); // Capital restant - vide

    return $row;
}

/**
 * Cree une ligne du tableau
 * @param {Object} ligne - Donnees de la ligne
 * @returns {jQuery} Element TR jQuery
 */
function creerLigneTableau(ligne) {
    var $row = $('<tr>');

    // Classe speciale pour les lignes en differe
    if (ligne.estEnDiffere) {
        $row.addClass('differe');
    }

    // Construction des cellules
    $row.append($('<td>').text(ligne.numeroEcheance));
    $row.append($('<td>').text(formatDate(ligne.dateEcheance)));
    $row.append($('<td>').text(formatMontant(ligne.capitalRestantDebut)));
    $row.append($('<td>').text(formatMontant(ligne.interets)));
    $row.append($('<td>').text(formatMontant(ligne.taf)));
    $row.append($('<td>').text(formatMontant(ligne.capitalRembourse)));
    $row.append($('<td>').text(formatMontant(ligne.mensualite)));
    $row.append($('<td>').text(formatMontant(ligne.capitalRestantFin)));

    return $row;
}

// =============================================================================
// BOUTONS D'EXPORT AVEC SPINNERS
// =============================================================================
/**
 * Initialise les gestionnaires d'evenements pour les boutons d'export
 */
function initExportButtons() {
    // Export Excel
    $('#btnExportExcel').on('click', function () {
        exporterTableau('/Amortissement/ExportExcel', $(this));
    });

    // Export PDF
    $('#btnExportPdf').on('click', function () {
        exporterTableau('/Amortissement/ExportPdf', $(this));
    });

    // Export Word
    $('#btnExportWord').on('click', function () {
        exporterTableau('/Amortissement/ExportWord', $(this));
    });
}

/**
 * Effectue l'export du tableau vers le format specifie
 * @param {string} url - URL de l'endpoint d'export
 * @param {jQuery} $button - Bouton clique
 */
function exporterTableau(url, $button) {
    var formData = collecterDonneesFormulaire();

    // Sauvegarder le contenu original du bouton
    var originalHtml = $button.html();

    // Activer le spinner et desactiver le bouton
    activerSpinnerBouton($button);

    // Utiliser fetch pour un POST avec JSON
    fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        body: JSON.stringify(formData)
    })
    .then(function(response) {
        if (!response.ok) {
            throw new Error('Erreur lors de l\'export');
        }
        return response.blob();
    })
    .then(function(blob) {
        // Extraire le nom du fichier selon le type d'export
        var fileName = getFileNameFromUrl(url);

        // Creer un lien de telechargement
        var downloadUrl = window.URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = downloadUrl;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(downloadUrl);
        a.remove();

        // Restaurer le bouton
        desactiverSpinnerBouton($button, originalHtml);
    })
    .catch(function(error) {
        // Restaurer le bouton en cas d'erreur
        desactiverSpinnerBouton($button, originalHtml);
        afficherErreur('Erreur lors de l\'export: ' + error.message);
    });
}

/**
 * Active le spinner sur un bouton d'export
 * @param {jQuery} $button - Bouton a modifier
 */
function activerSpinnerBouton($button) {
    // Sauvegarder la largeur actuelle pour eviter le redimensionnement
    var width = $button.outerWidth();
    $button.css('min-width', width + 'px');

    // Desactiver le bouton
    $button.prop('disabled', true);

    // Ajouter la classe de chargement
    $button.addClass('btn-loading');

    // Remplacer le contenu par un spinner
    $button.html('<span class="spinner"></span> Generation...');
}

/**
 * Desactive le spinner et restaure le bouton
 * @param {jQuery} $button - Bouton a restaurer
 * @param {string} originalHtml - Contenu HTML original
 */
function desactiverSpinnerBouton($button, originalHtml) {
    // Reactiver le bouton
    $button.prop('disabled', false);

    // Retirer la classe de chargement
    $button.removeClass('btn-loading');

    // Restaurer le contenu original
    $button.html(originalHtml);

    // Retirer la largeur fixe
    $button.css('min-width', '');
}

/**
 * Determine le nom du fichier selon le type d'export
 * @param {string} url - URL de l'endpoint
 * @returns {string} Nom du fichier avec extension
 */
function getFileNameFromUrl(url) {
    var timestamp = new Date().toISOString().slice(0, 10).replace(/-/g, '');

    if (url.includes('Excel')) {
        return 'Tableau_Amortissement_' + timestamp + '.xlsx';
    } else if (url.includes('Pdf')) {
        return 'Tableau_Amortissement_' + timestamp + '.pdf';
    } else if (url.includes('Word')) {
        return 'Tableau_Amortissement_' + timestamp + '.docx';
    }
    return 'export.dat';
}

// =============================================================================
// BOUTON REINITIALISER
// =============================================================================
/**
 * Initialise le bouton de reinitialisation du formulaire
 */
function initResetButton() {
    $('#btnReset').on('click', function () {
        // Reinitialiser le formulaire
        $('#amortissementForm')[0].reset();

        // Remettre la date du jour
        initDateDefaut();

        // Remettre les valeurs par defaut
        $('#tauxTAF').val(10);
        $('#assurancePourcentage').val(0);
        $('#assuranceFixe').val(0);
        $('#teg').val(0);
        $('#differeEcheances').val(0);

        // Masquer les resultats
        $('#resultsSection').hide();

        // Masquer les erreurs
        masquerErreur();
    });
}

// =============================================================================
// FONCTIONS UTILITAIRES
// =============================================================================

/**
 * Formate un montant avec separateur de milliers
 * @param {number} montant - Montant a formater
 * @returns {string} Montant formate
 */
function formatMontant(montant) {
    if (montant === null || montant === undefined || isNaN(montant)) {
        return '0';
    }
    return Math.round(montant).toLocaleString('fr-FR');
}

/**
 * Formate une date au format JJ/MM/AAAA
 * @param {string} dateStr - Date au format ISO
 * @returns {string} Date formatee
 */
function formatDate(dateStr) {
    if (!dateStr) return '-';

    var date = new Date(dateStr);
    var jour = ('0' + date.getDate()).slice(-2);
    var mois = ('0' + (date.getMonth() + 1)).slice(-2);
    var annee = date.getFullYear();

    return jour + '/' + mois + '/' + annee;
}

/**
 * Affiche ou masque le loader du bouton de calcul
 * @param {boolean} show - true pour afficher, false pour masquer
 */
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

/**
 * Affiche un message d'erreur
 * @param {string} message - Message d'erreur a afficher
 */
function afficherErreur(message) {
    $('#errorMessage').html(message);
    $('#errorContainer').show();

    // Scroll vers l'erreur
    $('html, body').animate({
        scrollTop: $('#errorContainer').offset().top - 20
    }, 300);
}

/**
 * Masque le conteneur d'erreur
 */
function masquerErreur() {
    $('#errorContainer').hide();
    $('#errorMessage').empty();
}
