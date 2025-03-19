namespace StudentCoreWebApi.DTOs
{
    public class LoginResponseDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public long? Phone { get; set; }
        public Guid Role_Id { get; set; }
        public string Role_Name { get; set; }
        public string Token { get; set; }

    }
}
