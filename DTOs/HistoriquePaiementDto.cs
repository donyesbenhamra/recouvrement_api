namespace RecouvrementAPI.DTOs
{
    public class HistoriquePaiementDto
    {
        public decimal MontantPaye { get; set; }
        public string TypePaiement { get; set; }
        public DateTime DatePaiement { get; set; }
    }
}