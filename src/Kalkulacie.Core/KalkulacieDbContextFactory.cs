using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kalkulacie.Core;

public class KalkulacieDbContextFactory : IDesignTimeDbContextFactory<KalkulacieDbContext>
{
    public KalkulacieDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<KalkulacieDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new KalkulacieDbContext(options);
    }
}
