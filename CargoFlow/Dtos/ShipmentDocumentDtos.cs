namespace CargoFlow.Dtos;

public record ShipmentDocumentDto(
    int Id,
    int ShipmentId,
    string FileName,
    string ContentType,
    long FileSize,
    int UploadedById,
    DateTime UploadedAt
);
