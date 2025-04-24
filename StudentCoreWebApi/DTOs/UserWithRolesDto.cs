namespace StudentCoreWebApi.DTOs
{
    public class UserRoleDto
    {
        public Guid Role_Id { get; set; }
        public string Role_Name { get; set; }
    }

    public class UserWithRolesDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public long? Phone { get; set; }
        public List<UserRoleDto> Roles { get; set; }
    }
}
