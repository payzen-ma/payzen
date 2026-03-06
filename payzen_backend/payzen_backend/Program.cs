using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using payzen_backend.Data;
using payzen_backend.Seeding;
using payzen_backend.Services;
using payzen_backend.Services.Company;
using payzen_backend.Services.Company.Defaults;
using payzen_backend.Services.Company.Defaults.Seeders;
using payzen_backend.Services.Company.Interfaces;
using payzen_backend.Services.Company.Onboarding;
using payzen_backend.Services.Convergence;
using payzen_backend.Services.Dashboard;
using payzen_backend.Services.Leave;
using payzen_backend.Services.Llm;
using payzen_backend.Services.Payroll;
using payzen_backend.Services.Validation;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuration explicite pour accepter JSON
// S'assurer que l'API accepte et retourne du JSON correctement
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        // Accept string enum values in JSON (e.g. "CAPPED", "PER_MONTH") for payroll referentiel DTOs
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});


// Configuration de la base de données
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(conn));

// Configuration JWT
var jwtKey = builder.Configuration["JwtSettings:Key"] 
    ?? throw new InvalidOperationException("JWT Key not found in configuration");
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            
            NameClaimType = "unique_name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<PasswordGeneratorService>();
builder.Services.AddScoped<EmployeeEventLogService>();
builder.Services.AddScoped<CompanyEventLogService>();
builder.Services.AddScoped<LeaveEventLogService>();
builder.Services.AddScoped<WorkingDaysCalculator>();
builder.Services.AddScoped<LeaveBalanceService>();
builder.Services.AddScoped<IMoroccanPayrollService, MoroccanPayrollService>();
builder.Services.AddScoped<EmployeePayrollDataService>();
builder.Services.AddScoped<ICompanyDocumentService, CompanyDocumentService>();

// Service LLM : Mock / Claude / Gemini
var useMock = builder.Configuration.GetValue<bool>("Anthropic:UseMock");
var useGemini = builder.Configuration.GetValue<bool>("Google:UseGemini");

if (useMock)
{
    builder.Services.AddScoped<IClaudeService, MockClaudeService>();
    builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Information);
}
else if (useGemini)
{
    builder.Services.AddHttpClient(); // Nécessaire pour GeminiService
    builder.Services.AddScoped<IClaudeService, GeminiService>();
}
else
{
    builder.Services.AddScoped<IClaudeService, ClaudeService>();
}

// Service de simulation Claude pour la paie
builder.Services.AddScoped<IClaudeSimulationService, ClaudeSimulationService>();

builder.Services.AddScoped<PaieService>();
builder.Services.AddScoped<PayrollCalculationEngine>();

// Company onboarding : seed par défaut (contrats, départements, postes, calendrier, congés)
builder.Services.AddScoped<ContractTypeSeeder>();
builder.Services.AddScoped<DepartmentSeeder>();
builder.Services.AddScoped<JobPositionSeeder>();
builder.Services.AddScoped<EmployeeCategorySeeder>();
builder.Services.AddScoped<WorkingCalendarSeeder>();
builder.Services.AddScoped<LeaveSeeder>();
builder.Services.AddScoped<ICompanyDefaultsSeeder, CompanyDefaultsSeeder>();
builder.Services.AddScoped<ICompanyOnboardingService, CompanyOnboardingService>();

builder.Services.AddScoped<IElementRuleResolutionService, ElementRuleResolutionService>();
builder.Services.AddScoped<payzen_backend.Services.Payroll.IPayrollExportService,
                            payzen_backend.Services.Payroll.PayrollExportService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDashboardHrService, DashboardHrService>();

// Payroll Referential Refactoring Services
builder.Services.AddScoped<IConvergenceAnalysisService, ConvergenceAnalysisService>();
builder.Services.AddScoped<IReferentialValidationService, ReferentialValidationService>();

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:50171", "http://localhost:4200", "https://localhost:4200", "http://localhost:55879")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ── IronPDF : initialisation de la licence (une seule fois au démarrage) ──
IronPdf.License.LicenseKey = app.Configuration["IronPdf.LicenseKey"] ?? "";

// Afficher le mode LLM utilisé
var useMockClaude = app.Configuration.GetValue<bool>("Anthropic:UseMock");
var useGeminiMode = app.Configuration.GetValue<bool>("Google:UseGemini");
var logger = app.Services.GetRequiredService<ILogger<Program>>();

if (useMockClaude)
{
    logger.LogWarning("🧪 MODE MOCK ACTIVÉ - Aucune consommation de tokens");
    logger.LogWarning("⚠️  Les calculs sont simplifiés. Pour la production, désactiver le mock.");
}
else if (useGeminiMode)
{
    logger.LogInformation("🌟 MODE GEMINI ACTIVÉ - Google Gemini 1.5 Flash");
    logger.LogInformation("💚 100% GRATUIT - 15 req/min, 1500 req/jour, 1M tokens/jour");
}
else
{
    logger.LogInformation("🚀 MODE CLAUDE ACTIVÉ - Anthropic Claude Sonnet 4.5");
    logger.LogInformation("💰 Attention : consommation de tokens à chaque calcul");
}

// === Seed de la base de données idempotent ===
using (var scope = app.Services.CreateScope())
{
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        seedLogger.LogInformation("Démarrage du seed...");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DbSeeder.SeedAsync(db);
        seedLogger.LogInformation("Seed terminé avec succès.");
    }
    catch (Exception ex)
    {
        seedLogger.LogError(ex, "Erreur lors du seed de la base de données. Arrêt du démarrage.");
        throw; // remonter l'erreur pour empêcher un démarrage incomplet
    }
}

// CORS must be called before Authentication/Authorization
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
