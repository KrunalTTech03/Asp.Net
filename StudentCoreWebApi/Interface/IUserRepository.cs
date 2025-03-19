using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Model;
using StudentCoreWebApi.Response;
using StudentCoreWebApi.Services;
using System.Threading.Tasks;

namespace StudentCoreWebApi.Interface
{
    public interface IUserRepository
    {
        Task<ApiResponse<object>> GetAllUsersAsync(string query, string sortBy, string sortOrder, int pageNumber, int pageSize);
        Task<ApiResponse<User>> GetByIdAsync(Guid id);
        Task<ApiResponse<AddUserResponseDto>> AddUserAsync(AddUser userDto, string role);
        Task<ApiResponse<User>> RegisterUserAsync(RegisterRequest userDto);
        Task<ApiResponse<LoginResponseDto>> LoginUserAsync(LoginRequest request, JwtTokenService jwtTokenService);
        Task<ApiResponse<AddUserResponseDto>> UpdateAsync(Guid id, UpdateUser updateUser, string role);
        Task<ApiResponse<object>> AddOrEditUserAsync(Guid? id, UpsertDto userDto, string role);
        Task<ApiResponse<User>> DeleteAsync(Guid id, string role);
        Task<User?> GetUserByEmailAsync(string email);
    }
}
