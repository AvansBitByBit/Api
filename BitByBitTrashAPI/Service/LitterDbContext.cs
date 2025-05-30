using Microsoft.EntityFrameworkCore;

namespace BitByBitTrashAPI.Service;
public class LitterDbContext : DbContext
{
    public LitterDbContext(DbContextOptions<LitterDbContext> options) : base(options)
    {
    }
    public DbSet<BitByBitTrashAPI.Models.LitterModel> LitterModels { get; set; }
}