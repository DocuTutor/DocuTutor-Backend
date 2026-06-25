using System;

namespace DocuTutor.Application.DTOs
{
    public class DocumentUploadResponseDto
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string CloudinaryUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "processing";
        public DateTime CreatedAt { get; set; }
    }
}
