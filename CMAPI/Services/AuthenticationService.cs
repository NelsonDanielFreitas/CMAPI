using System.Net;
using System.Net.Mail;
using System.Text;
using CMAPI.Data;
using CMAPI.DTO;
using CMAPI.Helper;
using CMAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CMAPI.Services;

public class AuthenticationService
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtTokenService;
    private readonly IConfiguration _config;
    
    public AuthenticationService(AppDbContext context, JwtService jwtTokenService, IConfiguration configuration)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _config = configuration;
    }
    
    public async Task<bool> RegisterUserAsync(RegisterDTO registerDto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            return false;

        bool isFirstUser = !await _context.Users.AnyAsync();
        string desiredRoleName = isFirstUser ? "ADMIN" : "CLIENTE";

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleName == desiredRoleName);

        if (role == null)
            throw new InvalidOperationException(
                $"Role '{desiredRoleName}' not found in the Roles table.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
        var verificationCode = new Random().Next(100000, 999999).ToString();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            PhoneNumber = registerDto.PhoneNumber,
            Email = registerDto.Email,
            Password = passwordHash,
            Role = role,                     
            EmailVerificationCode = verificationCode,
            EmailVerified = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        SendVerificationEmail(user.Email, verificationCode);
        return true;
    }
    
    //Função para enviar o email com o código
    private void  SendVerificationEmail(string email, string code)
    {
        var fromAddress = new MailAddress(_config["Email:Username"], "Your App");
        var toAddress = new MailAddress(email);
        const string subject = "Email Verification";
        string body = $"Your verification code is: {code}";

        var smtp = new SmtpClient
        {
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            Credentials = new NetworkCredential(_config["Email:Username"], _config["Email:Password"])
        };

        using var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body,
        };
        smtp.Send(message);
    }
    
    //Função para validar o email
    public async Task<bool> VerifyEmailAsync(VerifyEmailDTO verifyEmail)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == verifyEmail.Email);
        if (user == null || user.EmailVerificationCode != verifyEmail.Code)
        {
            return false;
        }

        user.EmailVerified = true;
        user.EmailVerificationCode = null;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<AuthenticationResult> ValidateUserAsync(UserLoginDTO login)
    {
        var user = await _context.Users
            .Include(u => u.Role)                // make sure Role is loaded
            .SingleOrDefaultAsync(u => u.Email == login.Email);

        if (user == null)
        {
            return new AuthenticationResult { User = null };
        }

        if (!user.isActive)
        {
            return new AuthenticationResult { User = null };
        }

        if (user.LockoutEndTime.HasValue && user.LockoutEndTime > DateTime.UtcNow)
        {
            return new AuthenticationResult
            {
                Locked = true,
                LockoutTimeLeft = (user.LockoutEndTime.Value - DateTime.UtcNow).Minutes
            };
        }

        if (!BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= user.MaxFailedAttempts)
            {
                user.LockoutEndTime = DateTime.UtcNow.Add(user.LockoutDuration);
                await _context.SaveChangesAsync();
                return new AuthenticationResult
                {
                    Locked = true,
                    LockoutTimeLeft = user.LockoutDuration.TotalMinutes
                };
            }

            await _context.SaveChangesAsync();
            return new AuthenticationResult { User = null };
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndTime = null;
        
        var plainRefreshToken = _jwtTokenService.GenerateRefreshToken();
        
        string encryptionKeyString = _config["Encryption:RefreshTokenKey"];
        byte[] encryptionKey = Encoding.UTF8.GetBytes(encryptionKeyString);
            
        // Encrypt the refresh token using AES-256
        user.refreshToken = AesEncryption.Encrypt(plainRefreshToken, encryptionKey);
        user.RefreshTokenExpiryTime = _jwtTokenService.GetRefreshTokenExpiry();
        /*user.refreshToken = _jwtTokenService.GenerateRefreshToken();
        user.refreshTokenExpiryTime = _jwtTokenService.GetRefreshTokenExpiry();*/
        await _context.SaveChangesAsync();
        
        var dto = new UserDto {
            Id = user.Id,
            Email = user.Email,
            EmailVerified = user.EmailVerified,
            refreshToken = user.refreshToken,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = new RoleDto {
                RoleName = user.Role.RoleName
            }
        };

        return new AuthenticationResult { User = dto };
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordDTO request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return false;
        }

        var resetCode = new Random().Next(100000, 999999).ToString();
        user.PasswordResetCode = resetCode;
        user.PasswordResetCodeExpiry = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        SendResetPasswordEmail(user.Email, resetCode);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDTO request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || user.PasswordResetCode != request.ResetCode)
        {
            return false;
        }

        if (user.PasswordResetCodeExpiry < DateTime.UtcNow)
        {
            return false;
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetCode = null;
        user.PasswordResetCodeExpiry = null;
        await _context.SaveChangesAsync();

        return true;
    }
    
    
    private void SendResetPasswordEmail(string email, string code)
    {
        var fromAddress = new MailAddress(_config["Email:Username"], "Your App");
        var toAddress = new MailAddress(email);
        const string subject = "Reset Password";
        string body = $"Your reset code is: {code}";

        var smtp = new SmtpClient
        {
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            Credentials = new NetworkCredential(_config["Email:Username"], _config["Email:Password"])
        };

        using var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body,
        };
        smtp.Send(message);
    }
    
    public async Task<RefreshTokenDTO?> RefreshTokenAsync(string providedEncryptedRefreshToken)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.refreshToken == providedEncryptedRefreshToken);
        if (user == null)
        {
            return null; 
        }
            
        if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            return null; 
        }
            
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id.ToString(), user.Email, user.Role.RoleName);
            
        var newPlainRefreshToken = _jwtTokenService.GenerateRefreshToken();
            
        string encryptionKeyString = _config["Encryption:RefreshTokenKey"];
        byte[] encryptionKey = Encoding.UTF8.GetBytes(encryptionKeyString);
        var newEncryptedRefreshToken = AesEncryption.Encrypt(newPlainRefreshToken, encryptionKey);
            
        user.refreshToken = newEncryptedRefreshToken;
        user.RefreshTokenExpiryTime = _jwtTokenService.GetRefreshTokenExpiry();
        await _context.SaveChangesAsync();
            
        return new RefreshTokenDTO
        {
            AccessToken = newAccessToken,
            //EncryptedRefreshToken = newEncryptedRefreshToken
            EncryptedRefreshToken = user.refreshToken
        };
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        return await _context.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Email == email);
    }
}