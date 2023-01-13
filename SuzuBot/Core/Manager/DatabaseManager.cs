using SQLite;
using SuzuBot.Core.Tables;

namespace SuzuBot.Core.Manager;

public class DatabaseManager : BaseManager
{
    private readonly string _dbPath;
    public SQLiteAsyncConnection Connection { get; private set; }

    public DatabaseManager(Context context) : base(context)
    {
        _dbPath = Path.Combine(Context.DatabaseDirectory, "suzubot.db");
        Connection = new(_dbPath);

        // Create tables if not exist
        Connection.CreateTableAsync<SuzuUserInfo>().Wait();
        Connection.CreateTableAsync<SuzuGroupInfo>().Wait();
        Connection.CreateTableAsync<ExecutionRecord>().Wait();
        Connection.CreateTableAsync<ExceptionRecord>().Wait();
    }

    public SuzuUserInfo GetUserInfo(uint uin)
    {
        var info = Connection
            .Table<SuzuUserInfo>()
            .Where(x => x.Uin == uin)
            .FirstOrDefaultAsync().Result;

        if (info is null)
        {
            info = new()
            {
                Uin = uin
            };
            Connection.InsertAsync(info);
            return info;
        }
        else
        {
            return info;
        }
    }
    public bool UpdateUserInfo(SuzuUserInfo info)
        => Connection.UpdateAsync(info).Result > 0;
    public SuzuGroupInfo GetGroupInfo(uint uin)
    {
        var info = Connection
            .Table<SuzuGroupInfo>()
            .Where(x => x.Uin == uin)
            .FirstOrDefaultAsync().Result;

        if (info is null)
        {
            info = new()
            {
                Uin = uin
            };
            Connection.InsertAsync(info);
            return info;
        }
        else
        {
            return info;
        }
    }
    public bool UpdateGroupInfo(SuzuGroupInfo info)
        => Connection.UpdateAsync(info).Result > 0;
}