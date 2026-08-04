using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HaushaltsPlaner.Server.Data;
using HaushaltsPlaner.Shared.DTOs;
using HaushaltsPlaner.Shared.Models;

namespace HaushaltsPlaner.Server.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return new AuthResponse { Success = false, Message = "Username already exists" };
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return new AuthResponse { Success = false, Message = "Email already exists" };
            }

            // Find or create household
            var household = await _context.Households
         .FirstOrDefaultAsync(h => h.Name == request.HouseholdName);

            if (household == null)
            {
                household = new Household
                {
                    Name = request.HouseholdName,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Households.Add(household);
                await _context.SaveChangesAsync();
            }

            // Create user
            var user = new User
            {
                Username = request.Username,
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                HouseholdId = household.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user, household.Name);

            return new AuthResponse
            {
                Success = true,
                Token = token,
                Message = "Registration successful",
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    ProfileImageUrl = user.ProfileImageUrl,
                    HouseholdName = household.Name
                }
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = $"Registration failed: {ex.Message}" };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _context.Users
         .Include(u => u.Household)
           .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResponse { Success = false, Message = "Invalid username or password" };
            }

            var token = GenerateJwtToken(user, user.Household?.Name ?? "");
            return new AuthResponse
            {
                Success = true,
                Token = token,
                Message = "Login successful",
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    ProfileImageUrl = user.ProfileImageUrl,
                    HouseholdName = user.Household?.Name
                }
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = $"Login failed: {ex.Message}" };
        }
    }

    private string GenerateJwtToken(User user, string householdName)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key must be configured (e.g. via the Jwt__Key environment variable).");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
       new Claim(ClaimTypes.Name, user.Username),
            new Claim("FullName", user.FullName),
    new Claim(ClaimTypes.Email, user.Email),
     new Claim("HouseholdName", householdName),
     new Claim("HouseholdId", user.HouseholdId?.ToString() ?? "0")
        };

        var token = new JwtSecurityToken(
      issuer: _configuration["Jwt:Issuer"] ?? "HaushaltsPlaner",
         audience: _configuration["Jwt:Audience"] ?? "HaushaltsPlaner",
     claims: claims,
        expires: DateTime.UtcNow.AddDays(7),
           signingCredentials: credentials
          );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
