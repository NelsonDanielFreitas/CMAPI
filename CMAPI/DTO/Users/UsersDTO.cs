namespace CMAPI.DTO.Users;

public class UsersDTO
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public bool isActive { get; set; }
    public RoleDto RoleId { get; set; }
    
}