using CMAPI.Models;

namespace CMAPI.DTO;

public class UserDto
{
    public Guid Id { get; set; }
    public string? refreshToken { get; set; }

    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public bool EmailVerified { get; set; }
    public RoleDto Role { get; set; }
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string RoleName { get; set; }
}

public class AuthenticationResult
{
    public bool Locked { get; set; }
    public double LockoutTimeLeft { get; set; }
    public UserDto User { get; set; }
}