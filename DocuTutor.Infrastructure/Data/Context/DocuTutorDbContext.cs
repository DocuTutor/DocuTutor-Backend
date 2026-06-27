using DocuTutor.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.ComponentModel;
using System.Text.Json;

namespace DocuTutor.Infrastructure.Data.Context
{
    public class DocuTutorDbContext : IdentityDbContext<ApplicationUser>
    {
        public DocuTutorDbContext(DbContextOptions<DocuTutorDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<DocumentChunk> DocumentChunks { get; set; } = null!;

        //Adding them when we implement the conversation and message features to avoid conflict
        //public DbSet<Conversation> Conversations => Set<Conversation>();
        //public DbSet<Message> Messages => Set<Message>();
        //public DbSet<MessageCitation> MessageCitations => Set<MessageCitation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //apply when working in feature 
            //modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocuTutorDbContext).Assembly);

            // Value converter for float[] to vector type
            var embeddingConverter = new ValueConverter<float[]?, string?>(
                v => v == null ? null : $"[{string.Join(",", v)}]",
                v => v == null ? null : JsonSerializer.Deserialize<float[]>(v));

            // Value comparer for float[]
            var embeddingComparer = new ValueComparer<float[]?>(
                (c1, c2) => c1 == null && c2 == null || c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v)),
                c => c == null ? null : c.ToArray());

            // Document configuration
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
                entity.Property(e => e.CloudinaryUrl).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("processing");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Relationships
                entity.HasMany(e => e.Chunks)
                    .WithOne(c => c.Document)
                    .HasForeignKey(c => c.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DocumentChunk configuration
            modelBuilder.Entity<DocumentChunk>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ChunkText).IsRequired();
                
                // pgvector configuration with value converter and comparer
                entity.Property(e => e.Embedding)
                    .HasColumnType("vector(1536)")
                    .HasConversion(embeddingConverter, embeddingComparer);
                
                // Create index for vector search
                entity.HasIndex(e => e.Embedding)
                    .HasMethod("hnsw")
                    .HasOperators("vector_cosine_ops");
                
                // Create index for document_id
                entity.HasIndex(e => e.DocumentId);
            });
        }
    }
}
