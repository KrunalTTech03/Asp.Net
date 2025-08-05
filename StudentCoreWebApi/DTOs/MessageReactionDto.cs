namespace StudentCoreWebApi.DTOs
{
    public class MessageReactionDto
    {
        public Guid MessageId { get; set; }
        public string Emoji { get; set; } = string.Empty;
    }
}
