namespace CargoFlow.Dtos;

public record ShipmentDocumentDto(
    int Id,
    int ShipmentId,
    string FileName,
    string StorageKey,
    string ContentType,
    long FileSize,
    int UploadedById,
    DateTime UploadedAt
);
