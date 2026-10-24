using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Noctra.Data;
using Noctra.Models;

namespace Noctra.Services;

public class AccountService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    private readonly AppDbContext _db;

    public AccountService(AppDbContext db) => _db = db;

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;
        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    /// <summary>Проверка пароля: не короче 8 символов, есть буквы и цифры.</summary>
    public static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return "Password must be at least 8 characters";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "Password must contain letters and digits";
        return null;
    }

    public async Task<(User? user, string? error)> RegisterAsync(string name, string email, string phone, string password)
    {
        email = email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return (null, "An account with this email already exists");

        var user = new User { Name = name.Trim(), Email = email, Phone = phone.Trim(), PasswordHash = HashPassword(password) };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return (user, null);
    }

    /// <summary>Вход по имени пользователя, почте и паролю (как на макете).</summary>
    public async Task<User?> CheckCredentialsAsync(string name, string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !string.Equals(user.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)) return null;
        return VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    public static Task SignInAsync(HttpContext http, User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }

    public static Task SignOutAsync(HttpContext http) =>
        http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id текущего пользователя или null, если он не вошёл.</summary>
    public static int? GetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
