namespace CMAPI.DTO.Users;

public class UpdateUser
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public Guid RoleId { get; set; }
    public string PhoneNumber { get; set; }
    public bool isActive { get; set; }
}