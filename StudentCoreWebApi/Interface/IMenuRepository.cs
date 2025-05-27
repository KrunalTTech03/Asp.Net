using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Model;
using StudentCoreWebApi.Response;

namespace StudentCoreWebApi.Interface
{
    public interface IMenuRepository
    {
        Task<List<MenuDTO>> GetAllMenusAsync();
        Task<List<CreateMenuPermission>> GetAllPermissionsAsync();
        Task<List<MenuDTO>> GetMenuByUserAsync(Guid userId);
        Task<ApiResponse<Menu>> CreateMenuAsync(Guid userId, MenuDTO menuDto);
        Task<ApiResponse<Menu>> UpdateMenuAsync(Guid userId, Guid menuId, MenuDTO menuDto);
        Task<ApiResponse<string>> DeleteMenuAsync(Guid userId, Guid menuId);
        Task<ApiResponse<List<CreateMenuPermission>>> GetPermissionsByMenuIdAsync(Guid menuId);
        Task<ApiResponse<object>> AssignPermissionToMenuAsync(Guid userId, Guid menuId,Guid roleId, List<Guid> permissionIds);
        Task<ApiResponse<object>> CreatePermissionAsync(Guid userId, CreateMenuPermission createMenuPermission);
        Task<ApiResponse<string>> DeletePermissionAsync(Guid userId, Guid permissionId);
        Task<ApiResponse<object>> DeleteMenuPermissionsAsync(Guid userId, Guid menuId, List<Guid> permissionIds);
        Task<ApiResponse<object>> GetFilteredMenusAsync(List<GenericFilter> filters);
    }
}