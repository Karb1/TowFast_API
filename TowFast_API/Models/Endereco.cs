using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TowFast_API.Models
{
    public class Endereco
    {
        [Key]
        public Guid Id_Endereco { get; set; }
        public string Local_real_time { get; set; }
        public string Lat_long { get; set; }
    }

}
