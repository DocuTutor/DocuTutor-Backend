using DocuTutor.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace DocuTutor.Application.Interfaces
{
    public interface IDocumentService
    {
        Task<Document> CreateDocumentAsync(string fileName, string cloudinaryUrl, Guid? userId = null);
        Task<Document?> GetDocumentByIdAsync(Guid documentId);
        Task UpdateDocumentStatusAsync(Guid documentId, string status);
        Task SaveChangesAsync();
    }
}
