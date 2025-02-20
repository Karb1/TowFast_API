using TowFast_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TowFast_API.Context;
using BCrypt.Net;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace TowFast_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogarController : ControllerBase
    {
        private readonly TowFastDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public LogarController(TowFastDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        // Endpoint para login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Email e senha são obrigatórios.");
            }

            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.Email.ToLower(), request.Email.ToLower()));

            if (user != null && BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                if (user.tipo == "Motorista")
                {
                    var motorista = await _dbContext.Cliente
                        .AsNoTracking()
                        .Where(c => c.Id_Cliente == user.Id_Cliente)
                        .FirstOrDefaultAsync();

                    if (motorista == null)
                        return NotFound("Motorista não encontrado.");

                    return Ok(new LoginResponse
                    {
                        Id = user.Id_Cliente,
                        Id_Endereco = motorista.Id_Endereco,
                        Id_Veiculo = motorista.Id_Veiculo,
                        Email = user.Email,
                        Message = "Login bem-sucedido!",
                        Tipo = user.tipo,
                    });
                }
                else if (user.tipo == "Guincho")
                {
                    var guincho = await _dbContext.Guincho
                        .AsNoTracking()
                        .Where(g => g.Id_Cliente == user.Id_Cliente)
                        .FirstOrDefaultAsync();

                    if (guincho == null)
                        return NotFound("Guincho não encontrado.");

                    return Ok(new LoginResponse
                    {
                        Id = user.Id_Cliente,
                        Id_Endereco = guincho.Id_Endereco,
                        Id_Veiculo = guincho.Id_Veiculo,
                        Email = user.Email,
                        Message = "Login bem-sucedido!",
                        Tipo = user.tipo,
                    });
                }
            }

            return Unauthorized(new { Message = "Usuário ou senha incorretos." });
        }

        // Endpoint para verificar existência de usuário
        [HttpPost("user")]
        public async Task<ActionResult> CheckUserExists([FromBody] UsernameRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username))
            {
                return BadRequest("O email do usuário é obrigatório.");
            }

            var userExists = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(x => EF.Functions.Like(x.Email.ToLower(), request.Username.ToLower()));

            if (userExists)
            {
                return Ok(new { Message = "Usuário encontrado. Pode prosseguir." });
            }

            return NotFound(new { Message = "Usuário não encontrado." });
        }

        // Endpoint para registrar usuário
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] ModelGeral registerModel)
        {
            if (registerModel == null || string.IsNullOrEmpty(registerModel.Email) || string.IsNullOrEmpty(registerModel.Password))
            {
                return BadRequest("Email e senha são obrigatórios.");
            }

            var existingUser = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(x => EF.Functions.Like(x.Email.ToLower(), registerModel.Email.ToLower()));

            if (existingUser)
            {
                return BadRequest("Usuário já existe.");
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

            var veiculo = new Veiculo
            {
                Id_Veiculo = Guid.NewGuid(),
                Modelo = registerModel.modelo,
                Placa = registerModel.LicensePlate
            };
            await _dbContext.Veiculo.AddAsync(veiculo);
            _dbContext.SaveChanges();

            var endereco = new Endereco
            {
                Id_Endereco = Guid.NewGuid(),
                Local_real_time = "",
                Lat_long = ""
            };
            await _dbContext.Endereco.AddAsync(endereco);
            _dbContext.SaveChanges();

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

                await _dbContext.Cliente.AddAsync(cliente);
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
                    Cnh = registerModel.cnh,
                    Ultimo_Status = DateTime.Now,
                    Status = 0
                };

                await _dbContext.Guincho.AddAsync(guincho);
            }

            await _dbContext.SaveChangesAsync();

            return Ok("Registrado com sucesso.");
        }

        // Atualizar senha do usuário
        [HttpPut("updatePassword")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.NewPassword))
            {
                return BadRequest("Email e nova senha são obrigatórios.");
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.Email.ToLower(), model.Username.ToLower()));

            if (user == null)
            {
                return NotFound("Usuário não encontrado.");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            _dbContext.Entry(user).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();

            return Ok("Senha atualizada com sucesso.");
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
                endereco.Lat_long = request.Lat_long;
            }
            else
            {
                // Caso o valor enviado seja nulo, pode-se deixar o campo como null, se necessário
                endereco.Local_real_time = "";
            }

            // Salvar as alterações no banco
            _dbContext.SaveChanges();

            // Retornar sucesso
            return Ok("Local_real_time atualizado com sucesso.");
        }

        [HttpPut("AtualizaStatusGuincho")]
        public IActionResult StatusGuincho([FromBody] AtualizaStatusGuincho request)
        {
            var guincho = _dbContext.Guincho.FirstOrDefault(e => e.Id_Cliente == request.Id_cliente);

            if(guincho == null)
            {
                return NotFound("Cliente nao encontrado");
            }

            if(request.Status != null)
            {
                guincho.Status = request.Status;
                guincho.Ultimo_Status = DateTime.Now;
            }
            else
            {
                guincho.Status = guincho.Status;
                guincho.Ultimo_Status = guincho.Ultimo_Status;
            }

            _dbContext.SaveChanges();
            return Ok("Status atualizado");
        }

        [HttpPost("preSolicitacao")]
        public IActionResult preSoli([FromBody] preSolicitacao request)
        {
            if (request == null)
            {
                return BadRequest("A solicitação não pode ser nula.");
            }

            var solicitacao = new preSolicitacao
            {
                Id_Solicitacao = Guid.NewGuid(),
                Id_Motorista = request.Id_Motorista,
                Id_Guincho = request.Id_Guincho,
                Distancia = request.Distancia,
                Preco = request.Preco,
                LatLongCliente = request.LatLongCliente,
                LatLongGuincho = request.LatLongGuincho,
                Status = request.Status,
                Dta_Solicitacao = request.Dta_Solicitacao,
            };

            try
            {
                _dbContext.preSolicitacao.Add(request);
                _dbContext.SaveChanges();
                return Ok(new { message = "Solicitação registrada com sucesso."});
            }
            catch (Exception ex)
            {
                var innerExceptionMessage = ex.InnerException != null ? ex.InnerException.Message : "Sem detalhes da exceção interna.";
                return StatusCode(500, new { message = "Erro ao registrar solicitação.", error = ex.Message, innerError = innerExceptionMessage });
            }
        }


        [HttpGet]
        [Route("GetGuinchosAtivos")]
        public IActionResult GetGuinchosAtivos()
        {
            try
            {
                // Obter a string de conexão do appsettings.json
                string connectionString = _configuration.GetConnectionString("SqlTowFast");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("GetGuinchosAtivos", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            var result = new List<object>();

                            while (reader.Read())
                            {
                                result.Add(new
                                {
                                    Nome = reader["Nome"].ToString(),
                                    Telefone = reader["Telefone"].ToString(),
                                    Modelo = reader["Modelo"].ToString(),
                                    Lat_long = reader["Lat_long"].ToString()
                                });
                            }

                            return Ok(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao buscar guinchos ativos.", error = ex.Message });
            }
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
        public Guid Id_Endereco {  get; set; }
        public Guid Id_Veiculo { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public string Tipo { get; set; }
    }

    public class AtualizaStatusGuincho
    {
        public Guid Id_cliente { get; set; }
        public int Status { get; set; }
        public DateTime UltimoStatus { get; set; }
    }

    public class ModelGeral
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string CPF_CNPJ { get; set; }
        public string LicensePlate { get; set; }
        public string modelo { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime BirthDate { get; set; }
        public string cnh { get; set; }
        public string tipo { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [JsonIgnore]
        public DateTime UltimoStatus { get; set; }
        [JsonIgnore]
        public int Status { get; set; }

    }

}
