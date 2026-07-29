using System.ComponentModel.DataAnnotations;

namespace AbdClub.Dtos;

public class BroadcastInputDto
{
    [Required(ErrorMessage = "Please supply a notification subject line.")]
    [StringLength(100, ErrorMessage = "Subject must be under 100 characters.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "The message contents cannot be empty.")]
    [StringLength(2000, ErrorMessage = "The broadcast letter body must be under 2000 characters.")]
    public string MessageContent { get; set; } = string.Empty;
}
