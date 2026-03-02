namespace RecouvrementAPI.DTOs
{
    public class EcheanceDto
    {
        public decimal Montant { get; set; }
        public DateTime DateEcheance { get; set; }
        public string Statut { get; set; }
    }
}