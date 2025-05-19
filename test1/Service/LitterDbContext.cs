using Microsoft.EntityFrameworkCore;

namespace test1.Service;
public class LitterDbContext : DbContext
{
    public LitterDbContext(DbContextOptions<LitterDbContext> options) : base(options)
    {
    }
    public DbSet<test1.models.LitterModel> LitterModels { get; set; }
}