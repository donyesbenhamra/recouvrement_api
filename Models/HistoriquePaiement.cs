using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecouvrementAPI.Models
{
    // Table historique_paiement
    [Table("historique_paiement")]
    public class HistoriquePaiement
    {
        [Key]
        [Column("id_paiement")]
        public int IdPaiement { get; set; }

        [Column("id_dossier")]
        public int IdDossier { get; set; }

        [Column("montant_paye")]
        public decimal MontantPaye { get; set; }

        [Column("type_paiement")]
        public string TypePaiement { get; set; }

        [Column("date_paiement")]
        public DateTime DatePaiement { get; set; }

        public DossierRecouvrement Dossier { get; set; }
    }
}