namespace StudentCoreWebApi.DTOs
{
    public class MenuRoleDto
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }
        public string MenuTitle { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
    }
}