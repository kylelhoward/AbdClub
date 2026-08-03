namespace AbdClub.Models.Interfaces
{
    public interface IRegistryPerson
    {
        int Id { get; set; }
        string Name { get; set; }
        string? Email { get; set; }
        string? Phone { get; set; }
        string? Notes { get; set; }
    }
}
