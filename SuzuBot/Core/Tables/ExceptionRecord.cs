using SQLite;

namespace SuzuBot.Core.Tables;

[Table("t_exception_record")]
internal class ExceptionRecord
{
    [PrimaryKey]
    [Column("date")]
    public DateTime Date { get; set; }
    [Column("type")]
    public string Type { get; set; }
    [Column("message")]
    public string Message { get; set; }
}