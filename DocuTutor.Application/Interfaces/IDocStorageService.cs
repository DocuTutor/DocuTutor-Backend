using DocuTutor.Application.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Application.Interfaces
{
    public interface IDocStorageService
    {
        Task<DocUploadDto> UploadDocumentAsync(IFormFile file);
    }
}
