using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecouvrementAPI.Data;
using RecouvrementAPI.DTOs;
using RecouvrementAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace RecouvrementAPI.Controllers
{
    [ApiController]
    [Route("api/client")]
    public class ClientController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ClientController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ==============================
        // GET api/client/historique/{token}
        // ==============================
        [HttpGet("historique/{token}")]
        public async Task<IActionResult> GetHistorique(string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Token requis");

            var client = await _context.Clients
                .Include(c => c.Agence)
                .Include(c => c.Dossiers)
                    .ThenInclude(d => d.Echeances)
                .Include(c => c.Dossiers)
                    .ThenInclude(d => d.HistoriquePaiements)
                .Include(c => c.Dossiers)
                    .ThenInclude(d => d.Relances)
                .Include(c => c.Dossiers)
                    .ThenInclude(d => d.Communications)
                .FirstOrDefaultAsync(c => c.TokenAcces == token);

            if (client == null)
                return Unauthorized("Token invalide");

            var dossier = client.Dossiers
                .OrderByDescending(d => d.DateCreation)
                .FirstOrDefault();

            if (dossier == null)
                return NotFound("Aucun dossier trouvé pour ce client");

            var dto = new ClientHistoriqueDto
            {
                NomComplet = client.Nom + " " + client.Prenom,
                IdAgence = client.Agence != null ? client.Agence.IdAgence : 0,
                VilleAgence = client.Agence?.Ville,
                MontantImpaye = dossier.MontantImpaye,
                FraisDossier = dossier.FraisDossier,
                StatutDossier = dossier.StatutDossier,

                DateEcheance = dossier.Echeances
                    .OrderBy(e => e.DateEcheance)
                    .Select(e => e.DateEcheance)
                    .FirstOrDefault(),

                Echeances = dossier.Echeances.Select(e => new EcheanceDto
                {
                    Montant = e.Montant,
                    DateEcheance = e.DateEcheance,
                    Statut = e.Statut
                }).ToList(),

                Paiements = dossier.HistoriquePaiements.Select(p => new HistoriquePaiementDto
                {
                    MontantPaye = p.MontantPaye,
                    TypePaiement = p.TypePaiement,
                    DatePaiement = p.DatePaiement
                }).ToList(),

                Relances = dossier.Relances.Select(r => new RelanceDto
                {
                    DateRelance = r.DateRelance,
                    Moyen = r.Moyen,
                    Statut = r.Statut
                }).ToList(),

                Communications = dossier.Communications.Select(c => new CommunicationDto
                {
                    Message = c.Message,
                    Origine = c.Origine,
                    DateEnvoi = c.DateEnvoi
                }).ToList()
            };

            return Ok(dto);
        }

        // ==============================
        // GET api/client/recu/{token}
        // ==============================
        [HttpGet("recu/{token}")]
        public async Task<IActionResult> GenerateRecu(string token)
        {
            var client = await _context.Clients
                .Include(c => c.Agence)
                .Include(c => c.Dossiers)
                .FirstOrDefaultAsync(c => c.TokenAcces == token);

            if (client == null)
                return Unauthorized("Token invalide");

            var dossier = client.Dossiers
                .OrderByDescending(d => d.DateCreation)
                .FirstOrDefault();

            if (dossier == null)
                return NotFound("Aucun dossier trouvé");

            string colorHex = dossier.StatutDossier == "regularise" ? Colors.Green.Medium :
                             (dossier.StatutDossier == "contentieux" ? Colors.Red.Medium : Colors.Blue.Medium);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("REÇU DE SITUATION").FontSize(22).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text($"Date d'édition : {DateTime.Now:dd/MM/yyyy}");
                        });
                        row.RelativeItem().AlignRight().Text($"{client.Agence?.Ville}").FontSize(16).Bold();
                    });

                    page.Content().PaddingVertical(25).Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Text(text => {
                            text.Span("Client : ").Bold();
                            text.Span($"{client.Nom} {client.Prenom}");
                        });

                        col.Item().Row(r => {
                            r.AutoItem().Text("Statut du dossier : ").Bold();
                            r.AutoItem().Background(colorHex).PaddingHorizontal(5)
                                .Text(dossier.StatutDossier.ToUpper()).FontColor(Colors.White).Bold();
                        });

                        col.Item().PaddingTop(15).Background(Colors.Grey.Lighten4).Padding(15).Column(inner =>
                        {
                            inner.Item().Text("RESTE À PAYER").FontSize(10).Bold();
                            inner.Item().Text($"{dossier.MontantImpaye} TND").FontSize(26).Bold().FontColor(colorHex);
                        });
                    });

                    page.Footer().AlignCenter().Text(x => {
                        x.Span("Document généré automatiquement - Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            byte[] pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Recu_{client.Nom}.pdf");
        }

        // ==============================
        // ✅ NOUVEAU : POST api/client/upload/{token}
        // Upload justificatif de paiement
        // ==============================
        [HttpPost("upload/{token}")]
        public async Task<IActionResult> UploadJustificatif(string token, IFormFile fichier)
        {
            // Vérifier le token
            var client = await _context.Clients
                .Include(c => c.Dossiers)
                .FirstOrDefaultAsync(c => c.TokenAcces == token);

            if (client == null)
                return Unauthorized("Token invalide");

            // Vérifier fichier
            if (fichier == null || fichier.Length == 0)
                return BadRequest("Aucun fichier envoyé");

            // Vérifier type fichier
            var extensionsAutorisees = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(fichier.FileName).ToLower();
            if (!extensionsAutorisees.Contains(extension))
                return BadRequest("Format non autorisé. Utilisez PDF, JPG ou PNG.");

            // Vérifier taille max 5MB
            if (fichier.Length > 5 * 1024 * 1024)
                return BadRequest("Fichier trop volumineux. Maximum 5 MB.");

            // Récupérer dossier
            var dossier = client.Dossiers
                .OrderByDescending(d => d.DateCreation)
                .FirstOrDefault();

            if (dossier == null)
                return NotFound("Aucun dossier trouvé");

            // Créer dossier uploads
            var uploadsPath = Path.Combine(_env.ContentRootPath, "uploads", dossier.IdDossier.ToString());
            Directory.CreateDirectory(uploadsPath);

            // Nom unique du fichier
            var nomFichier = $"{DateTime.Now:yyyyMMddHHmmss}_{client.Nom}{extension}";
            var cheminComplet = Path.Combine(uploadsPath, nomFichier);

            // Sauvegarder fichier
            using (var stream = new FileStream(cheminComplet, FileMode.Create))
            {
                await fichier.CopyToAsync(stream);
            }

            // Historique action
            _context.HistoriqueActions.Add(new HistoriqueAction
            {
                IdDossier = dossier.IdDossier,
                ActionDetail = $"Client a uploadé un justificatif : {nomFichier}",
                Acteur = "client",
                DateAction = DateTime.Now
            });

            // Communication vers agent
            _context.Communications.Add(new Communication
            {
                IdDossier = dossier.IdDossier,
                Message = $"Le client a envoyé un justificatif : {nomFichier}",
                Origine = "client",
                DateEnvoi = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Fichier uploadé avec succès",
                nomFichier = nomFichier
            });
        } // Fin de la méthode UploadJustificatif
    }     // Fin de la classe ClientController
}         // Fin du namespace