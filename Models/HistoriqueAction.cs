using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecouvrementAPI.Models
{
    // Table historique_action
    [Table("historique_action")]
    public class HistoriqueAction
    {
        [Key]
        [Column("id_action")]
        public int IdAction { get; set; }

        [Column("id_dossier")]
        public int IdDossier { get; set; }

        [Column("action_detail")]
        public string ActionDetail { get; set; }

        // client / agent / systeme
        [Column("acteur")]
        public string Acteur { get; set; }

        [Column("date_action")]
        public DateTime DateAction { get; set; }

        public DossierRecouvrement Dossier { get; set; }
    }
}