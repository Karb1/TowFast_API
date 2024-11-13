using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TowFast_API.Models
{
    public class Veiculo
    {
        [Key]
        [JsonIgnore]
        public Guid Id_Veiculo {get; set;}
        public string Modelo {get; set;}
        public string Placa {get; set;}
    }
}
