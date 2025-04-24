namespace StudentCoreWebApi.Model
{
    public class Menu
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Icon { get; set; }
        public string? Path { get; set; }
        public int? Order { get; set; }

        public Guid? ParentMenuId { get; set; }
        public Menu? ParentMenu { get; set; }
        public List<Menu> SubMenus { get; set; }

        public List<MenuPermission> MenuPermissions { get; set; }
    }
}
