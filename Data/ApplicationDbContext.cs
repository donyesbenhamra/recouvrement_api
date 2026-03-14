using Microsoft.EntityFrameworkCore;
using RecouvrementAPI.Models;

namespace RecouvrementAPI.Data
{
    // ApplicationDbContext est le pont entre l’API .NET et la base MySQL
    public class ApplicationDbContext : DbContext
    {
        // Constructeur appelé par ASP.NET Core (Injection de dépendance)
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================
        // TABLES DE LA BASE
        // =========================

        // Table agence
        public DbSet<Agence> Agences { get; set; }

        // Table client
        public DbSet<Client> Clients { get; set; }

        // Table agent (back-office)
        public DbSet<UtilisateurBack> UtilisateursBack { get; set; }

        // Table dossier_recouvrement
        public DbSet<DossierRecouvrement> Dossiers { get; set; }

        // Table echeance
        public DbSet<Echeance> Echeances { get; set; }

        // Table historique_paiement
        public DbSet<HistoriquePaiement> HistoriquePaiements { get; set; }

        // Table intention_client
        public DbSet<IntentionClient> Intentions { get; set; }

        // Table relance_client
        public DbSet<RelanceClient> Relances { get; set; }

        // Table garantie
        public DbSet<Garantie> Garanties { get; set; }

        // Table communication
        public DbSet<Communication> Communications { get; set; }

        // Table score_risque
        public DbSet<ScoreRisque> ScoresRisque { get; set; }

        // Table historique_action
        public DbSet<HistoriqueAction> HistoriqueActions { get; set; }

        // =========================
        // CONFIGURATION DES RELATIONS
        // =========================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Client → Agence (Plusieurs clients pour une agence)
            modelBuilder.Entity<Client>()
                .HasOne(c => c.Agence)
                .WithMany(a => a.Clients)
                .HasForeignKey(c => c.IdAgence);

            // Dossier → Client (Un client peut avoir plusieurs dossiers)
            modelBuilder.Entity<DossierRecouvrement>()
                .HasOne(d => d.Client)
                .WithMany(c => c.Dossiers)
                .HasForeignKey(d => d.IdClient);

            // Echeance → Dossier (Un dossier peut avoir plusieurs échéances)
            modelBuilder.Entity<Echeance>()
                .HasOne(e => e.Dossier)
                .WithMany(d => d.Echeances)
                .HasForeignKey(e => e.IdDossier);

            // HistoriquePaiement → Dossier
            modelBuilder.Entity<HistoriquePaiement>()
                .HasOne(p => p.Dossier)
                .WithMany(d => d.HistoriquePaiements)
                .HasForeignKey(p => p.IdDossier);

            // IntentionClient → Dossier
            modelBuilder.Entity<IntentionClient>()
                .HasOne(i => i.Dossier)
                .WithMany(d => d.Intentions)
                .HasForeignKey(i => i.IdDossier);

            // RelanceClient → Dossier
            modelBuilder.Entity<RelanceClient>()
                .HasOne(r => r.Dossier)
                .WithMany(d => d.Relances)
                .HasForeignKey(r => r.IdDossier);

            // Communication → Dossier
            modelBuilder.Entity<Communication>()
                .HasOne(c => c.Dossier)
                .WithMany(d => d.Communications)
                .HasForeignKey(c => c.IdDossier);

            // Garantie → Dossier
            modelBuilder.Entity<Garantie>()
                .HasOne(g => g.Dossier)
                .WithMany(d => d.Garanties)
                .HasForeignKey(g => g.IdDossier);

            // ScoreRisque → Dossier
            modelBuilder.Entity<ScoreRisque>()
                .HasOne(s => s.Dossier)
                .WithMany(d => d.ScoresRisque)
                .HasForeignKey(s => s.IdDossier);

            // HistoriqueAction → Dossier
            modelBuilder.Entity<HistoriqueAction>()
                .HasOne(h => h.Dossier)
                .WithMany(d => d.HistoriqueActions)
                .HasForeignKey(h => h.IdDossier);

            base.OnModelCreating(modelBuilder);
        }
    }
}