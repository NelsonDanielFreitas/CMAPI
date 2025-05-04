namespace CMAPI.Services;

public interface IPhotoService
{
    Task<string> SaveBase64ImageAsync(string base64);
}