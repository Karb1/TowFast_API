using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TowFast_API.Models
{
    public class AtualizaCliente                
    {
        [Key]
        [JsonIgnore]
        public Guid Id_Cliente {  get; set; }
        [JsonIgnore]
        public Guid Id_Endereco { get; set; }
        [JsonIgnore]
        public Guid Id_Veiculo { get; set; }
        public string Nome { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DtNasc { get; set; }
        public int Idade { get; set; }
        public string DocumentoCliente { get; set; }
        public string Telefone { get; set; }

    }
}
