using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace backend;

using Entities;

internal partial class PdsContext : DbContext
{
    public PdsContext() { }

    public PdsContext(DbContextOptions<PdsContext> options)
        : base(options) { }

    public DbSet<FriendsEntity> Friends { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FriendsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("friends", "public");

            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Id).HasColumnName("id");
        });
    }
}
