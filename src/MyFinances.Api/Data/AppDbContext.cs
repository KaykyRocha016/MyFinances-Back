using Microsoft.EntityFrameworkCore;
using MyFinances.Api.Models;

namespace MyFinances.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Despesa> Despesas => Set<Despesa>();
    public DbSet<DespesaRateio> DespesasRateio => Set<DespesaRateio>();
    public DbSet<Nucleo> Nucleos => Set<Nucleo>();
    public DbSet<Ciclo> Ciclos => Set<Ciclo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map to lowercase snake_case tables and columns
        modelBuilder.Entity<Nucleo>(entity =>
        {
            entity.ToTable("nucleos");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasColumnName("nome").HasMaxLength(100);
        });

        modelBuilder.Entity<Ciclo>(entity =>
        {
            entity.ToTable("ciclos");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasColumnName("nome").HasMaxLength(100);
            entity.Property(e => e.DataInicio).HasColumnName("data_inicio");
            entity.Property(e => e.DataFim).HasColumnName("data_fim");
            entity.Property(e => e.Ativo).HasColumnName("ativo");
            entity.Property(e => e.NucleoId).HasColumnName("nucleo_id");

            entity.HasOne(c => c.Nucleo)
                .WithMany()
                .HasForeignKey(c => c.NucleoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasColumnName("nome").HasMaxLength(100);
            entity.Property(e => e.Renda).HasColumnName("renda").HasPrecision(18, 2);
            entity.Property(e => e.NucleoId).HasColumnName("nucleo_id");

            entity.HasOne(u => u.Nucleo)
                .WithMany()
                .HasForeignKey(u => u.NucleoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categorias");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasColumnName("nome").HasMaxLength(100);
            entity.Property(e => e.TipoDivisao).HasColumnName("tipo_divisao").HasMaxLength(50);
        });

        modelBuilder.Entity<Despesa>(entity =>
        {
            entity.ToTable("despesas");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Descricao).HasColumnName("descricao").HasMaxLength(255);
            entity.Property(e => e.Valor).HasColumnName("valor").HasPrecision(18, 2);
            entity.Property(e => e.Data).HasColumnName("data");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.CicloId).HasColumnName("ciclo_id");

            entity.HasOne(d => d.Usuario)
                .WithMany()
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Categoria)
                .WithMany()
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Ciclo)
                .WithMany()
                .HasForeignKey(d => d.CicloId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DespesaRateio>(entity =>
        {
            entity.ToTable("despesas_rateio");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DespesaId).HasColumnName("despesa_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.Valor).HasColumnName("valor").HasPrecision(18, 2);

            entity.HasOne(d => d.Despesa)
                .WithMany(p => p.Rateios)
                .HasForeignKey(d => d.DespesaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Usuario)
                .WithMany()
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
