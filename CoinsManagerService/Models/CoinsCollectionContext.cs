using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace CoinsManagerService.Models
{
    public partial class CoinsCollectionContext : DbContext
    {
        public CoinsCollectionContext()
        {
        }

        public CoinsCollectionContext(DbContextOptions<CoinsCollectionContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Coin> Coins { get; set; }
        public virtual DbSet<CoinType> CoinTypes { get; set; }
        public virtual DbSet<Continent> Continents { get; set; }
        public virtual DbSet<Country> Countries { get; set; }
        public virtual DbSet<Period> Periods { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=.\\SQLExpress;Database=CoinsCollection;Trusted_Connection=True;");
            }
        }

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

                entity.Property(e => e.Continent1)
                    .IsRequired()
                    .HasColumnName("Continent");
            });

            modelBuilder.Entity<Country>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Country1)
                    .IsRequired()
                    .HasColumnName("Country");

                entity.HasOne(d => d.ContinentNavigation)
                    .WithMany(p => p.Countries)
                    .HasForeignKey(d => d.Continent)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Countries_Continents");
            });

            modelBuilder.Entity<Period>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Period1)
                    .IsRequired()
                    .HasColumnName("Period");

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
