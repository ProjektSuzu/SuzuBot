using SQLite;

namespace RinBot.Core.Databases.Tables;

public enum CommandExecuteResult
{
    Success,
    AuthFail,
    Error
}

[Table("t_execution_record")]
internal class ExecutionRecord
{
    [Column("date")]
    public DateTime Date { get; set; }
    [Column("id")]
    public uint Id { get; set; }
    [Column("name")]
    public string Name { get; set; }
    [Column("group_id")]
    public uint GroupId { get; set; }
    [Column("group_name")]
    public string GroupName { get; set; }
    [Column("command")]
    public string Command { get; set; }
    [Column("message")]
    public string Message { get; set; }
    [Column("result")]
    public CommandExecuteResult Result { get; set; }
}
