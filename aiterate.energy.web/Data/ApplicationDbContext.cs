using aiterate.energy.web.Models.Identity;
using aiterate.energy.web.Models.Persistence;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace aiterate.energy.web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<HomeWizardQuarterHourAggregate> HomeWizardQuarterHourAggregates => Set<HomeWizardQuarterHourAggregate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<HomeWizardQuarterHourAggregate>(entity =>
        {
            entity.ToTable("HomeWizardQuarterHourAggregates");
            entity.HasKey(x => x.PeriodStart);
            entity.HasIndex(x => x.PeriodEnd);

            foreach (var property in entity.Metadata.GetProperties()
                         .Where(property => property.ClrType == typeof(decimal)))
            {
                property.SetPrecision(12);
                property.SetScale(3);
            }
        });
    }
}
