using System;
using CoinsManagerService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace CoinsManagerService.Data
{
    public partial class AppDbContext : DbContext
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Coin> Coins { get; set; }
        public virtual DbSet<CoinType> CoinTypes { get; set; }
        public virtual DbSet<Continent> Continents { get; set; }
        public virtual DbSet<Country> Countries { get; set; }
        public virtual DbSet<Period> Periods { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "Cyrillic_General_CI_AS");

            modelBuilder.Entity<Coin>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Currency).IsRequired();

                entity.Property(e => e.Nominal)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Period).HasDefaultValueSql("((1))");

                entity.Property(e => e.Year)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PictPreviewPath);

                entity.HasOne(d => d.PeriodNavigation)
                    .WithMany(p => p.Coins)
                    .HasForeignKey(d => d.Period)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Coins_Periods");

                entity.HasOne(d => d.TypeNavigation)
                    .WithMany(p => p.Coins)
                    .HasForeignKey(d => d.Type)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Coins_Types");
            });

            modelBuilder.Entity<CoinType>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");
            });

            modelBuilder.Entity<Continent>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("Name");
            });

            modelBuilder.Entity<Country>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("Name");

                entity.HasOne(d => d.ContinentNavigation)
                    .WithMany(p => p.Countries)
                    .HasForeignKey(d => d.Continent)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Countries_Continents");
            });

            modelBuilder.Entity<Period>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("Name");

                entity.HasOne(d => d.CountryNavigation)
                    .WithMany(p => p.Periods)
                    .HasForeignKey(d => d.Country)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Periods_Countries");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
