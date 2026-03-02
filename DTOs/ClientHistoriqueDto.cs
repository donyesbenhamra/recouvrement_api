using System;
using System.Collections.Generic;

namespace RecouvrementAPI.DTOs
{
    public class ClientHistoriqueDto
    {
        public string NomComplet { get; set; }
        
        
        public int IdAgence { get; set; } 
        
        public string VilleAgence { get; set; }
        public decimal MontantImpaye { get; set; }
        public decimal FraisDossier { get; set; }
        public string StatutDossier { get; set; }
        public DateTime? DateEcheance { get; set; }

        public List<EcheanceDto> Echeances { get; set; }
        public List<HistoriquePaiementDto> Paiements { get; set; }
        public List<RelanceDto> Relances { get; set; }
        public List<CommunicationDto> Communications { get; set; }
    }
}