using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Dtos;

public record LoginRequestDto(string Email, string Password);
public record RegisterCustomerRequestDto(
    [EmailAddress]
    string Email,
    string Password,
    [MaxLength(25, ErrorMessage = "Name must have 25 character maximum")]
    string Name,
    [MaxLength(10, ErrorMessage = "NationalId must have 10 characters maximum")]
    string NationalId);
public record RegisterCompanyRequestDto(
    [EmailAddress]
    string Email,
    string Password,
    [MaxLength(40, ErrorMessage = "Name must have maximum 40 characters")]
    string Name,
    [MaxLength(15, ErrorMessage = "TaxId must have maximum 15 characters")]
    string TaxID
);
public record LoginResponseDto(string AccessToken);
public record RegisterResponseDto(string Message);
