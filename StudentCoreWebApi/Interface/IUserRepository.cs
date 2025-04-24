using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Model;
using StudentCoreWebApi.Response;
using StudentCoreWebApi.Services;
using System.Threading.Tasks;

namespace StudentCoreWebApi.Interface
{
    public interface IUserRepository
    {
        Task<ApiResponse<object>> GetAllUsersAsync(string query, string sortBy, string sortOrder, int pageNumber, int pageSize, Guid userId);
        Task<ApiResponse<User>> GetByIdAsync(Guid id);
        Task<ApiResponse<AddUserResponseDto>> AddUserAsync(AddUser userDto, Guid userId);
        Task<ApiResponse<AddUserResponseDto>> UpdateAsync(Guid id, UpdateUser updateUser, Guid currentUserId);
        Task<ApiResponse<User>> RegisterUserAsync(RegisterRequest userDto);
        Task<ApiResponse<LoginResponseDto>> LoginUserAsync(LoginRequest request, JwtTokenService jwtTokenService);
        Task<ApiResponse<object>> AddOrEditUserAsync(UpsertDto userDto, Guid currentUserId);
        Task<ApiResponse<object>> DeleteAsync(Guid id, Guid userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<List<Role>> GetRolesAsync();
    }
}
