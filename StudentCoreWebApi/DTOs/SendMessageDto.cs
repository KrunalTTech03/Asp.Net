namespace StudentCoreWebApi.DTOs
{
    public class SendMessageDto
    {
        public Guid ReceiverId { get; set; }
        public string Content { get; set; }
    }
}