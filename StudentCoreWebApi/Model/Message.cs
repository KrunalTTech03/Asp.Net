namespace StudentCoreWebApi.Model
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SendAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public bool IsRead { get; set; } = false;
        public string? Reaction { get; set; }
    }
}