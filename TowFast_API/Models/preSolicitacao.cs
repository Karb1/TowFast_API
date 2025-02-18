using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TowFast_API.Models
{
    public class preSolicitacao
    {
        [Key]
        [JsonIgnore]
        public int Id_Solicitacao { get; set; }
        public Guid Id_Motorista { get; set; }
        public Guid Id_Guincho { get; set; }
        public string Distancia { get; set; }
        public string Preco { get; set; }
        public string LatLongCliente { get; set; }
        public string LatLongGuincho { get; set; }
        public string Status { get; set; }
        [JsonIgnore]
        public DateTime Dta_Solicitacao { get; set; } = DateTime.Now;
    }
}
