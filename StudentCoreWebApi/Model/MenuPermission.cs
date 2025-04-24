namespace StudentCoreWebApi.Model
{
    public class MenuPermission
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }
        public Menu Menu { get; set; }

        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; }
    }
}