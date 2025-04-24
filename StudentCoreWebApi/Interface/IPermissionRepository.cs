using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Model;
using StudentCoreWebApi.Response;

namespace StudentCoreWebApi.Interface
{
    public interface IPermissionRepository
    {
        Task<ApiResponse<Permission>> CreatePermissionAsync(Guid userId, Permission permission);
        Task<ApiResponse<string>> AssignPermissionsToRoleAsync(Guid userId, Guid roleId, List<Guid> permissionIds);
        Task<ApiResponse<string>> RemovePermissionFromRoleAsync(Guid userId, Guid roleId, Guid permissionId);
        Task<ApiResponse<List<string>>> GetPermissionsByRoleAsync(Guid roleId);
        Task<ApiResponse<List<RolePermissionDto>>> GetAllRolePermissionsAsync();
        Task<bool> HasPermissionAsync(Guid userId, string permissionName);
    }
}
