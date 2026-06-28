using DocuTutor.Application.DTOs;

namespace DocuTutor.Application.Interfaces
{
    public interface IRetrievalService
    {
        Task<AnswerResultDto> AnswerAsync(
            Guid documentId,
            string question,
            CancellationToken cancellationToken = default);
    }
}
