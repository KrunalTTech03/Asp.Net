namespace StudentCoreWebApi.Model
{
    public class User
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }   
        public string? LastName { get; set; }    
        public string Email { get; set; }        
        public long? Phone { get; set; }         
        public string? PasswordHash { get; set; }
        public string? PasswordSalt { get; set; }
        public bool IsDeleted { get; set; }
    }
}