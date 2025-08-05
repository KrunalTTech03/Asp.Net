namespace StudentCoreWebApi.DTOs
{
    public class UserCurrentProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public long? Phone { get; set; }
        public string Email { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; }
        public string ProfileImage { get; set; }
        public bool IsPremium { get; set; }
    }
}