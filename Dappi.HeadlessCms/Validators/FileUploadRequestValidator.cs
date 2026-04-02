using Dappi.HeadlessCms.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Dappi.HeadlessCms.Validators
{
    public class FileUploadRequest
    {
        public IFormFile File { get; set; }
        public string FieldName { get; set; }
    }

    public class FileUploadRequestValidator : AbstractValidator<FileUploadRequest>
    {
        private readonly AwsStorageOptions _storageOptions;

        private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new()
        {
            {
                "jpg",
                new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } }
            },
            {
                "jpeg",
                new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } }
            },
            {
                "png",
                new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47 } }
            },
            {
                "gif",
                new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } }
            },
            {
                "bmp",
                new List<byte[]> { new byte[] { 0x42, 0x4D } }
            },
            {
                "pdf",
                new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } }
            },
            {
                "doc",
                new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } }
            },
            {
                "docx",
                new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } }
            },
            {
                "xlsx",
                new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } }
            },
            {
                "txt",
                new List<byte[]> { new byte[] { 0xEF, 0xBB, 0xBF }, new byte[] { } }
            },
        };

        public FileUploadRequestValidator(IConfiguration configuration)
        {
            _storageOptions = configuration
                .GetSection(AwsStorageOptions.AwsStorage)
                .Get<AwsStorageOptions>()!;

            RuleFor(x => x.File)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("No file was uploaded.")
                .Must(file => file.Length > 0)
                .WithMessage("File is empty.")
                .Must(BeAValidFile)
                .WithMessage("Unsupported media type.");

            RuleFor(x => x.FieldName).NotEmpty().WithMessage("Field name is required.");
        }

        private bool BeAValidFile(IFormFile? file)
        {
            if (file == null)
                return false;

            if (!BeASupportedExtension(file))
                return false;

            return HasValidFileSignature(file);
        }

        private bool BeASupportedExtension(IFormFile file)
        {
            if (
                _storageOptions.SupportedExtensions == null
                || _storageOptions.SupportedExtensions.Count == 0
            )
                return true;

            var extension = Path.GetExtension(file.FileName)?.TrimStart('.').ToLower();
            if (string.IsNullOrEmpty(extension))
                return false;

            return _storageOptions.SupportedExtensions.Any(ext =>
                ext.Equals(extension, System.StringComparison.InvariantCultureIgnoreCase)
            );
        }

        private bool HasValidFileSignature(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
            if (string.IsNullOrEmpty(extension) || !_fileSignatures.ContainsKey(extension))
                return false;

            var signatures = _fileSignatures[extension];

            using var stream = file.OpenReadStream();
            foreach (var signature in signatures)
            {
                if (signature.Length == 0)
                    return true;

                var headerBytes = new byte[signature.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.ReadExactly(headerBytes, 0, headerBytes.Length);

                if (headerBytes.SequenceEqual(signature))
                    return true;
            }

            return false;
        }
    }
}
