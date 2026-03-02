using Microsoft.EntityFrameworkCore;
using RecouvrementAPI.Data;
using QuestPDF.Infrastructure; 

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURATION QUESTPDF ---
// Définit la licence en mode communautaire pour autoriser la génération de PDF
QuestPDF.Settings.License = LicenseType.Community;

// --- SERVICES CONTAINER (Injection de Dépendances) ---

// Récupération de la "Connection String" depuis le fichier de configuration appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Définition explicite de la version du serveur MySQL. 
var serverVersion = new MySqlServerVersion(new Version(8, 0, 31)); 

// Injection du DbContext dans le conteneur de services. 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

// Enregistrement des services nécessaires pour supporter les contrôleurs API
builder.Services.AddControllers();

var app = builder.Build();

// --- HTTP REQUEST PIPELINE (Configuration des Middlewares) ---

app.UseAuthorization();

// Point d'entrée racine pour le diagnostic.
app.MapGet("/", () => "L'API est en ligne sur .NET 10 !");

// Analyse les attributs [Route] (ex: api/client, api/agence).
app.MapControllers();

// Lance l'écoute des requêtes HTTP entrantes.
app.Run();