using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BitByBitTrashAPI.Service;

public class LitterDbContext : IdentityDbContext
{
    public LitterDbContext(DbContextOptions<LitterDbContext> options) : base(options)
    {
    }

    public DbSet<BitByBitTrashAPI.Models.TrashPickup> LitterModels { get; set; }
}
