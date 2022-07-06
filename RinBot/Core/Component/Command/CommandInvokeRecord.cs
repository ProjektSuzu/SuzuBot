using RinBot.Core.Component.Event;
using SQLite;

namespace RinBot.Core.Component.Command
{
    [Table("T_CMD_INVOKE")]
    internal class CommandInvokeRecord
    {
        [PrimaryKey]
        [AutoIncrement]
        [Column("id")]
        public long Id { get; set; }
        [Column("module")]
        public string Module { get; set; }
        [Column("command")]
        public string Command { get; set; }
        [Column("source_type")]
        public EventSourceType SourceType { get; set; }
        [Column("sender_id")]
        public string SenderId { get; set; }
        [Column("subject_id")]
        public string SubjectId { get; set; }
        [Column("message_content")]
        public string MessageContent { get; set; }
        [Column("is_invoked")]
        public bool IsInvoked { get; set; }
        [Column("date")]
        public DateTime Date { get; set; }
    }
}
