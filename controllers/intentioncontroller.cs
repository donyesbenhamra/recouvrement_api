using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecouvrementAPI.Data;
using RecouvrementAPI.Models;

namespace RecouvrementAPI.Controllers
{
    [ApiController]
    [Route("api/intention")]
    public class IntentionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public IntentionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==============================
        // POST api/intention
        // Enregistrer l’intention du client
        // ==============================
        [HttpPost]
        public async Task<IActionResult> AjouterIntention([FromBody] IntentionClient intention)
        {
            if (intention == null)
                return BadRequest("Données manquantes");

            if (string.IsNullOrEmpty(intention.TypeIntention))
                return BadRequest("Type intention requis");

            var dossier = await _context.Dossiers.FindAsync(intention.IdDossier);
            if (dossier == null)
                return NotFound("Dossier introuvable");

            //  Blocage multi-soumission (1 intention / jour)
            bool dejaSoumis = await _context.Intentions.AnyAsync(i =>
                i.IdDossier == intention.IdDossier &&
                i.DateIntention.Date == DateTime.Today);

            if (dejaSoumis)
            {
                return BadRequest(new
                {
                    message = "Vous avez déjà soumis une réponse aujourd'hui. Veuillez contacter votre agence."
                });
            }

            intention.DateIntention = DateTime.Now;
            _context.Intentions.Add(intention);

            string commentairePart = string.IsNullOrEmpty(intention.Commentaire)
                ? ""
                : $" Commentaire : {intention.Commentaire}";

            // ==============================
            // CAS 1 : Paiement immédiat
            // ==============================
            if (intention.TypeIntention == "paiement_immediat")
            {
                _context.Communications.Add(new Communication
                {
                    IdDossier = intention.IdDossier,
                    Message = $"Le client a indiqué vouloir effectuer un paiement immédiat.{commentairePart}",
                    Origine = "systeme",
                    DateEnvoi = DateTime.Now
                });

                _context.HistoriqueActions.Add(new HistoriqueAction
                {
                    IdDossier = intention.IdDossier,
                    ActionDetail = "Client : intention de paiement immédiat déclarée",
                    Acteur = "client",
                    DateAction = DateTime.Now
                });
            }

            // ==============================
            // CAS 2 : Promesse de paiement
            // ==============================
            else if (intention.TypeIntention == "promesse_paiement"
                     && intention.DatePaiementPrevue.HasValue)
            {
                _context.Echeances.Add(new Echeance
                {
                    IdDossier = dossier.IdDossier,
                    Montant = dossier.MontantImpaye,
                    DateEcheance = intention.DatePaiementPrevue.Value,
                    Statut = "impaye"
                });

                _context.Communications.Add(new Communication
                {
                    IdDossier = intention.IdDossier,
                    Message = $"Le client a promis un paiement pour le {intention.DatePaiementPrevue.Value:dd/MM/yyyy}.{commentairePart}",
                    Origine = "systeme",
                    DateEnvoi = DateTime.Now
                });

                _context.HistoriqueActions.Add(new HistoriqueAction
                {
                    IdDossier = intention.IdDossier,
                    ActionDetail = $"Promesse de paiement prévue le {intention.DatePaiementPrevue.Value:dd/MM/yyyy}",
                    Acteur = "client",
                    DateAction = DateTime.Now
                });
            }

            // ==============================
            // CAS 3 : Réclamation
            // ==============================
            else if (intention.TypeIntention == "reclamation")
            {
                dossier.StatutDossier = "contentieux";

                _context.Communications.Add(new Communication
                {
                    IdDossier = intention.IdDossier,
                    Message = $"Le client a soumis une réclamation. Dossier passé en contentieux.{commentairePart}",
                    Origine = "systeme",
                    DateEnvoi = DateTime.Now
                });

                _context.HistoriqueActions.Add(new HistoriqueAction
                {
                    IdDossier = intention.IdDossier,
                    ActionDetail = "Réclamation soumise — dossier passé en contentieux",
                    Acteur = "client",
                    DateAction = DateTime.Now
                });
            }

            // ==============================
            // CAS 4 : Demande d’échéancier
            // ==============================
            else if (intention.TypeIntention == "demande_echeance")
            {
                _context.Communications.Add(new Communication
                {
                    IdDossier = intention.IdDossier,
                    Message = $"Le client demande un échéancier de paiement.{commentairePart}",
                    Origine = "systeme",
                    DateEnvoi = DateTime.Now
                });

                _context.HistoriqueActions.Add(new HistoriqueAction
                {
                    IdDossier = intention.IdDossier,
                    ActionDetail = "Demande d’échéancier soumise",
                    Acteur = "client",
                    DateAction = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Intention enregistrée avec succès", type = intention.TypeIntention });
        }

        // ==============================
        // GET api/intention/{idDossier}
        // Récupérer les intentions d’un dossier
        // ==============================
        [HttpGet("{idDossier}")]
        public async Task<IActionResult> GetIntentions(int idDossier)
        {
            var intentions = await _context.Intentions
                .Where(i => i.IdDossier == idDossier)
                .OrderByDescending(i => i.DateIntention)
                .ToListAsync();

            if (!intentions.Any())
                return NotFound("Aucune intention trouvée pour ce dossier");

            return Ok(intentions);
        }
    }
}