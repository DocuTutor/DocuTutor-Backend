using DocuTutor.Application.DTOs;

namespace DocuTutor.Application.Interfaces
{
    public interface IRetrievalService
    {
        Task<AnswerResultDto> AnswerAsync(
            Guid documentId,
            string question,
            string? userId = null,
            CancellationToken cancellationToken = default);
    }
}
