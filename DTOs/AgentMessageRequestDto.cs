using System.ComponentModel.DataAnnotations;

namespace RecouvrementAPI.DTOs
{
    public class AgentMessageRequestDto
    {
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        // telephone / email / portail
        [Required]
        [MaxLength(30)]
        public string Canal { get; set; } = "portail";
    }
}

