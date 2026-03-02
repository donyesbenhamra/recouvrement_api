#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecouvrementAPI.Models
{
    [Table("intention_client")]
    public class IntentionClient
    {
        [Key]
        [Column("id_intention")]
        public int IdIntention { get; set; }

        [Column("id_dossier")]
        public int IdDossier { get; set; }

        [Required]
        [Column("type_intention")]
        public string TypeIntention { get; set; } = string.Empty; // paiement_immediat, promesse_paiement, paiement_partiel, demande_consolidation, reclamation

        [Column("date_intention")]
        public DateTime DateIntention { get; set; }

        [Column("date_paiement_prevue")]
        public DateTime? DatePaiementPrevue { get; set; } //

        // Colonne réelle à ajouter en SQL pour le règlement partiel
        [Column("montant_propose")]
        public decimal? MontantPropose { get; set; }

        // PROPRIÉTÉ COMMENTAIRE :
        // Elle n'existe pas en base de données pour cette table (NotMapped)
        // Mais elle permet de recevoir le message du client pour l'envoyer vers la table 'communication'
        [NotMapped]
        public string? Commentaire { get; set; } 
        public DossierRecouvrement? Dossier { get; set; }
    }
}