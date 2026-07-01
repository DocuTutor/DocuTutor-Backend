using DocuTutor.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace DocuTutor.Application.Interfaces
{
    public interface IDocumentService
    {
        Task<Document> CreateDocumentAsync(string fileName, string cloudinaryUrl, string userId);
        Task<Document?> GetDocumentByIdAsync(Guid documentId, string? userId = null);
        Task UpdateDocumentStatusAsync(Guid documentId, string status, string? userId = null);
        Task SaveChangesAsync();
    }
}
