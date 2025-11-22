using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using WebAuthen.Data;
using WebAuthen.Service;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using RepoDb;
using WebAuthen.App_code;
using Microsoft.Extensions.FileProviders;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;


var builder = WebApplication.CreateBuilder(args);

GlobalConfiguration.Setup().UseSqlServer();

builder.Services.AddScoped<AuthClass>();
builder.Services.AddScoped<WorkiwiseClass>();
builder.Services.AddScoped<DynamicConnectionService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<HandleApiReturn>();
builder.Services.AddScoped<CompanyClass>();

builder.Services.AddControllers();
var env = builder.Environment;

// using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole());
// var logger = loggerFactory.CreateLogger<Program>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocal3000", p => p
        .WithOrigins("http://localhost:3000")
        .AllowAnyMethod()
        .AllowAnyHeader()
    // ถ้าใช้ cookie/withCredentials ให้เปิดบรรทัดนี้
    //.AllowCredentials()
    );
});


builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Connection45"));

    options.UseOpenIddict();
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

var parentDir = Path.GetFullPath(Path.Combine(env.ContentRootPath));
var certDir = Path.Combine(parentDir, "cert");
var keyDir = Path.Combine(parentDir, "key");

var pfxDir = Path.Combine(certDir, "server-reexport.pfx");
var pfxBytes = await File.ReadAllBytesAsync(pfxDir);
var certPassword = builder.Configuration.GetValue<string>("Certificate:Password");

// logger.LogInformation("ContentRoot={root}; PfxPath={p}", env.ContentRootPath, pfxDir);
// logger.LogInformation("PfxExists={exists}", File.Exists(pfxDir));
// logger.LogInformation("certPassword={certPassword}", certPassword);
// logger.LogInformation("PfxLen={len}, PwdLen={pwdLen}", pfxBytes.Length, certPassword?.Length);

var cert = X509CertificateLoader.LoadPkcs12(
    pfxBytes,
    certPassword,
    // X509KeyStorageFlags.MachineKeySet |
    // X509KeyStorageFlags.PersistKeySet
    X509KeyStorageFlags.EphemeralKeySet
);

builder.Services.AddOpenIddict().AddCore(options =>
{
    options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
}).AddServer(options =>
{
    options.SetTokenEndpointUris("api/Auth/token");
    // options.SetRevocationEndpointUris("Authorization/revoke");

    options.AllowClientCredentialsFlow();
    options.AllowPasswordFlow();
    options.AllowCustomFlow("third_party");
    options.AllowRefreshTokenFlow();

    options.UseReferenceAccessTokens();
    options.UseReferenceRefreshTokens();

    options.RegisterScopes(OpenIddictConstants.Scopes.OfflineAccess);

    options.SetAccessTokenLifetime(TimeSpan.FromHours(2));
    options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

    options.DisableSlidingRefreshTokenExpiration();

    options.AcceptAnonymousClients();

    options.AddEncryptionCertificate(cert)
           .AddSigningCertificate(cert);
    // options.AddDevelopmentEncryptionCertificate()
    //        .AddDevelopmentSigningCertificate();

    options.UseAspNetCore()
           .EnableTokenEndpointPassthrough();


}).AddValidation(options =>
{
    options.UseLocalServer();

    options.UseAspNetCore();
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyDir))
    .ProtectKeysWithCertificate(cert)
    .SetApplicationName("WebAuthen");

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});
builder.Services.AddAuthorization();

builder.Services.AddHostedService<ClientService>();

// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenLocalhost(5035, listenOptions =>
//     {
//         listenOptions.UseHttps();
//     });
// });


var app = builder.Build();

var uploadPath = Path.Combine(builder.Environment.ContentRootPath, "workspace_file");
var uploadPath2 = Path.Combine(builder.Environment.ContentRootPath, "..", "MediaCenter");
Directory.CreateDirectory(uploadPath);
Directory.CreateDirectory(uploadPath2);

// for workspace file
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/workspace_file"
});

// for media center
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath2),
    RequestPath = "/MediaCenter"
});

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowLocal3000");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
