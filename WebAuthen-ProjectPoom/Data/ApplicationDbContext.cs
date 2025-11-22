using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace WebAuthen.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole, String>(options)
{





    // EF Core จะสร้างตาราง ASP.NET Identity และ OpenIddict ให้ทั้งหมดที่นี่
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Map OpenIddict entities (Applications, Scopes, Tokens…)
        builder.UseOpenIddict();
    }
}

// public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
// {

//     public DbSet<OpenIddictEntityFrameworkCoreApplication> Applications { get; set; }
//     public DbSet<OpenIddictEntityFrameworkCoreAuthorization> Authorizations { get; set; }
//     public DbSet<OpenIddictEntityFrameworkCoreScope> Scopes { get; set; }
//     public DbSet<OpenIddictEntityFrameworkCoreToken> Tokens { get; set; }
// }
