﻿using App.Entities.Entities_2;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Context;

public partial class MyDbContext : DbContext
{
    public MyDbContext()
    {
    }

    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }
    
    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Handbag> Handbags { get; set; }

    public virtual DbSet<SystemAccount> SystemAccounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
         modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandID).HasName("PK__Brand__DAD4F3BEE8CB9D91");

            entity.ToTable("Brand");

            entity.Property(e => e.BrandID).ValueGeneratedNever();
            entity.Property(e => e.BrandName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Website)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Handbag>(entity =>
        {
            entity.HasKey(e => e.HandbagID).HasName("PK__Handbag__785BD69FFB0AC100");

            entity.ToTable("Handbag");

            entity.Property(e => e.HandbagID).ValueGeneratedNever();
            entity.Property(e => e.Color)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Material)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ModelName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Brand).WithMany(p => p.Handbags)
                .HasForeignKey(d => d.BrandID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_handbag_brand");
        });

        modelBuilder.Entity<SystemAccount>(entity =>
        {
            entity.HasKey(e => e.AccountID).HasName("PK__SystemAc__349DA586CF399C7A");

            entity.Property(e => e.AccountID).ValueGeneratedNever();
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
