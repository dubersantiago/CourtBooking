using Domain.Courts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> builder)
    {
        builder.ToTable("courts");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();

        builder.Property(b => b.IsActive).IsRequired();

        builder.OwnsMany(c => c.Images, images =>
        {
            images.ToTable("court_images");
            images.WithOwner().HasForeignKey("CourtId");

            images.HasKey(i => i.Id);
            images.Property(i => i.Id).ValueGeneratedNever();
            images.Property(i => i.StorageKey).HasMaxLength(255).IsRequired();
        });

        builder.Navigation(c => c.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}