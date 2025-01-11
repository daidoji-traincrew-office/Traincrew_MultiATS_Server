using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenIddict.Abstractions;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.InterlockingObject;
using Traincrew_MultiATS_Server.Repositories.LockCondition;
using Traincrew_MultiATS_Server.Repositories.Discord;
using Traincrew_MultiATS_Server.Repositories.Station;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
    dataSourceBuilder.MapEnum<LockType>();
    dataSourceBuilder.MapEnum<NR>();
    dataSourceBuilder.MapEnum<NRC>();
    options.UseNpgsql(dataSourceBuilder.Build());
    // Todo: セッションであることを考えると、Redisを使ったほうが良いかも
    options.UseOpenIddict();
});
// SignalR
builder.Services.AddSignalR();

// 認証周り
builder.Services.AddOpenIddict()
    // Register the OpenIddict core components.
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    // Register the OpenIddict client components.
    .AddClient(options =>
    {
        // Enable the client credentials flow. 
        options.AllowAuthorizationCodeFlow();

        // Register the signing and encryption credentials used to protect
        // sensitive data like the state tokens produced by OpenIddict.
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();
        }
        else
        {
            // Todo: 関数化する
            const string encryptionCertificatePath = "cert/client-encryption-certificate.pfx";
            const string signingCertificatePath = "cert/client-signing-certificate.pfx";
            // Generate a certificate at startup and register it.
            if (!File.Exists(encryptionCertificatePath))
            {
                using var algorithm = RSA.Create(2048);

                var subject = new X500DistinguishedName("CN=Fabrikam Client Encryption Certificate");
                var request = new CertificateRequest(subject, algorithm, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment,
                    true));

                var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));

                File.WriteAllBytes(encryptionCertificatePath,
                    certificate.Export(X509ContentType.Pfx, string.Empty));
            }

            if (!File.Exists(signingCertificatePath))
            {
                using var algorithm = RSA.Create(2048);

                var subject = new X500DistinguishedName("CN=Fabrikam Client Signing Certificate");
                var request = new CertificateRequest(subject, algorithm, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature,
                    true));

                var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));

                File.WriteAllBytes(signingCertificatePath,
                    certificate.Export(X509ContentType.Pfx, string.Empty));
            }

            options.AddEncryptionCertificate(
                new X509Certificate2(encryptionCertificatePath, string.Empty));
            options.AddSigningCertificate(
                new X509Certificate2(signingCertificatePath, string.Empty));
        }

        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
        options.UseAspNetCore()
            .EnableRedirectionEndpointPassthrough();

        // Register the System.Net.Http integration and use the identity of the current
        // assembly as a more specific user agent, which can be useful when dealing with
        // providers that use the user agent as a way to throttle requests (e.g Reddit).
        options.UseSystemNetHttp()
            .SetProductInformation(typeof(Program).Assembly);

        // Register the Web providers integrations.
        options.UseWebProviders()
            .AddDiscord(discord =>
            {
                discord.AddScopes("identify", "guilds", "guilds.members.read");
                discord
                    .SetClientId(builder.Configuration["Discord:ClientId"] ?? "")
                    .SetClientSecret(builder.Configuration["Discord:ClientSecret"] ?? "")
                    .SetRedirectUri("auth/callback");
            });
    })
    // Register the OpenIddict server components.
    .AddServer(options =>
    {
        // Authorizationとtokenエンドポイントを有効にする
        options.SetAuthorizationEndpointUris("auth/authorize")
            .SetTokenEndpointUris("token");

        // AuthorizationCodeFlowとRefreshTokenFlowを有効にする
        options.AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow();

        // Register the signing and encryption credentials
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();
        }
        else
        {
            // Todo: 関数化する
            const string encryptionCertificatePath = "cert/server-encryption-certificate.pfx";
            const string signingCertificatePath = "cert/server-signing-certificate.pfx";
            // Generate a certificate at startup and register it.
            if (!File.Exists(encryptionCertificatePath))
            {
                using var algorithm = RSA.Create(2048);

                var subject = new X500DistinguishedName("CN=Fabrikam Server Encryption Certificate");
                var request = new CertificateRequest(subject, algorithm, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment,
                    true));

                var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));

                File.WriteAllBytes(encryptionCertificatePath,
                    certificate.Export(X509ContentType.Pfx, string.Empty));
            }

            if (!File.Exists(signingCertificatePath))
            {
                using var algorithm = RSA.Create(2048);

                var subject = new X500DistinguishedName("CN=Fabrikam Server Signing Certificate");
                var request = new CertificateRequest(subject, algorithm, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature,
                    true));

                var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));

                File.WriteAllBytes(signingCertificatePath,
                    certificate.Export(X509ContentType.Pfx, string.Empty));
            }

            options.AddEncryptionCertificate(
                new X509Certificate2(encryptionCertificatePath, string.Empty));
            options.AddSigningCertificate(
                new X509Certificate2(signingCertificatePath, string.Empty));
        }

        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
        //
        // Note: unlike other samples, this sample doesn't use token endpoint pass-through
        // to handle token requests in a custom MVC action. As such, the token requests
        // will be automatically handled by OpenIddict, that will reuse the identity
        // resolved from the authorization code to produce access and identity tokens.
        //
        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough();
    })
    // Register the OpenIddict validation components.
    .AddValidation(options =>
    {
        // Import the configuration from the local OpenIddict server instance.
        options.UseLocalServer();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });
builder.Services.AddAuthorization()
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });
// DI周り
builder.Services
    .AddScoped<IStationRepository, StationRepository>()
    .AddScoped<IInterlockingObjectRepository, InterlockingObjectRepository>()
    .AddScoped<ILockConditionRepository, LockConditionRepository>()
    .AddScoped<StationService>()
    .AddSingleton<DiscordService>()
    .AddSingleton<IDiscordRepository, DiscordRepository>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

// Create a new application registration matching the values configured in MultiATS_Client.
// Note: in a real world application, this step should be part of a setup script.
await using (var scope = app.Services.CreateAsyncScope())
{
    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    if (await manager.FindByClientIdAsync("MultiATS_Client") == null)
    {
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ApplicationType = ApplicationTypes.Native,
            ClientId = "MultiATS_Client",
            ClientType = ClientTypes.Public,
            RedirectUris =
            {
                new Uri("http://localhost:49152/")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code
            }
        });
    }
}

app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<TrainHub>("/hub/train");

app.Run();