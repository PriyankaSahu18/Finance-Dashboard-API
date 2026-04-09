using Microsoft.AspNetCore.Mvc;
using FinanceDashboard.Data;
using FinanceDashboard.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinanceDashboard.DTOs;


namespace FinanceDashboard.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string key = "ThisIsASecretKey12345ThisIsASecretKey12345";

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public IActionResult Register(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role
            });
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto login)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Email == login.Email && u.Password == login.Password);

            if (user == null)
                return Unauthorized("Invalid credentials");

            if (!user.IsActive)
                return Unauthorized("User is inactive");

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.UTF8.GetBytes(key);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(tokenKey),
                    SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new
            {
                token = tokenHandler.WriteToken(token)
            });
        }
    }
}