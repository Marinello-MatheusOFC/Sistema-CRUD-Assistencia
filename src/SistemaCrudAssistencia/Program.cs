using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCrudAssistencia.Data;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Services;
using SistemaCrudAssistencia.Services.Endereco;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------------
// Configuração de ambiente
// ------------------------------------------------------------------
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var ptBr = CultureInfo.GetCultureInfo("pt-BR");

    options.DefaultRequestCulture = new RequestCulture(ptBr);
    options.SupportedCultures = [ptBr];
    options.SupportedUICultures = [ptBr];
});

// ------------------------------------------------------------------
// Connection string
// ------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string não configurada. Defina a variável de ambiente " +
        "'ConnectionStrings__Default' (veja o README e o arquivo .env.example).");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName()!.Name);
        npgsql.MigrationsHistoryTable("__migrations_historico");
    }));

// ------------------------------------------------------------------
// ASP.NET Core Identity (autenticação com cookie + hash de senha)
// ------------------------------------------------------------------
builder.Services
    .AddIdentity<Usuario, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;

        options.Lockout.MaxFailedAccessAttempts = 10;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;

        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AcessoNegado";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// Política padrão: tudo exige usuário autenticado.
// Só Login, Logout e Error ficam acessíveis anonimamente.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// ------------------------------------------------------------------
// MVC
// ------------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddHttpClient();

builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IAparelhoService, AparelhoService>();
builder.Services.AddScoped<IOrdemServicoService, OrdemServicoService>();
builder.Services.AddScoped<IBuscaService, BuscaService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddHttpClient<ICepService, ViaCepService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});

var app = builder.Build();

// ------------------------------------------------------------------
// Pipeline HTTP
// ------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Account/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Account/HttpStatus", "?codigo={0}");
app.UseRequestLocalization();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ------------------------------------------------------------------
// Migrations e seed
// ------------------------------------------------------------------
await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var ambiente = app.Environment.EnvironmentName;

    try
    {
        var contexto = services.GetRequiredService<AppDbContext>();

        if (app.Configuration.GetValue("Aplicacao:AplicarMigrationsAutomaticamente", ambiente != "Production"))
        {
            logger.LogInformation("Aplicando migrations do banco de dados...");
            await contexto.Database.MigrateAsync();
        }
        else
        {
            logger.LogInformation("AplicarMigrationsAutomaticamente=false — migrations não aplicadas.");
        }

        await SeedExecutor.ExecutarAsync(services, app.Configuration, ambiente);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Falha ao inicializar o banco de dados. Verifique a connection string e as migrations.");
        throw;
    }
}

app.Run();

public partial class Program;
