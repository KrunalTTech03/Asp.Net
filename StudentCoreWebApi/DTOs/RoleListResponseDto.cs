using StudentCoreWebApi.Model;

namespace StudentCoreWebApi.DTOs
{
    public class RoleListResponseDto
    {
        public int TotalCount { get; set; }
        public List<Role> Roles { get; set; }
    }
}
