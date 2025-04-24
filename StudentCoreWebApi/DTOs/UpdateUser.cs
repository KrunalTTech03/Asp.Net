namespace StudentCoreWebApi.DTOs
{
    public class UpdateUser
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public long Phone { get; set; }
        public List<Guid> Roles { get; set; }

    }
}
