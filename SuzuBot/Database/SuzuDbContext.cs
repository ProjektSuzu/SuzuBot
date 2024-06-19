using Microsoft.EntityFrameworkCore;
using SuzuBot.Database.Tables;

namespace SuzuBot.Database;

internal class SuzuDbContext(DbContextOptions<SuzuDbContext> options) : DbContext(options)
{
    public DbSet<UserInfo> UserInfos { get; set; }
    public DbSet<GroupRule> GroupRules { get; set; }
}
