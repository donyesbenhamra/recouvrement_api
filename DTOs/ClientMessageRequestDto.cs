using System.ComponentModel.DataAnnotations;

namespace RecouvrementAPI.DTOs
{
    public class ClientMessageRequestDto
    {
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;
    }
}

