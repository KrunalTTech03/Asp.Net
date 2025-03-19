namespace StudentCoreWebApi.DTOs
{
    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; } = null;
        public string? LastName { get; set; } = null;
        public string Email { get; set; }
        public long? Phone { get; set; } = null;
        public string Password { get; set; }
        public bool? IsDeleted { get; set; } = null;
    }
}