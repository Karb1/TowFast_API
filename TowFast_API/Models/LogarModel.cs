using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TowFast_API.Models
{
    public class LogarModel
    {
        [Key]
        [JsonIgnore]
        public Guid Id_Cliente { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string tipo { get; set; }

    }
}
