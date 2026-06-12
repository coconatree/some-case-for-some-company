using System.ComponentModel.DataAnnotations;

namespace MilkingSystem.Api.Requests;

public class CreateAnimalRequest
{
    public string IdentificationNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public DateTime? BirthDate { get; set; }
}
