using SQLite;
using SuzuBot.Core.Databases.Tables;

namespace SuzuBot.Core.Databases;
internal class DataBaseManager : BaseManager
{
    private readonly string _dbPath;
    private readonly SQLiteAsyncConnection _connection;

    public SQLiteAsyncConnection Connection => _connection;

    internal DataBaseManager(Context context) : base(context)
    {
        _dbPath = Path.Combine("databases", "suzubot.db");
        _connection = new(_dbPath);

        // Create tables if not exist
        _connection.CreateTableAsync<SuzuUserInfo>().Wait();
        _connection.CreateTableAsync<SuzuAuthGroup>().Wait();
        _connection.CreateTableAsync<ExecutionRecord>().Wait();
        _connection.CreateTableAsync<ExceptionRecord>().Wait();
    }

    public async Task<SuzuUserInfo> GetUserInfo(uint uin)
    {
        var info = await Connection.FindAsync<SuzuUserInfo>(uin);
        if (info == null)
        {
            info = new()
            { Uin = uin };
            await Connection.InsertOrReplaceAsync(info);
        }
        return info;
    }

    public async Task<bool> UpdateUserInfo(SuzuUserInfo info)
        => await Connection.UpdateAsync(info) > 0;
}