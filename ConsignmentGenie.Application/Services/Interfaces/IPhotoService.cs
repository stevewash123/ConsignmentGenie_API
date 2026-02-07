using ConsignmentGenie.Application.Models;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IPhotoService
{
    Task<string> UploadPhotoAsync(Guid organizationId, Guid itemId, Stream fileStream, string fileName);
    Task<bool> DeletePhotoAsync(Guid organizationId, string photoUrl);
    Task<string> GenerateThumbnailAsync(string photoUrl);
    Task<List<PhotoInfo>> GetPhotosAsync(Guid itemId);
}