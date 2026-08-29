using AbdClub.Models.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace AbdClub.Models
{
    public enum EntertainmentType
    {
        DJ = 1,
        Band = 2,
        Other = 3
    }

    public class MasterDJ : IRegistryPerson
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [EmailAddress] public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public EntertainmentType EntertainmentType { get; set; } = EntertainmentType.DJ;
    }
}
