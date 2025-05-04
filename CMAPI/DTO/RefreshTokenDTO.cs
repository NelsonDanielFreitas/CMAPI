namespace CMAPI.DTO;

public class RefreshTokenDTO
{
    public string AccessToken { get; set; }
    public string EncryptedRefreshToken { get; set; }
}