using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecouvrementAPI.Models
{
    // Table score_risque
    [Table("score_risque")]
    public class ScoreRisque
    {
        [Key]
        [Column("id_score")]
        public int IdScore { get; set; }

        [Column("id_dossier")]
        public int IdDossier { get; set; }

        [Column("valeur")]
        public decimal Valeur { get; set; }

        [Column("date_calcul")]
        public DateTime DateCalcul { get; set; }

        public DossierRecouvrement Dossier { get; set; }
    }
}