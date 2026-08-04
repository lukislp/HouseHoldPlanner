using Microsoft.EntityFrameworkCore;
using HaushaltsPlaner.Server.Data;
using HaushaltsPlaner.Shared.DTOs;

namespace HaushaltsPlaner.Server.Services;

public class ProfileService
{
    private readonly AppDbContext _context;
    private readonly string _uploadsPath;

    public ProfileService(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;

        // Get web root path or use default
        var webRootPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        _uploadsPath = Path.Combine(webRootPath, "uploads", "profiles");

        // Ensure uploads directory exists
        if (!Directory.Exists(_uploadsPath))
        {
            Directory.CreateDirectory(_uploadsPath);
        }
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Household)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            ProfileImageUrl = user.ProfileImageUrl,
            Role = user.Role,
            HouseholdName = user.Household?.Name,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.FullName = request.FullName;
        user.Email = request.Email;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UploadProfileImageResponse> UploadProfileImageAsync(int userId, IFormFile file)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new UploadProfileImageResponse
                {
                    Success = false,
                    ErrorMessage = "Benutzer nicht gefunden"
                };
            }

            // Validate file
            if (file.Length > 5 * 1024 * 1024) // 5MB limit
            {
                return new UploadProfileImageResponse
                {
                    Success = false,
                    ErrorMessage = "File is too large (max. 5MB)"
                };
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return new UploadProfileImageResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid file format. Allowed: JPG, PNG, GIF"
                };
            }

            // Delete old profile image if exists
            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                var oldImagePath = Path.Combine(_uploadsPath, Path.GetFileName(user.ProfileImageUrl));
                if (File.Exists(oldImagePath))
                {
                    File.Delete(oldImagePath);
                }
            }

            // Generate unique filename
            var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadsPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update user profile
            user.ProfileImageUrl = $"/uploads/profiles/{fileName}";
            await _context.SaveChangesAsync();

            return new UploadProfileImageResponse
            {
                Success = true,
                ImageUrl = user.ProfileImageUrl
            };
        }
        catch (Exception ex)
        {
            return new UploadProfileImageResponse
            {
                Success = false,
                ErrorMessage = $"Fehler beim Hochladen: {ex.Message}"
            };
        }
    }

    public async Task<bool> DeleteProfileImageAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.ProfileImageUrl)) return false;

        // Delete file
        var imagePath = Path.Combine(_uploadsPath, Path.GetFileName(user.ProfileImageUrl));
        if (File.Exists(imagePath))
        {
            File.Delete(imagePath);
        }

        // Update database
        user.ProfileImageUrl = null;
        await _context.SaveChangesAsync();
        return true;
    }
}
