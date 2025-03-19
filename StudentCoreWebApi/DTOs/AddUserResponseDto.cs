namespace StudentCoreWebApi.DTOs
{
    public class AddUserResponseDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public long Phone { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public List<UserRoleDto> Roles { get; set; }
    }
}