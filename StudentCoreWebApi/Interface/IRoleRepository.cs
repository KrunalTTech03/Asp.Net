using StudentCoreWebApi.Model;
using StudentCoreWebApi.Response;

namespace StudentCoreWebApi.Interface
{
    public interface IRoleRepository
    {
        Task<List<Role>> GetRolesAsync();
        Task<ApiResponse<Role>> CreateRoleAsync(Guid userId, Role role);
        Task<ApiResponse<Role>> UpdateRoleAsync(Guid userId, Guid roleId, Role role);
        Task<ApiResponse<string>> DeleteRoleAsync(Guid userId, Guid roleId);
        Task<ApiResponse<string>> AssignRoleToUserAsync(Guid currentUserId, Guid userId, Guid roleId);
        Task<ApiResponse<string>> RemoveUserRoleAsync(Guid currentUserId, Guid userId, Guid roleId);
        Task<ApiResponse<List<string>>> GetUserAssignedRoleAsync(Guid userId);

    }
}
