namespace StudentCoreWebApi.Model
{
    public class MenuRoles
    {
        public Guid Id { get; set; }

        public Guid MenuId { get; set; }
        public Menu Menu { get; set; }

        public Guid RoleId { get; set; }
        public Role Role { get; set; }
    }
}
