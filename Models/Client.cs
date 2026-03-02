using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace RecouvrementAPI.Models
{
    // Table "client" : stocke les informations personnelles des clients
    [Table("client")]
    public class Client
    {
        [Key]
        [Column("id_client")]
        public int IdClient { get; set; } // Clé primaire

        [Column("id_agence")]
        public int? IdAgence { get; set; } // Clé étrangère vers l'agence

        [Required]
        [Column("nom")]
        public string Nom { get; set; } // Nom du client

        [Required]
        [Column("prenom")]
        public string Prenom { get; set; } // Prénom du client

        [Column("token_acces")]
        public string TokenAcces { get; set; } 
        // Token unique pour accéder au formulaire sans login (sécurité)

        public Agence Agence { get; set; } 
        // Navigation vers l'agence pour récupérer la ville

        public ICollection<DossierRecouvrement> Dossiers { get; set; } 
        // Navigation vers les dossiers du client
    }
}