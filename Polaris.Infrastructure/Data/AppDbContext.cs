using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Polaris.Domain.Entities;
using Polaris.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Polaris.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSet properties for each entity
        public DbSet<LocalUser> LocalUsers { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ProjectFile> ProjectFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. ApplicationUser - LocalUser (One-to-One)
            modelBuilder.Entity<LocalUser>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);

                entity.HasOne<ApplicationUser>()
                      .WithOne()
                      .HasForeignKey<LocalUser>(u => u.Id);
            });

            // 2. LocalUser - Project (One-to-Many)
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasOne<LocalUser>()
                      .WithMany(u => u.Projects)
                      .HasForeignKey(p => p.UserId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 3. LocalUser - Conversation (One-to-Many)
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasOne(c => c.User)
                      .WithMany(u => u.Conversations)
                      .HasForeignKey(c => c.UserId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);

                // Conversation - Project (Optional One-to-One)
                entity.HasOne(c => c.Project)
                      .WithMany(p => p.Conversations)
                      .HasForeignKey(c => c.ProjectId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // 4. Conversation - Message (One-to-Many)
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Conversation)
                      .WithMany(c => c.Messages)
                      .HasForeignKey(m => m.ConversationId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Improving code storage in Postgres
                entity.Property(m => m.Content).HasColumnType("text");
            });

            // 5. Project - ProjectFile (One-to-Many)
            modelBuilder.Entity<ProjectFile>(entity =>
            {
                entity.HasOne(f => f.Project)
                      .WithMany(p => p.Files)
                      .HasForeignKey(f => f.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Improving code storage in Postgres
                entity.Property(f => f.Content).HasColumnType("text");
            });
        }
    }
}
