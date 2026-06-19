using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RunApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly VerificationService _verification;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext db,
        VerificationService verification,
        IConfiguration config,
        ILogger<AuthController> logger)
    {
        _db = db;
        _verification = verification;
        _config = config;
        _logger = logger;
    }

    // POST api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { error = "Email already registered" });

        var user = new User
        {
            Name = req.Name,
            Email = req.Email,
            Phone = req.Phone ?? "",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            IsVerified = false
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Send verification code
        await _verification.GenerateAndSendCodeAsync(
            user.Id, user.Email, VerificationType.EmailLogin);

        _logger.LogInformation("User registered: {Email}", req.Email);

        return Ok(new 
        { 
            message = "Verification code sent to your email",
            userId = user.Id,
            requiresVerification = true
        });
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid credentials" });

        // Always require 2FA verification code
        await _verification.GenerateAndSendCodeAsync(
            user.Id, user.Email, VerificationType.EmailLogin);

        return Ok(new
        {
            message = "Verification code sent",
            userId = user.Id,
            requiresVerification = true
        });
    }

    // POST api/auth/verify
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyRequest req)
    {
        var isValid = await _verification.VerifyCodeAsync(
            req.UserId, req.Code, VerificationType.EmailLogin);

        if (!isValid)
            return BadRequest(new { error = "Invalid or expired code" });

        var user = await _db.Users.FindAsync(req.UserId);
        if (user == null) return NotFound();

        user.IsVerified = true;
        user.VerifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Generate JWT
        var token = GenerateJwtToken(user);

        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.Name,
                user.Email,
                user.IsVerified
            }
        });
    }

    // POST api/auth/resend-code
    [HttpPost("resend-code")]
    public async Task<IActionResult> ResendCode([FromBody] ResendRequest req)
    {
        var user = await _db.Users.FindAsync(req.UserId);
        if (user == null) return NotFound();

        await _verification.GenerateAndSendCodeAsync(
            user.Id, user.Email, VerificationType.EmailLogin);

        return Ok(new { message = "New code sent" });
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("verified", user.IsVerified.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// DTOs
public record RegisterRequest(string Name, string Email, string Password, string? Phone);
public record LoginRequest(string Email, string Password);
public record VerifyRequest(int UserId, string Code);
public record ResendRequest(int UserId);