using System.ComponentModel.DataAnnotations;

namespace CMAPI.DTO;

public class UserLoginDTO
{
    [Required, EmailAddress]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
}