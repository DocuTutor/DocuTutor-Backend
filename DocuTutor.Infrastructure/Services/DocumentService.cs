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

        public async Task<Document> CreateDocumentAsync(string fileName, string cloudinaryUrl, Guid? userId = null)
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

        public async Task<Document?> GetDocumentByIdAsync(Guid documentId)
        {
            return await _context.Documents
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == documentId);
        }

        public async Task UpdateDocumentStatusAsync(Guid documentId, string status)
        {
            var document = await _context.Documents.FindAsync(documentId);
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
