using DocuTutor.Domain.Entities;
using DocuTutor.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Infrastructure.Configurations
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("messages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(x => x.Content)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(MessageStatus.Completed);


            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ConversationId);
            builder.HasIndex(x => x.CreatedAt);
        }
    }
