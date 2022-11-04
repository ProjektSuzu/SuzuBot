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
    }
}