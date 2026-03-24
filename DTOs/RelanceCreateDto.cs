using System.ComponentModel.DataAnnotations;

namespace RecouvrementAPI.DTOs
{
    public class RelanceCreateDto
    {
        // email / sms / appel
        [Required]
        [MaxLength(30)]
        public string Moyen { get; set; } = "email";

        // envoye / repondu / sans_reponse
        [Required]
        [MaxLength(30)]
        public string Statut { get; set; } = "envoye";
    }
}

