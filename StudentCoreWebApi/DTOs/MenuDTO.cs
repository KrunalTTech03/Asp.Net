namespace StudentCoreWebApi.DTOs
{
    public class MenuDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Icon { get; set; }
        public string? Path { get; set; }
        public int? Order { get; set; } 
        public Guid? ParentMenuId { get; set; }
        public List<MenuDTO> SubMenus { get; set; } = new();
    }

}
