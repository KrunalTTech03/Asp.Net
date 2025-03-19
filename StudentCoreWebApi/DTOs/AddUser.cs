namespace StudentCoreWebApi.DTOs
{
    public class AddUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public long? Phone { get; set; }  
        public string Password { get; set; }
        public List<Guid> Roles { get; set; }  
    }

}