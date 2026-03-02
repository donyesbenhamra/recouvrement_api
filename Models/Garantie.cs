using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecouvrementAPI.Models
{
    // Table garantie
    [Table("garantie")]
    public class Garantie
    {
        [Key]
        [Column("id_garantie")]
        public int IdGarantie { get; set; }

        [Column("id_dossier")]
        public int IdDossier { get; set; }

        // hypotheque / salaire / caution
        [Column("type_garantie")]
        public string TypeGarantie { get; set; }

        [Column("description")]
        public string Description { get; set; }

        public DossierRecouvrement Dossier { get; set; }
    }
}