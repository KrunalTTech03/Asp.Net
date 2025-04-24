namespace StudentCoreWebApi.DTOs
{
    public class RolePermissionDto
    {
        public Guid RolePermissionId { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public Guid PermissionId { get; set; }
        public string PermissionName { get; set; }
    }
}