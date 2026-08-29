using System.ComponentModel.DataAnnotations;

namespace AbdClub.Models;

public class Outing : Event
{
    [Url, StringLength(1024)]
    public string? ExternalWebsiteUrl { get; set; }

    [StringLength(1000)]
    public string? RegistrationInstructions { get; set; }
}
