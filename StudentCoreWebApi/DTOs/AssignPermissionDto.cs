namespace StudentCoreWebApi.DTOs
{
    public class AssignPermissionDto
    {
        public Guid RoleId { get; set; }
        public Guid MenuId { get; set; }
        public List<Guid> PermissionIds { get; set; }
    }

}