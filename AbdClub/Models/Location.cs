using System.ComponentModel.DataAnnotations;

namespace AbdClub.Models;

public class Location
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string VenueName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Address { get; set; } = string.Empty;

    public string? Description { get; set; } // Entry notes, gate codes, parking tips
    [Url(ErrorMessage = "Please provide a valid navigation hyperlink URL address layout.")]
    [StringLength(1024, ErrorMessage = "Google Maps link cannot exceed 1024 characters.")]
    public string? GoogleMapsUrl { get; set; }
    public string? PhotoUrl { get; set; } // Direct path to static asset or storage bucket

    // Navigation tracking
    public List<Event> Events { get; set; } = new();
}

