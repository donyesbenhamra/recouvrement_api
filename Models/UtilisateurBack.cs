using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecouvrementAPI.Models
{
    [Table("utilisateur_back")] // Lie cette classe à la table "utilisateur_back" dans la base de données
    public class UtilisateurBack
    {
        [Key] // Clé primaire
        [Column("id_utilisateur_back")] // Correspond à la colonne id_utilisateur_back
        public int IdAgent { get; set; }

        [Column("id_agence")] 
        public int? IdAgence { get; set; } 
        // Clé étrangère vers la table Agence
        // Nullable car un agent peut exister sans être encore affecté

        [Required]
        [MaxLength(100)]
        [Column("nom")]
        public string Nom { get; set; }
        // Nom de l’agent

        [Required]
        [MaxLength(100)]
        [Column("prenom")]
        public string Prenom { get; set; }
        // Prénom de l’agent

        [Required]
        [MaxLength(150)]
        [EmailAddress]
        [Column("email")]
        public string Email { get; set; }
        // Email unique utilisé pour l’authentification

        [Required]
        [Column("mot_de_passe")]
        public string MotDePasse { get; set; }
        // Mot de passe (à stocker HASHÉ, jamais en clair)

        [Required]
        [MaxLength(50)]
        [Column("role")]
        public string Role { get; set; }
        // Rôle de l’agent (ex: admin, agent_recouvrement)

        // Relation avec Agence
        [ForeignKey("IdAgence")]
        public Agence Agence { get; set; }
        // Navigation : permet d’accéder à l’agence de l’agent
    }
}