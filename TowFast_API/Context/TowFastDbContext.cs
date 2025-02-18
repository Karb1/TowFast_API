using Microsoft.EntityFrameworkCore;
using TowFast_API.Models;

namespace TowFast_API.Context
{
    public class TowFastDbContext : DbContext
    {
        public TowFastDbContext(DbContextOptions<TowFastDbContext> options)
            : base(options)
        {
        }

        public DbSet<LogarModel> Users { get; set; }
        public DbSet<Endereco> Endereco { get; set; }
        public DbSet<AtualizaCliente> Cliente { get; set; }
        public DbSet<AtualizaGuincho> Guincho { get; set; }
        public DbSet<Veiculo> Veiculo { get; set; }
        public DbSet<preSolicitacao> preSolicitacao { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
