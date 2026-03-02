namespace RecouvrementAPI.DTOs
{
    // DTO (Data Transfer Object) :
    // Sert à transférer uniquement les informations nécessaires vers le front.
    // Sécurité : on ne retourne pas toute la table Client (pas de token, email, etc.).
    public class ClientFormDto
    {
        public string NomComplet { get; set; } 
        // Nom complet du client (Nom + Prénom)

        public string VilleAgence { get; set; } 
        // AJOUT : la ville de l'agence associée au client
        // Permet au client de savoir à quelle agence il est rattaché

        public decimal MontantImpaye { get; set; } 
        // Montant restant à payer dans le dossier actif

        public decimal FraisDossier { get; set; } 
        // Frais associés au dossier

        public string StatutDossier { get; set; } 
        // Statut du dossier : aimable / contentieux / regularisé

        public DateTime? DateEcheance { get; set; } 
        // Date d'échéance du paiement
    }
}