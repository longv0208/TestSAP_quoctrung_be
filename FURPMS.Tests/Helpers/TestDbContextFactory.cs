using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Helpers;

public static class TestDbContextFactory
{
    public static FURPMSDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<FURPMSDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new FURPMSDbContext(options);
    }
}
