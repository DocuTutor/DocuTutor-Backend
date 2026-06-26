using DocuTutor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Infrastructure.Configurations
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.ToTable("conversations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .HasMaxLength(300);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();

            //will be handeled after complete db setup
            //builder.HasOne(x => x.User)
            //    .WithMany(x => x.Conversations)
            //    .HasForeignKey(x => x.UserId)
            //    .OnDelete(DeleteBehavior.Cascade);

            //builder.HasOne(x => x.Document)
            //    .WithMany(x => x.Conversations)
            //    .HasForeignKey(x => x.DocumentId)
            //    .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.DocumentId);
        }
    }
}
