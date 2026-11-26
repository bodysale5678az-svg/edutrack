using System.Text;

namespace EduTrack.Services
{
    public sealed record FileCheck(bool Success, string? Error)
    {
        public static FileCheck Ok { get; } = new(true, null);

        public static FileCheck Fail(string error) => new(false, error);
    }

    public class FileStorageService
    {
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] PdfSignature = Encoding.ASCII.GetBytes("%PDF-");
        private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 };
        private static readonly byte[] RarSignature = { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 };

        public static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        public static readonly string[] SubmissionExtensions = { ".zip", ".rar", ".pdf" };
        public static readonly string[] AttachmentExtensions = { ".pdf" };

        public const long ImageMaxBytes = 2 * 1024 * 1024;
        public const long SubmissionMaxBytes = 20 * 1024 * 1024;
        public const long AttachmentMaxBytes = 10 * 1024 * 1024;

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public FileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
        }

        /// <summary>
        /// مجلد التخزين. على الاستضافة wwwroot غير قابل للكتابة،
        /// فبنقرأ المسار من FileStorage:RootFolder في إعدادات البيئة.
        /// لو فاضي بنستخدم wwwroot (سلوك التطوير المحلي).
        /// </summary>
        private string UploadRoot
        {
            get
            {
                string? configured = _configuration["FileStorage:RootFolder"];

                return string.IsNullOrWhiteSpace(configured)
                    ? _environment.WebRootPath
                    : Path.Combine(_environment.ContentRootPath, configured);
            }
        }

        public static long MaxBytesFor(string[] allowed, long configuredMax)
        {
            return allowed.SequenceEqual(ImageExtensions) ? ImageMaxBytes
                 : allowed.SequenceEqual(SubmissionExtensions) ? SubmissionMaxBytes
                 : configuredMax;
        }

        public static FileCheck Validate(IFormFile? file, string[] allowedExtensions, long maxBytes)
        {
            if (file == null || file.Length == 0)
            {
                return FileCheck.Ok;
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return FileCheck.Fail($"Only these file types are allowed: {string.Join(", ", allowedExtensions)}.");
            }

            if (file.Length > maxBytes)
            {
                return FileCheck.Fail($"The file must be smaller than {maxBytes / (1024 * 1024)} MB.");
            }

            if (!HasValidSignature(file, extension))
            {
                return FileCheck.Fail("The selected file is not a valid file of the chosen type.");
            }

            return FileCheck.Ok;
        }

        public static bool HasValidSignature(IFormFile file, string extension)
        {
            using var stream = file.OpenReadStream();
            Span<byte> header = stackalloc byte[12];
            int read = stream.Read(header);

            if (read <= 0)
            {
                return false;
            }

            return extension switch
            {
                ".png" => read >= 8 && header[..8].SequenceEqual(PngSignature),
                ".jpg" or ".jpeg" => read >= 3 && header[..3].SequenceEqual(JpegSignature),
                ".webp" => read >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
                ".pdf" => read >= 5 && header[..5].SequenceEqual(PdfSignature),
                ".zip" => read >= 4 && header[..4].SequenceEqual(ZipSignature),
                ".rar" => read >= 7 && header[..7].SequenceEqual(RarSignature),
                _ => false
            };
        }

        public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
        {
            string directory = Path.Combine(UploadRoot, folder);
            Directory.CreateDirectory(directory);

            string fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();

            await using FileStream target = File.Create(Path.Combine(directory, fileName));
            await file.CopyToAsync(target, cancellationToken);

            return fileName;
        }

        public void Delete(string? fileName, string folder)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            string root = Path.GetFullPath(UploadRoot);
            string fullPath = Path.GetFullPath(Path.Combine(root, folder, Path.GetFileName(fileName)));

            if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}
