using System.Security.Cryptography;
using System.Text;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsignmentGenie.Application.Services;

public interface IFileUploadTrackingService
{
    Task<FileUploadDuplicateResult> CheckForDuplicateAsync(Guid organizationId, string fileName, string fileType, string firstDataRow, int rowCount);
    Task SaveFileHashAsync(Guid organizationId, string fileName, string fileType, string firstDataRow, int rowCount);
}

public class FileUploadDuplicateResult
{
    public bool IsDuplicate { get; set; }
    public DateTime? LastUploadDate { get; set; }
    public string? LastFileName { get; set; }
    public string Hash { get; set; } = string.Empty;
}

public class FileUploadTrackingService : IFileUploadTrackingService
{
    private readonly ConsignmentGenieContext _context;

    public FileUploadTrackingService(ConsignmentGenieContext context)
    {
        _context = context;
    }

    public async Task<FileUploadDuplicateResult> CheckForDuplicateAsync(Guid organizationId, string fileName, string fileType, string firstDataRow, int rowCount)
    {
        var hash = GenerateHash(organizationId, firstDataRow);

        var existingHash = await _context.FileUploadHashes
            .Where(f => f.Hash == hash)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync();

        return new FileUploadDuplicateResult
        {
            IsDuplicate = existingHash != null,
            LastUploadDate = existingHash?.CreatedAt,
            LastFileName = existingHash?.FileName,
            Hash = hash
        };
    }

    public async Task SaveFileHashAsync(Guid organizationId, string fileName, string fileType, string firstDataRow, int rowCount)
    {
        var hash = GenerateHash(organizationId, firstDataRow);

        // Check if this exact hash already exists (shouldn't happen if we checked first, but being safe)
        var existing = await _context.FileUploadHashes
            .FirstOrDefaultAsync(f => f.Hash == hash);

        if (existing != null)
        {
            // Update the existing record with new file info
            existing.FileName = fileName;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new hash record
            var fileHash = new FileUploadHash
            {
                OrganizationId = organizationId,
                Hash = hash,
                FileName = fileName,
                FileType = fileType,
                FirstRowSample = firstDataRow.Length > 500 ? firstDataRow[..500] + "..." : firstDataRow,
                RowCount = rowCount
            };

            _context.FileUploadHashes.Add(fileHash);
        }

        await _context.SaveChangesAsync();
    }

    private static string GenerateHash(Guid organizationId, string firstDataRow)
    {
        // Combine organization ID and first data row for unique hash per organization
        var combined = $"{organizationId}|{firstDataRow}";
        var bytes = Encoding.UTF8.GetBytes(combined);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(bytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}