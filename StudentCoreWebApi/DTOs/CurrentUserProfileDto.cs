namespace StudentCoreWebApi.DTOs
{
    public class CurrentUserProfileDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<UserRoleDto> UserRole { get; set; } = new(); 
        public string Email { get; set; }
        public long Phone { get; set; }
    }

}
