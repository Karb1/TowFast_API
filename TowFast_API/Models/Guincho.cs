using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TowFast_API.Models
{
    public class AtualizaGuincho
    {
        [Key]
        [JsonIgnore]
        public Guid Id_Cliente { get; set; }
        [JsonIgnore]
        public Guid Id_Endereco { get; set; }
        [JsonIgnore]
        public Guid Id_Veiculo { get; set; }
        public string Nome { get; set; }
        public string Documento { get; set; }
        public string Cnh { get; set; }
        public string Telefone { get; set; }
        public int Status { get; set; }
        public DateTime Ultimo_Status { get; set; }
    }
}
