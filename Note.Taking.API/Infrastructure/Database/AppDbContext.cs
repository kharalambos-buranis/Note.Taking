using Microsoft.EntityFrameworkCore;
using Note.Taking.API.Common.Models;

namespace Note.Taking.API.Infrastructure.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Note.Taking.API.Common.Models.Note> Notes { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<NoteTag> NoteTags { get; set; } 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasKey(u => u.Id).HasName("pk_users");

                entity.Property(u => u.Id).HasColumnName("id");
                entity.Property(u => u.Email).HasColumnName("email");
                entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
                entity.Property(u => u.FullName).HasColumnName("full_name");
                entity.Property(u => u.CreatedAt).HasColumnName("created_at");

                entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("ux_users_emails");
            });

            modelBuilder.Entity<Note.Taking.API.Common.Models.Note>(entity =>
            {
                entity.ToTable("notes");

                entity.HasKey(n => n.Id).HasName("pk_notes");

                entity.Property(n => n.Id).HasColumnName("id");
                entity.Property(n => n.UserId).HasColumnName("user_id");
                entity.Property(n => n.Title).HasColumnName("title");
                entity.Property(n => n.Content).HasColumnName("content");
                entity.Property(n => n.IsDeleted).HasColumnName("is_deleted");
                entity.Property(n => n.CreatedAt).HasColumnName("created_at");

                entity.HasQueryFilter(n => !n.IsDeleted);

                entity.HasOne(n => n.User)
                .WithMany(n => n.Notes)
                .HasForeignKey(n => n.UserId)
                .HasConstraintName("fk_notes_user_id");
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.ToTable("tags");

                entity.HasKey(t => t.Id).HasName("pk_tags");

                entity.Property(t => t.Id).HasColumnName("id");
                entity.Property(t => t.Name).HasColumnName("name");

                entity.HasIndex(t => t.Name).IsUnique().HasDatabaseName("us_tags_name");
            });

            modelBuilder.Entity<NoteTag>(entity =>
            {
                entity.ToTable("note_tags");

                entity.HasKey(nt => new { nt.NoteId, nt.TagId }).HasName("pk_note_tags");

                entity.Property(nt => nt.NoteId).HasColumnName("note_id");
                entity.Property(nt => nt.TagId).HasColumnName("tag_id");

                entity.HasOne(nt => nt.Note)
                    .WithMany(n => n.NoteTags)
                    .HasForeignKey(nt => nt.NoteId)
                    .HasConstraintName("fk_note_tags_note_id");

                entity.HasOne(nt => nt.Tag)
                    .WithMany(t => t.NoteTags)
                    .HasForeignKey(nt => nt.TagId)
                    .HasConstraintName("fk_note_tags_tag_id");
            });
        }
    }
}
