using Microsoft.EntityFrameworkCore;
using StudentCoreWebApi.Data;
using StudentCoreWebApi.Interface;
using StudentCoreWebApi.Model;
using StudentCoreWebApi.Response;

namespace StudentCoreWebApi.Repository
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public RoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<bool> IsUserAdminAsync(Guid userId)
        {
            var userRole = await _context.UsersRoles
                .Where(ur => ur.User_Id == userId)
                .Join(_context.Roles,
                    ur => ur.Role_Id,
                    r => r.role_Id,
                    (ur, r) => r.role_name)
                .FirstOrDefaultAsync();

            return userRole == "Admin";
        }


        public async Task<List<Role>> GetRolesAsync()
        {
            return await _context.Roles.ToListAsync();
        }

        public async Task<ApiResponse<Role>> CreateRoleAsync(Guid userId, Role role)
        {
            if (!await IsUserAdminAsync(userId))
            {
                return new ApiResponse<Role>(false, "Access denied. Only Admins can create roles.");
            }

            if (role == null)
            {
                return new ApiResponse<Role>(false, "Role cannot be null.");
            }

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return new ApiResponse<Role>(true, "Role created successfully.", role);
        }

        public async Task<ApiResponse<Role>> UpdateRoleAsync(Guid userId, Guid roleId, Role role)
        {
            if (!await IsUserAdminAsync(userId))
            {
                return new ApiResponse<Role>(false, "Access denied. Only Admins can update roles.");
            }

            var existingRole = await _context.Roles.FindAsync(roleId);
            if (existingRole == null)
            {
                return new ApiResponse<Role>(false, "Role not found.");
            }

            existingRole.role_name = role.role_name;

            _context.Roles.Update(existingRole);
            await _context.SaveChangesAsync();

            return new ApiResponse<Role>(true, "Role updated successfully.", existingRole);
        }

        public async Task<ApiResponse<string>> DeleteRoleAsync(Guid userId, Guid roleId)
        {
            if (!await IsUserAdminAsync(userId))
            {
                return new ApiResponse<string>(false, "Access denied. Only Admins can delete roles.");
            }

            var role = await _context.Roles.FindAsync(roleId);
            if (role == null || role.IsDeleted)
            {
                return new ApiResponse<string>(false, "Role not found or already deleted.");
            }

            role.IsDeleted = true;
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();

            return new ApiResponse<string>(true, "Role deleted successfully.", "Role soft-deleted.");
        }


        public async Task<ApiResponse<string>> AssignRoleToUserAsync(Guid currentUserId, Guid userId, Guid roleId)
        {
            if (!await IsUserAdminAsync(currentUserId))
            {
                return new ApiResponse<string>(false, "Access denied. Only Admins can assign roles.");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            var roleExists = await _context.Roles.AnyAsync(r => r.role_Id == roleId);

            if (!userExists || !roleExists)
            {
                return new ApiResponse<string>(false, "User or Role does not exist.");
            }

            var alreadyAssigned = await _context.UsersRoles.AnyAsync(ur => ur.User_Id == userId && ur.Role_Id == roleId);
            if (alreadyAssigned)
            {
                return new ApiResponse<string>(false, "Role already assigned to the user.");
            }

            var userRole = new Userrole
            {
                Id = Guid.NewGuid(),
                User_Id = userId,
                Role_Id = roleId
            };

            _context.UsersRoles.Add(userRole);

            await _context.SaveChangesAsync();

            return new ApiResponse<string>(true, "Role assigned to user successfully.");
        }

        public async Task<ApiResponse<string>> RemoveUserRoleAsync(Guid currentUserId, Guid userId, Guid roleId)
        {
            if (!await IsUserAdminAsync(currentUserId))
            {
                return new ApiResponse<string>(false, "Access denied. Only Admins can remove roles.");
            }

            var userRole = await _context.UsersRoles
                .FirstOrDefaultAsync(ur => ur.User_Id == userId && ur.Role_Id == roleId);

            if (userRole == null)
            {
                return new ApiResponse<string>(false, "Specified role is not assigned to the user.");
            }

            _context.UsersRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return new ApiResponse<string>(true, "Specified role removed from user successfully.");
        }


        public async Task<ApiResponse<List<string>>> GetUserAssignedRoleAsync(Guid userId)
        {
            var roleNames = await _context.UsersRoles
                .Where(ur => ur.User_Id == userId)
                .Join(_context.Roles,
                      ur => ur.Role_Id,
                      r => r.role_Id,
                      (ur, r) => r.role_name)
                .ToListAsync();

            if (roleNames == null)
            {
                return new ApiResponse<List<string>>(false, "No roles assigned to the user.");
            }

            return new ApiResponse<List<string>>(true, "Roles fetched successfully.", roleNames);
        }
    }
}
