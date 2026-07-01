using DocuTutor.Application.Interfaces;
using DocuTutor.Domain.Entities;
using DocuTutor.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace DocuTutor.Infrastructure.Repositories
{
    public class DocumentService : IDocumentService
    {
        private readonly DocuTutorDbContext _context;

        public DocumentService(DocuTutorDbContext context)
        {
            _context = context;
        }

        public async Task<Document> CreateDocumentAsync(string fileName, string cloudinaryUrl, string userId)
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                CloudinaryUrl = cloudinaryUrl,
                UserId = userId,
                Status = "processing",
                CreatedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task<Document?> GetDocumentByIdAsync(Guid documentId, string? userId = null)
        {
            var query = _context.Documents.AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(d => d.Id == documentId && d.UserId == userId);
            }
            else
            {
                query = query.Where(d => d.Id == documentId);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task UpdateDocumentStatusAsync(Guid documentId, string status, string? userId = null)
        {
            var document = await GetDocumentByIdAsync(documentId, userId);
            if (document != null)
            {
                document.Status = status;
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
