namespace StudentCoreWebApi.DTOs
{
    public class AssignPermissionDto
    {
        public Guid MenuId { get; set; }
        public Guid RoleId { get; set; }
        public List<Guid> PermissionIds { get; set; } = new List<Guid>();
    }
}