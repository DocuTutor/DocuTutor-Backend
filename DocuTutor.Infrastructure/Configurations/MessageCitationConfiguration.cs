using DocuTutor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Infrastructure.Configurations
{
    public class MessageCitationConfiguration : IEntityTypeConfiguration<MessageCitation>
    {
        public void Configure(EntityTypeBuilder<MessageCitation> builder)
        {
            builder.ToTable("message_citations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuoteText)
                .HasMaxLength(2000);

            builder.Property(x => x.Ordinal)
                .HasDefaultValue(0);

            builder.HasOne(x => x.Message)
                .WithMany(x => x.Citations)
                .HasForeignKey(x => x.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
            //will be handeled later
            //builder.HasOne(x => x.DocumentChunk)
            //    .WithMany(x => x.MessageCitations)
            //    .HasForeignKey(x => x.DocumentChunkId)
            //    .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.MessageId);
            builder.HasIndex(x => x.DocumentChunkId);
        }
    }
}
