using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecouvrementAPI.Models
{
    // Table relance_client
    [Table("relance_client")]
    public class RelanceClient
    {
        [Key]
        [Column("id_relance")]
        public int IdRelance { get; set; }

        [Column("id_dossier")]
        public int IdDossier { get; set; }

        // email / sms / appel
        [Column("moyen")]
        public string Moyen { get; set; }

        // envoye / repondu / sans_reponse
        [Column("statut")]
        public string Statut { get; set; }

        [Column("date_relance")]
        public DateTime DateRelance { get; set; }

        public DossierRecouvrement Dossier { get; set; }
    }
}