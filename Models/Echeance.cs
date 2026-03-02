using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecouvrementAPI.Models
{
    [Table("echeance")]
    public class Echeance
    {
        [Key]
        [Column("id_echeance")]
        public int IdEcheance { get; set; } // Clé primaire

        [Column("id_dossier")]
        public int IdDossier { get; set; } // Clé étrangère vers le dossier

        [Column("montant")]
        public decimal Montant { get; set; } // Montant à payer

        [Column("date_echeance")]
        public DateTime DateEcheance { get; set; } // Date de paiement prévue

        [Column("statut")]
        public string Statut { get; set; } 
        // impaye / paye / partiel

        public DossierRecouvrement Dossier { get; set; } 
        // Navigation vers le dossier
    }
}