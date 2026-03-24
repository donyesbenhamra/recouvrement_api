using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecouvrementAPI.Data;
using RecouvrementAPI.DTOs;
using RecouvrementAPI.Models;

namespace RecouvrementAPI.Controllers
{
    // Endpoints "banque" (sans UI agent).
    // Protégés par JWT si tu actives un vrai login agent plus tard.
    [ApiController]
    [Route("api/backoffice")]
    public class BackOfficeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BackOfficeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==============================
        // POST api/backoffice/dossier/{idDossier}/relance
        // Crée une relance banque (visible côté client dans l'onglet Relances)
        // ==============================
        [HttpPost("dossier/{idDossier:int}/relance")]
        // TEMP: laisse ouvert tant que tu n'as pas encore le login agent/JWT.
        // Quand tu auras l'auth, remets [Authorize].
        public async Task<IActionResult> CreerRelance(int idDossier, [FromBody] RelanceCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Données manquantes");

            var dossier = await _context.Dossiers.FindAsync(idDossier);
            if (dossier == null)
                return NotFound("Dossier introuvable");

            var relance = new RelanceClient
            {
                IdDossier = idDossier,
                Moyen = dto.Moyen,
                Statut = dto.Statut,
                DateRelance = DateTime.Now
            };

            _context.Relances.Add(relance);

            _context.HistoriqueActions.Add(new HistoriqueAction
            {
                IdDossier = idDossier,
                ActionDetail = $"Relance banque créée (canal={dto.Moyen}, statut={dto.Statut})",
                Acteur = "agent",
                DateAction = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Relance créée" });
        }

        // ==============================
        // POST api/backoffice/dossier/{idDossier}/message
        // Réponse banque au client (visible côté client dans l'onglet Communications si tu affiches origine agent)
        // ==============================
        [HttpPost("dossier/{idDossier:int}/message")]
        // TEMP: laisse ouvert tant que tu n'as pas encore le login agent/JWT.
        // Quand tu auras l'auth, remets [Authorize].
        public async Task<IActionResult> EnvoyerMessageBanque(int idDossier, [FromBody] AgentMessageRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest("Message requis");

            var dossier = await _context.Dossiers.FindAsync(idDossier);
            if (dossier == null)
                return NotFound("Dossier introuvable");

            var canal = (dto.Canal ?? "portail").Trim().ToLowerInvariant();
            var message = dto.Message.Trim();

            // On garde le modèle Communication simple, et on préfixe le canal dans le message
            // (si tu veux un champ Canal plus tard, on fera une migration propre).
            _context.Communications.Add(new Communication
            {
                IdDossier = idDossier,
                Message = $"[{canal}] {message}",
                Origine = "agent",
                DateEnvoi = DateTime.Now
            });

            _context.HistoriqueActions.Add(new HistoriqueAction
            {
                IdDossier = idDossier,
                ActionDetail = $"Message banque envoyé (canal={canal})",
                Acteur = "agent",
                DateAction = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Message banque envoyé" });
        }
    }
}

