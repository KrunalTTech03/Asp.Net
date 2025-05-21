namespace StudentCoreWebApi.DTOs
{
    public class AssignMenuPermissionDTO
    {
        public Guid userId { get; set; }
        public Guid MenuId { get; set; }
        public Guid RoleId { get; set; }
        public List<Guid> PermissionIds { get; set; } = new List<Guid>();
    }
}