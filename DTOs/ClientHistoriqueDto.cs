namespace RecouvrementAPI.DTOs
{
    public class ClientHistoriqueDto
    {
        // Informations globales du client
        public string NomComplet { get; set; }
        public int IdAgence { get; set; }
        public string VilleAgence { get; set; }

        
        public List<DossierDto> Dossiers { get; set; }  
    }
}