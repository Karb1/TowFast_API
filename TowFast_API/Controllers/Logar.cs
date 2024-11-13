using TowFast_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TowFast_API.Context;
using BCrypt.Net;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

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

        [HttpPost("login")] // Endpoint para login completo com verificação de senha
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.Email.ToLower(), request.Email.ToLower()));

            if (user != null && BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return Ok(new LoginResponse
                {
                    Id = user.Id_Cliente,
                    Email = user.Email,
                    Message = "Login bem-sucedido!",
                    Tipo = user.tipo
                });
            }

            return Unauthorized(new { Message = "Usuário ou senha incorretos." });
        }

        [HttpPost("user")] // Endpoint para verificar existência do usuário sem senha
        public async Task<ActionResult> CheckUserExists([FromBody] UsernameRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username))
            {
                return BadRequest("Username is required.");
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.Email.ToLower(), request.Username.ToLower()));

            if (user != null)
            {
                return Ok(new { Id = user.Id_Cliente, Message = "Usuário encontrado. Pode prosseguir." });
            }

            return NotFound(new { Message = "Usuário não encontrado." });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] ModelGeral registerModel)
        {
            if (registerModel == null || string.IsNullOrEmpty(registerModel.Email) || string.IsNullOrEmpty(registerModel.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.Email.ToLower(), registerModel.Email.ToLower()));
            if (existingUser != null)
            {
                return BadRequest("Username already exists.");
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerModel.Password);

            var user = new LogarModel
            {
                Id_Cliente = Guid.NewGuid(),
                Password = hashedPassword,
                Email = registerModel.Email,
                tipo = registerModel.tipo,
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Salva na DM_VEICULO

            var veiculo = new Veiculo
            {
                Id_Veiculo = Guid.NewGuid(),
                Modelo = registerModel.Modelo,
                Placa = registerModel.LicensePlate
            };
            _dbContext.Veiculo.Add(veiculo);
            await _dbContext.SaveChangesAsync();

            // Salva na DM_ENDERECO

            var endereco = new Endereco
            {
                Id_Endereco = Guid.NewGuid(),
                Local_real_time = ""
            };
            _dbContext.Endereco.Add(endereco);
            await _dbContext.SaveChangesAsync();


            // Depois de salvar o usuário, verifique o tipo e insira manualmente nas tabelas Cliente ou Guincho
            if (user.tipo == "Motorista")
            {
                var cliente = new AtualizaCliente
                {
                    Id_Cliente = user.Id_Cliente,
                    Id_Veiculo = veiculo.Id_Veiculo,
                    Id_Endereco = endereco.Id_Endereco,
                    Nome = registerModel.Username,
                    DtNasc = registerModel.BirthDate,
                    Idade = DateTime.Now.Year - registerModel.BirthDate.Year,
                    DocumentoCliente = registerModel.CPF_CNPJ,
                    Telefone = registerModel.Phone
                };
                _dbContext.Cliente.Add(cliente);
                await _dbContext.SaveChangesAsync();
            }
            else if (user.tipo == "Guincho")
            {
                var guincho = new AtualizaGuincho
                {
                    Id_Cliente = user.Id_Cliente,
                    Id_Veiculo = veiculo.Id_Veiculo,
                    Id_Endereco = endereco.Id_Endereco,
                    Nome = registerModel.Username,
                    Documento = registerModel.CPF_CNPJ,
                    Telefone = registerModel.Phone,
                    Cnh = registerModel.cnh
                };
                _dbContext.Guincho.Add(guincho);
                await _dbContext.SaveChangesAsync();
            }

            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { id = user.Id_Cliente }, user);
        }

        [HttpPut("updatePassword")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.NewPassword))
            {
                return BadRequest("Username and new password are required.");
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.Email.ToLower(), model.Username.ToLower()));
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

        [HttpPut("atualizarlocalrealtime")]
        public IActionResult AtualizarLocalRealTime([FromBody] Endereco request)
        {
            // Verificar se o Id_Endereco existe na tabela
            var endereco = _dbContext.Endereco.FirstOrDefault(e => e.Id_Endereco == request.Id_Endereco);

            if (endereco == null)
            {
                // Se o endereço não for encontrado, retorna um erro
                return NotFound("Endereço não encontrado.");
            }

            // Verifica se o Local_real_time não é nulo antes de atualizar
            if (request.Local_real_time != null)
            {
                endereco.Local_real_time = request.Local_real_time;
            }
            else
            {
                // Caso o valor enviado seja nulo, pode-se deixar o campo como null, se necessário
                endereco.Local_real_time = null;
            }

            // Salvar as alterações no banco
            _dbContext.SaveChanges();

            // Retornar sucesso
            return Ok("Local_real_time atualizado com sucesso.");
        }


    }

    // Models for requests
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // Model for checking user existence
    public class UsernameRequest
    {
        public string Username { get; set; }
    }

    // Response model
    public class LoginResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public string Tipo { get; set; }
    }


    public class ModelGeral
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string CPF_CNPJ { get; set; }
        public string LicensePlate { get; set; }
        public string Modelo { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime BirthDate { get; set; }
        public string cnh { get; set; }
        public string tipo { get; set; }

    }

}
