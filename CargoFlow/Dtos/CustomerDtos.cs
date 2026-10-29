using System.ComponentModel.DataAnnotations;
using CargoFlow.Helpers;

namespace CargoFlow.Dtos;

public record CustomerDto(
    int Id,
    string Name,
    string NationalId
    );

public record CreateCustomerRequestDto(
    [Required]
    [MaxLength(25, ErrorMessage = "Name must have 25 characters maximum")]
    string Name,

    [Required]
    [ExactDigits(10)]
   string NationalId
    );
