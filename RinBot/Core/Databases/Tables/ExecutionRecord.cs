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
    [Column("receiver_id")]
    public uint ReceiverId { get; set; }
    [Column("receiver_name")]
    public string ReceiverName { get; set; }
    [Column("command")]
    public string Command { get; set; }
    [Column("message")]
    public string Message { get; set; }
    [Column("result")]
    public CommandExecuteResult Result { get; set; }
}
