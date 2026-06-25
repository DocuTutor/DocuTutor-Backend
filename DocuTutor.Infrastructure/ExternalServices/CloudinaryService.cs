using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DocuTutor.Application.DTOs;
using DocuTutor.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using Microsoft.Extensions.Options;


namespace DocuTutor.Infrastructure.ExternalServices
{
    public class CloudinaryService : IDocStorageService
    {
        private readonly Cloudinary _cloudinary;
        private const long MAX_FILE_SIZE = 50 * 1024 * 1024;
        private const string ALLOWED_MIME_TYPE = "application/pdf";
        private const string PDF_EXTENSION = ".pdf";

        public CloudinaryService(IOptions<CloudinarySettings> cloudinaryOptions)
        {
            var settings = cloudinaryOptions.Value;
            var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<DocUploadDto> UploadDocumentAsync(IFormFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file), "File cannot be null");

            if (file.Length > MAX_FILE_SIZE)
                throw new InvalidOperationException($"File size exceeds maximum allowed size of {MAX_FILE_SIZE / (1024 * 1024)} MB");

            if (file.Length == 0)
                throw new InvalidOperationException("File cannot be empty");

            if (file.ContentType != ALLOWED_MIME_TYPE)
                throw new InvalidOperationException($"Only PDF files are allowed. Received: {file.ContentType}");

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (fileExtension != PDF_EXTENSION)
                throw new InvalidOperationException($"File extension must be .pdf. Received: {fileExtension}");

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName).Replace(" ", "_");
                    var uploadParams = new RawUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        PublicId = $"docututor-pdfs/{Guid.NewGuid()}_{fileNameWithoutExtension}",
                        UseFilename = false,
                        UniqueFilename = false,
                        Overwrite = false,
                        Tags = "docututor,pdf",
                        AccessMode = "public"
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.Error != null)
                        throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");

                    return new DocUploadDto
                    {
                        Url = uploadResult.SecureUrl.ToString(),
                        PublicId = uploadResult.PublicId
                    };
                }
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"An error occurred while uploading the file: {ex.Message}", ex);
            }
        }
    }
}
