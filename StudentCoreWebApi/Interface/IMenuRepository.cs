using StudentCoreWebApi.DTOs;

namespace StudentCoreWebApi.Interface
{
    public interface IMenuRepository
    {
        Task<List<MenuDTO>> GetMenuByUserAsync(Guid userId);
        Task<bool> CreateMenuAsync(MenuDTO menuDto);
        Task AssignPermissionToMenuAsync(Guid menuId, List<Guid> permissionIds);
    }
}