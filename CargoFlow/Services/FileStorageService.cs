using CargoFlow.Domain;
using Microsoft.Extensions.Options;

namespace CargoFlow.Services;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string RootPath { get; init; } = "Uploads";
    public string ShipmentDocumentSubDirectory { get; init; } = "ShipmentDocuments";
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024; // 10MB
    public Dictionary<string, string[]> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = ["image/jpg", "image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".webp"] = ["image/webp"],
        [".png"] = ["image/png"],
        [".pdf"] = ["applicaion/pdf"],
        [".csv"] = ["application/csv", "text/csv", "text/plain"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
    };
}

public sealed record StoredFileStream(Stream Stream, string FileDownloadName, string ContentType);

public interface IFileStorageService
{
    Task<ShipmentDocument> UploadShipmentDocumentAsync(
        IFormFile file,
        int shipmentId,
        int uploaderId,
        CancellationToken cancellationToken = default
    );

    Task<StoredFileStream> OpenShipmentDocumentAsync(
        ShipmentDocument shipmentDocument
    );

    void DeleteShipmentDocument(ShipmentDocument shipmentDocument);
}

public class FileStorageService(
    IOptions<FileStorageOptions> options,
    IWebHostEnvironment env,
    TimeProvider timeProvider
) : IFileStorageService
{
    private readonly FileStorageOptions _options = options.Value;

    public async Task<ShipmentDocument> UploadShipmentDocumentAsync(
        IFormFile file,
        int shipmentId,
        int uploaderId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0)
        {
            throw new ArgumentException("Uploaded file cannot be empty", nameof(file));
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File exceeds maximum allowed size of {_options.MaxFileSizeBytes} bytes");
        }

        var originalFileName = file.FileName;
        var extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension)
            || !_options.AllowedExtensions.TryGetValue(extension, out var allowedMimeTypes)
        )
        {
            throw new InvalidOperationException($"File type {extension} is not allowed");
        }

        var contentType = file.ContentType?.Split(";", 2)[0].Trim();
        if (string.IsNullOrWhiteSpace(contentType)
            || !allowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)
        )
        {
            throw new InvalidOperationException($"Content type '{contentType}' does not match allowed types for '{extension}'");
        }

        var rootPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, _options.RootPath));
        var safeSubDirectory = SanitizeRelativePath(_options.ShipmentDocumentSubDirectory);
        var targetFolder = Path.GetFullPath(Path.Combine(rootPath, safeSubDirectory));

        var utcNow = timeProvider.GetUtcNow();
        var storageKey = $"{utcNow:yyyy/MM/dd}/{Guid.NewGuid()}{extension}";
        var fullPath = Path.GetFullPath(Path.Combine(targetFolder, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        EnsureInsideRoot(fullPath, rootPath, nameof(storageKey));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        try
        {
            await using var stream = new FileStream(
                fullPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true
            );

            await file.CopyToAsync(stream, cancellationToken);
        }
        catch
        {
            TryDeleteFile(fullPath);
            throw;
        }

        return new()
        {
            OriginalFileName = originalFileName,
            ContentType = file.ContentType,
            DocumentType = extension,
            FileSize = file.Length,
            ShipmentId = shipmentId,
            StorageKey = storageKey,
            UploadedById = uploaderId,
            UploadedAt = utcNow.UtcDateTime
        };
    }

    private static string SanitizeRelativePath(string relativPath)
    {
        if (string.IsNullOrWhiteSpace(relativPath))
        {
            return string.Empty;
        }

        var normalized = relativPath.Replace('\\', '/').Trim('/');
        var segments = normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s is not "." and not "..");
        return Path.Combine([.. segments]);
    }

    private static void EnsureInsideRoot(string targetPath, string rootPath, string paramName)
    {
        var rootPathWithSep = rootPath.EndsWith(Path.DirectorySeparatorChar) ? rootPath : rootPath + Path.DirectorySeparatorChar;

        if (!targetPath.StartsWith(rootPathWithSep, StringComparison.Ordinal))
        {
            throw new ArgumentException("Resolved path escapes the configured storage root", nameof(paramName));
        }
    }

    private static void TryDeleteFile(string file)
    {
        try
        {

            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        catch
        {

        }

    }

    public async Task<StoredFileStream> OpenShipmentDocumentAsync(
        ShipmentDocument shipmentDocument
    )
    {
        ArgumentNullException.ThrowIfNull(shipmentDocument);

        var baseRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, _options.RootPath));
        var normalizedKey = shipmentDocument.StorageKey.Replace('/', Path.DirectorySeparatorChar);
        var safeSubDirectory = SanitizeRelativePath(_options.ShipmentDocumentSubDirectory);
        var targetFolder = Path.GetFullPath(Path.Combine(baseRoot, safeSubDirectory));
        var fullPath = Path.GetFullPath(Path.Combine(targetFolder, normalizedKey));

        EnsureInsideRoot(
            fullPath,
            baseRoot,
            nameof(shipmentDocument.StorageKey)
        );

        if (!File.Exists(fullPath))
        {
            throw new Exception($"shipment #{shipmentDocument.ShipmentId} document #{shipmentDocument.Id} is no longer exists");
        }

        var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true
        );

        var fileDownloadName = shipmentDocument.OriginalFileName;
        var contentType = shipmentDocument.ContentType;
        return new StoredFileStream(stream, fileDownloadName, contentType);

    }

    public void DeleteShipmentDocument(ShipmentDocument shipmentDocument)
    {
        var baseRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, _options.RootPath));
        var safeSubDirectory = SanitizeRelativePath(_options.ShipmentDocumentSubDirectory);
        var targetFolder = Path.GetFullPath(Path.Combine(baseRoot, safeSubDirectory));

        EnsureInsideRoot(targetFolder, baseRoot, nameof(shipmentDocument.StorageKey));

        var normalizedKey = shipmentDocument.StorageKey.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(targetFolder, normalizedKey));

        if (!File.Exists(fullPath))
        {
            throw new Exception($"shipment #{shipmentDocument.ShipmentId} document #{shipmentDocument.Id} is no longer exists");
        }

        try
        {
            File.Delete(fullPath);
        }
        catch
        {
            throw new Exception($"deleting shipment #{shipmentDocument.ShipmentId} document #{shipmentDocument.Id} has failed");
        }
    }
}
