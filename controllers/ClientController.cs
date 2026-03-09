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
    // Contrôleur qui gère tout ce que le CLIENT peut faire
    // Route de base : http://localhost:5203/api/client
    [ApiController]
    [Route("api/client")]
    public class ClientController : ControllerBase
    {
        // _context : accès à la base de données MySQL
        private readonly ApplicationDbContext _context;

        // _env : accès au système de fichiers du serveur (upload)
        private readonly IWebHostEnvironment _env;

        // Constructeur : .NET injecte automatiquement les dépendances
        public ClientController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ==============================
        // MÉTHODE PRIVÉE : Vérifier token
        // Charge le client avec son agence et TOUS ses dossiers.
        // Retourne null si token introuvable → les endpoints retournent 401.
        // ==============================
        private async Task<Client> VerifierToken(string token)
        {
            return await _context.Clients
                .Include(c => c.Agence)
                .Include(c => c.Dossiers)
                .FirstOrDefaultAsync(c => c.TokenAcces == token);
        }

        // ==============================
        // MÉTHODE PRIVÉE : Résoudre le dossier cible
        //
        // Logique de sélection du dossier (comportement par défaut) :
        //   • Si idDossier est fourni  → cherche ce dossier précis parmi ceux du client
        //                                (retourne null si non trouvé ou n'appartient pas au client)
        //   • Si idDossier est null    → prend automatiquement le dossier le plus récent
        //                                (OrderByDescending sur DateCreation)
        //
        // Ce comportement par défaut garantit qu'un client qui ne sélectionne
        // rien obtient toujours son dossier actif le plus récent.
        // ==============================
        private DossierRecouvrement ResoudreDossier(Client client, int? idDossier)
        {
            if (idDossier.HasValue)
            {
                // Recherche du dossier spécifié ET appartenant bien à ce client
                // (protection contre la manipulation d'ID par un autre client)
                return client.Dossiers.FirstOrDefault(d => d.IdDossier == idDossier.Value);
            }

            // Comportement par défaut : dossier le plus récent
            return client.Dossiers
                .OrderByDescending(d => d.DateCreation)
                .FirstOrDefault();
        }

        // ==============================
        // MÉTHODE PRIVÉE : Vérifier retard > 3 mois
        // Déclenche une communication automatique si retard > 90 jours.
        // Appelée pour chaque dossier dans GetHistorique().
        // ==============================
        private async Task VerifierRetard3Mois(DossierRecouvrement dossier)
        {
            // Cherche la première échéance impayée dépassée
            var premiereEcheance = dossier.Echeances
                .Where(e => e.Statut == "impaye" && e.DateEcheance < DateTime.Now)
                .OrderBy(e => e.DateEcheance)
                .FirstOrDefault();

            // Aucune échéance impayée → rien à faire
            if (premiereEcheance == null) return;

            int joursRetard = (int)(DateTime.Now - premiereEcheance.DateEcheance).TotalDays;

            if (joursRetard > 90)
            {
                // Anti-doublon : pas de communication si une a déjà été envoyée ce mois
                bool dejaEnvoyee = await _context.Communications
                    .AnyAsync(c =>
                        c.IdDossier == dossier.IdDossier &&
                        c.Origine == "systeme" &&
                        c.DateEnvoi >= DateTime.Now.AddMonths(-1));

                if (!dejaEnvoyee)
                {
                    _context.Communications.Add(new Communication
                    {
                        IdDossier = dossier.IdDossier,
                        Message = $"Alerte automatique : retard de {joursRetard} jours " +
                                  $"détecté sur votre dossier. " +
                                  $"Montant impayé : {dossier.MontantImpaye} TND. " +
                                  $"Veuillez régulariser votre situation.",
                        Origine = "systeme",
                        DateEnvoi = DateTime.Now
                    });

                    _context.HistoriqueActions.Add(new HistoriqueAction
                    {
                        IdDossier = dossier.IdDossier,
                        ActionDetail = $"Communication auto déclenchée — retard > 3 mois ({joursRetard} jours)",
                        Acteur = "systeme",
                        DateAction = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                }
            }
        }

        // ==============================
        // MÉTHODE PRIVÉE : Calculer jours de retard
        // Factorisée pour éviter la duplication entre GetHistorique() et GenerateRecu().
        // Retourne 0 si aucune échéance impayée dépassée.
        // ==============================
        private int CalculerJoursRetard(DossierRecouvrement dossier)
        {
            var echeancesImpayeesDepassees = dossier.Echeances
                .Where(e => e.Statut == "impaye" && e.DateEcheance < DateTime.Now);

            if (!echeancesImpayeesDepassees.Any()) return 0;

            return (int)(DateTime.Now - echeancesImpayeesDepassees.Min(e => e.DateEcheance)).TotalDays;
        }

        // ==============================
        // MÉTHODE PRIVÉE : Mapper DossierRecouvrement → DossierDto
        // Factorisée pour éviter la duplication entre GetHistorique() et tout futur endpoint.
        // ==============================
        private DossierDto MapDossierToDto(DossierRecouvrement dossier)
        {
            int joursRetard = CalculerJoursRetard(dossier);

            return new DossierDto
            {
                IdDossier      = dossier.IdDossier,
                TypeEmprunt    = dossier.TypeEmprunt,
                MontantImpaye  = dossier.MontantImpaye,
                MontantInitial = dossier.MontantInitial,
                MontantPaye    = dossier.MontantInitial - dossier.MontantImpaye,
                FraisDossier   = dossier.FraisDossier,
                StatutDossier  = dossier.StatutDossier,
                TauxInteret    = dossier.TauxInteret,

                // Intérêts : 0 si retard ≤ 90 jours, calculés sinon
                MontantInterets = joursRetard > 90
                    ? dossier.MontantImpaye * (dossier.TauxInteret / 100) * (decimal)joursRetard / 365
                    : 0,

                NombreJoursRetard = joursRetard,

                // Prochaine échéance (la plus proche dans le temps)
                DateEcheance = dossier.Echeances
                    .OrderBy(e => e.DateEcheance)
                    .Select(e => e.DateEcheance)
                    .FirstOrDefault(),

                Garanties = dossier.Garanties.Select(g => new GarantieDto
                {
                    TypeGarantie = g.TypeGarantie,
                    Description  = g.Description
                }).ToList(),

                Echeances = dossier.Echeances.Select(e => new EcheanceDto
                {
                    Montant      = e.Montant,
                    DateEcheance = e.DateEcheance,
                    Statut       = e.Statut
                }).ToList(),

                Paiements = dossier.HistoriquePaiements.Select(p => new HistoriquePaiementDto
                {
                    MontantPaye  = p.MontantPaye,
                    TypePaiement = p.TypePaiement,
                    DatePaiement = p.DatePaiement
                }).ToList(),

                Relances = dossier.Relances.Select(r => new RelanceDto
                {
                    DateRelance = r.DateRelance,
                    Moyen       = r.Moyen,
                    Statut      = r.Statut
                }).ToList(),

                Communications = dossier.Communications.Select(c => new CommunicationDto
                {
                    Message   = c.Message,
                    Origine   = c.Origine,
                    DateEnvoi = c.DateEnvoi
                }).ToList()
            };
        }

        // ==============================
        // GET api/client/historique/{token}
        //
        // Retourne TOUS les dossiers du client en JSON.
        // Appelé par Angular pour afficher la liste des dossiers
        // et laisser le client en choisir un.
        // ==============================
       [HttpGet("historique/{token}")]
        public async Task<IActionResult> GetHistorique(string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Token requis");

            // Chargement eager : toutes les relations en une seule requête SQL
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
                .Include(c => c.Dossiers)
                    .ThenInclude(d => d.Garanties)
                .FirstOrDefaultAsync(c => c.TokenAcces == token);

            if (client == null)
                return Unauthorized("Token invalide");

            // Journalisation de l'accès (dossier le plus récent comme référence de log)
            var dossierPrincipal = client.Dossiers
                .OrderByDescending(d => d.DateCreation)
                .FirstOrDefault();

            if (dossierPrincipal != null)
            {
                _context.HistoriqueActions.Add(new HistoriqueAction
                {
                    IdDossier = dossierPrincipal.IdDossier,
                    ActionDetail = $"Accès client via token UUID — IP : {HttpContext.Connection.RemoteIpAddress}",
                    Acteur = "client",
                    DateAction = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            // Vérification du retard > 3 mois pour chaque dossier
            foreach (var dossier in client.Dossiers)
            {
                await VerifierRetard3Mois(dossier);
            }

            // Construction du DTO — contient TOUS les dossiers du client
            // Angular pourra afficher la liste et laisser le client en choisir un
            var dto = new ClientHistoriqueDto
            {
                NomComplet = client.Nom + " " + client.Prenom,
                IdAgence   = client.Agence != null ? client.Agence.IdAgence : 0,
                VilleAgence = client.Agence?.Ville,

                // Tous les dossiers, du plus récent au plus ancien
                Dossiers = client.Dossiers
                    .OrderByDescending(d => d.DateCreation)
                    .Select(dossier => MapDossierToDto(dossier))
                    .ToList()
            };

            return Ok(dto);
        }
// ==============================
        // GET api/client/recu/{token}
        // GET api/client/recu/{token}?idDossier=42   ← dossier spécifique
        //
        // Génère et télécharge un PDF du reçu de situation.
        //   • Sans idDossier → PDF du dossier le plus récent (comportement par défaut)
        //   • Avec idDossier → PDF du dossier choisi par le client
        // ============================== 
    [HttpGet("recu/{token}")]
public async Task<IActionResult> GenerateRecu(string token, [FromQuery] int? idDossier = null)
{
    // 1. Sécurité : Vérification de l'identité du client via le Token unique (UUID)
    var client = await VerifierToken(token);
    if (client == null)
        return Unauthorized("Token invalide");

    // 2. Sélection du dossier : Soit l'ID fourni, soit le dossier le plus récent par défaut
    var dossier = ResoudreDossier(client, idDossier);
    if (dossier == null) return NotFound("Dossier introuvable");

    // 3. Chargement des données : On inclut les échéances pour calculer le retard
    dossier = await _context.Dossiers
        .Include(d => d.Echeances)
        .FirstOrDefaultAsync(d => d.IdDossier == dossier.IdDossier);

    // 4. Logique métier : Calcul du nombre de jours de retard cumulés
    int joursRetard = CalculerJoursRetard(dossier);
    
    // --- CALCUL DU MONTANT PAYÉ ---
    // Différence entre ce qui était prévu au départ et ce qui reste à payer
    decimal montantPaye = dossier.MontantInitial - dossier.MontantImpaye;
    
    // 5. Calcul des intérêts de retard (Règle des 90 jours)
    decimal montantInterets = joursRetard > 90
        ? dossier.MontantImpaye * (dossier.TauxInteret / 100) * ((decimal)joursRetard / 365)
        : 0;

    // 6. Calcul du Total (Principal restant + Intérêts)
    decimal totalARegler = dossier.MontantImpaye + montantInterets;

    // 7. Identité visuelle selon le statut
    string colorHex = dossier.StatutDossier == "regularise" ? Colors.Green.Medium :
                     (dossier.StatutDossier == "contentieux" ? Colors.Red.Medium : Colors.Blue.Medium);

    // 8. Génération du document PDF
    var document = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Margin(50);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(x => x.FontSize(11));

            // EN-TÊTE CORRIGÉ : Affichage STB BANK + Ville
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("REÇU DE SITUATION").FontSize(22).SemiBold().FontColor(Colors.Blue.Medium);
                    col.Item().Text($"Dossier n° {dossier.IdDossier}").FontSize(10);
                });
                
                // Ici on force "STB BANK" suivi de la ville de l'agence
                row.RelativeItem().AlignRight().Text($"STB BANK - {client.Agence?.Ville}").Bold();
            });

            page.Content().PaddingVertical(25).Column(col =>
            {
                col.Spacing(10);
                col.Item().Text($"Client : {client.Nom} {client.Prenom}").Bold();
                
                col.Item().Text(text => {
                    text.Span("Retard constaté : ").Bold();
                    text.Span($"{joursRetard} jours").FontColor(joursRetard > 0 ? Colors.Red.Medium : Colors.Green.Medium).Bold();
                });

                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                col.Item().Text($"Type de crédit : {dossier.TypeEmprunt}");
                col.Item().Text($"Montant initial : {dossier.MontantInitial:F3} TND");
                
                // --- AFFICHAGE DU MONTANT DÉJÀ PAYÉ ---
                col.Item().Text(text => {
                    text.Span("Montant déjà payé : ");
                    text.Span($"{montantPaye:F3} TND").FontColor(Colors.Green.Medium).SemiBold();
                });

                col.Item().Text($"Principal restant : {dossier.MontantImpaye:F3} TND");
                
                if (montantInterets > 0)
                {
                    col.Item().Text(text => {
                        text.Span($"Intérêts de retard ({dossier.TauxInteret}%) : ").Bold();
                        text.Span($"{montantInterets:F3} TND").FontColor(Colors.Red.Medium);
                    });
                }

                col.Item().Text($"Frais de dossier : {dossier.FraisDossier:F3} TND");

                // BLOC RÉCAPITULATIF FINAL
                col.Item().PaddingTop(15).Background(Colors.Grey.Lighten4).Padding(15).Column(inner =>
                {
                    inner.Item().Text("Montant à apyer").FontSize(11).Bold();
                    inner.Item().Text($"{totalARegler:F3} TND")
                        .FontSize(28).Bold().FontColor(colorHex);
                    
                });
            });

            page.Footer().AlignCenter().Text($"Document généré le {DateTime.Now:dd/MM/yyyy HH:mm}");
        });
    });

    byte[] pdfBytes = document.GeneratePdf();
    return File(pdfBytes, "application/pdf", $"Recu_STB_Dossier_{dossier.IdDossier}.pdf");
}
            // Nom du fichier inclut l'idDossier pour distinguer les reçus d'un même client
            
       

        // ==============================
        // POST api/client/upload/{token}
        // POST api/client/upload/{token}?idDossier=42   ← dossier spécifique
        //
        // Upload justificatif de paiement (PDF/JPG/PNG max 5MB).
        //   • Sans idDossier → upload attaché au dossier le plus récent (comportement par défaut)
        //   • Avec idDossier → upload attaché au dossier choisi par le client
        // ==============================
        [HttpPost("upload/{token}")]
        public async Task<IActionResult> UploadJustificatif(
            string token,
            IFormFile File,
            [FromQuery] int? idDossier = null)
        {
            var client = await VerifierToken(token);
            if (client == null)
                return Unauthorized("Token invalide");

            if (File == null || File.Length == 0)
                return BadRequest("Aucun fichier envoyé");

            // Whitelist d'extensions autorisées (insensible à la casse)
            var extensionsAutorisees = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(File.FileName).ToLower();
            if (!extensionsAutorisees.Contains(extension))
                return BadRequest("Format non autorisé. Utilisez PDF, JPG ou PNG.");

            if (File.Length > 5 * 1024 * 1024)
                return BadRequest("Fichier trop volumineux. Maximum 5 MB.");

            // Résolution du dossier cible (défaut = le plus récent)
            var dossier = ResoudreDossier(client, idDossier);
            if (dossier == null)
                return NotFound(idDossier.HasValue
                    ? $"Dossier {idDossier} introuvable ou n'appartient pas à ce client."
                    : "Aucun dossier trouvé.");

            // Stockage dans un sous-dossier propre à chaque dossier de recouvrement
            var uploadsPath = Path.Combine(
                _env.ContentRootPath, "uploads", dossier.IdDossier.ToString());
            Directory.CreateDirectory(uploadsPath);

            // Nom unique : horodatage + nom client → évite toute collision de fichier
            var nomFichier = $"{DateTime.Now:yyyyMMddHHmmss}_{client.Nom}{extension}";
            var cheminComplet = Path.Combine(uploadsPath, nomFichier);

            using (var stream = new FileStream(cheminComplet, FileMode.Create))
            {
                await File.CopyToAsync(stream);
            }

            _context.HistoriqueActions.Add(new HistoriqueAction
            {
                IdDossier    = dossier.IdDossier,
                ActionDetail = $"Client a uploadé un justificatif : {nomFichier}",
                Acteur       = "client",
                DateAction   = DateTime.Now
            });

            // Communication automatique vers l'agent pour l'informer du nouveau justificatif
            _context.Communications.Add(new Communication
            {
                IdDossier = dossier.IdDossier,
                Message   = $"Le client a envoyé un justificatif : {nomFichier}",
                Origine   = "client",
                DateEnvoi = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Fichier uploadé avec succès",
                nomFichier = nomFichier,
                // Retourne l'idDossier effectivement utilisé pour qu'Angular
                // sache à quel dossier l'upload a été rattaché (utile si idDossier était null)
                idDossierUtilise = dossier.IdDossier
            });
        }
    }
}