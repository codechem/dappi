using Dappi.HeadlessCms.Database;
using Microsoft.EntityFrameworkCore;

namespace Dappi.TestEnv.Data
{
    public class TestDbContext(DbContextOptions options) : DappiDbContext(options)
    {
        
    }
}