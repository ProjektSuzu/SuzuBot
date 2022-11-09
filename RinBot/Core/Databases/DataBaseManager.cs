using RinBot.Core.Databases.Tables;
using SQLite;

namespace RinBot.Core.Databases;
internal class DataBaseManager : BaseManager
{
    private readonly string _dbPath;
    private readonly SQLiteAsyncConnection _connection;

    public SQLiteAsyncConnection Connection => _connection;

    internal DataBaseManager(Context context) : base(context)
    {
        _dbPath = Path.Combine("databases", "rinbot.db");
        _connection = new(_dbPath);

        // Create tables if not exist
        _connection.CreateTableAsync<RinUserInfo>().Wait();
        _connection.CreateTableAsync<RinAuthGroup>().Wait();
        _connection.CreateTableAsync<ExecutionRecord>().Wait();
        _connection.CreateTableAsync<ExceptionRecord>().Wait();
    }

    public async Task<RinUserInfo> GetUserInfo(uint uin)
    {
        var info = await Connection.FindAsync<RinUserInfo>(uin);
        if (info == null)
        {
            info = new()
            { Uin = uin };
            await Connection.InsertOrReplaceAsync(info);
        }
        return info;
    }

    public async Task<bool> UpdateUserInfo(RinUserInfo info)
        => await Connection.UpdateAsync(info) > 0;
}