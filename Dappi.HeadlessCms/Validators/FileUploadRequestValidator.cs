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
                .Must(BeASupportedExtension)
                .WithMessage("Unsupported media type.");

            RuleFor(x => x.FieldName).NotEmpty().WithMessage("Field name is required.");
        }

        private bool BeASupportedExtension(IFormFile? file)
        {
            if (file == null)
                return false;

            var extension = System.IO.Path.GetExtension(file.FileName);
            if (
                _storageOptions.SupportedExtensions is null
                || _storageOptions.SupportedExtensions.Count == 0
            )
            {
                return true;
            }

            var normalizedExtension = extension.TrimStart('.').ToLower();
            return _storageOptions.SupportedExtensions.Any(allowed =>
                allowed.Equals(
                    normalizedExtension,
                    System.StringComparison.CurrentCultureIgnoreCase
                )
            );
        }
    }
}
