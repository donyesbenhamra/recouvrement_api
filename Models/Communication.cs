using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecouvrementAPI.Models
{
    // Table communication
    [Table("communication")]
    public class Communication
    {
        [Key]
        [Column("id_communication")]
        public int IdCommunication { get; set; }

        [Column("id_dossier")]
        public int IdDossier { get; set; }

        [Column("message")]
        public string Message { get; set; }

        // client / agent / systeme
        [Column("origine")]
        public string Origine { get; set; }

        [Column("date_envoi")]
        public DateTime DateEnvoi { get; set; }

        public DossierRecouvrement Dossier { get; set; }
    }
}