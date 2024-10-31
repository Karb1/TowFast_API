using TowFast_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TowFast_API.Context;
using BCrypt.Net;

namespace TowFast_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogarController : ControllerBase
    {
        private readonly TowFastDbContext _dbContext;

        public LogarController(TowFastDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("login")] // Alterado para POST
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.Username.ToLower(), request.Username.ToLower()));

            if (user != null && BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return Ok(new LoginResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Message = "Login bem-sucedido!"
                });
            }

            return Unauthorized(new { Message = "Usuário ou senha incorretos." });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] LogarModel registerModel)
        {
            if (registerModel == null || string.IsNullOrEmpty(registerModel.Username) || string.IsNullOrEmpty(registerModel.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.Username.ToLower(), registerModel.Username.ToLower()));
            if (existingUser != null)
            {
                return BadRequest("Username already exists.");
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerModel.Password);

            var user = new LogarModel
            {
                Username = registerModel.Username,
                Password = hashedPassword,
                Email = registerModel.Email,
                Phone = registerModel.Phone,
                CPF_CNPJ = registerModel.CPF_CNPJ,
                LicensePlate = registerModel.LicensePlate,
                BirthDate = registerModel.BirthDate
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
        }

        [HttpPut("updatePassword")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.NewPassword))
            {
                return BadRequest("Username and new password are required.");
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.Username.ToLower(), model.Username.ToLower()));
            if (user == null)
            {
                return NotFound("User not found.");
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.Password = hashedPassword;

            _dbContext.Entry(user).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();

            return Ok("Password updated successfully.");
        }
    }

    // Models for requests
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    // Response model
    public class LoginResponse
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
    }
}
