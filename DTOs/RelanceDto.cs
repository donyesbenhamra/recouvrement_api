namespace RecouvrementAPI.DTOs
{
    public class RelanceDto
    {
        public DateTime DateRelance { get; set; }
        public string Moyen { get; set; }
        public string Statut { get; set; }

        // Réponse banque associée (si disponible)
        public string ReponseBanque { get; set; }
        public DateTime DateReponseBanque { get; set; }
    }
}