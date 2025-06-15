using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BitByBitTrashAPI.Service;

public class LitterDbContext : IdentityDbContext
{
    public LitterDbContext(DbContextOptions<LitterDbContext> options) : base(options)
    {
    }

    public DbSet<BitByBitTrashAPI.Models.TrashPickup> LitterModels { get; set; }
    // No change needed for Confidence, EF Core will map the new property automatically after migration
}
