using Microsoft.EntityFrameworkCore;
using StudentCoreWebApi.Data;
using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Interface;
using StudentCoreWebApi.Model;
using StudentCoreWebApi.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentCoreWebApi.Repository
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly ApplicationDbContext _dbcontext;

        public PermissionRepository(ApplicationDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        public async Task<ApiResponse<Permission>> CreatePermissionAsync(Guid userId, Permission permission)
        {
            if (!await HasPermissionAsync(userId, "ManagePermissions"))
                return new ApiResponse<Permission>(false, "Access denied. Missing permission: ManagePermissions");

            permission.Id = Guid.NewGuid();
            _dbcontext.Permissions.Add(permission);
            await _dbcontext.SaveChangesAsync();

            return new ApiResponse<Permission>(true, "Permission created successfully", permission);
        }

        public async Task<ApiResponse<string>> AssignPermissionsToRoleAsync(Guid userId, Guid roleId, List<Guid> permissionIds)
        {
            if (!await HasPermissionAsync(userId, "ManagePermissions"))
            {
                return new ApiResponse<string>(false, "Access denied. Missing permission: ManagePermissions");
            }

            foreach (var permissionId in permissionIds)
            {
                var rolePermission = new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    PermissionId = permissionId
                };

                _dbcontext.RolePermissions.Add(rolePermission);
            }

            await _dbcontext.SaveChangesAsync();

            return new ApiResponse<string>(true, "Permissions assigned to role successfully.");
        }


        public async Task<ApiResponse<string>> RemovePermissionFromRoleAsync(Guid userId, Guid roleId, Guid permissionId)
        {
            if (!await HasPermissionAsync(userId, "ManagePermissions"))
                return new ApiResponse<string>(false, "Access denied. Missing permission: ManagePermissions");

            var rp = await _dbcontext.RolePermissions
                .FirstOrDefaultAsync(r => r.RoleId == roleId && r.PermissionId == permissionId);

            if (rp == null)
                return new ApiResponse<string>(false, "Permission not found for this role");

            _dbcontext.RolePermissions.Remove(rp);
            await _dbcontext.SaveChangesAsync();

            return new ApiResponse<string>(true, "Permission removed from role successfully");
        }

        public async Task<ApiResponse<List<string>>> GetPermissionsByRoleAsync(Guid roleId)
        {
            var permissions = await _dbcontext.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Join(_dbcontext.Permissions,
                    rp => rp.PermissionId,
                    p => p.Id,
                    (rp, p) => p.Name)
                .ToListAsync();

            return new ApiResponse<List<string>>(true, "Permissions retrieved successfully", permissions);
        }

        public async Task<bool> HasPermissionAsync(Guid userId, string permissionName)
        {
            var roleIds = await _dbcontext.UsersRoles
                .Where(ur => ur.User_Id == userId)
                .Select(ur => ur.Role_Id)
                .ToListAsync();


            var hasPermission = await _dbcontext.RolePermissions
                .AnyAsync(rp => roleIds.Contains(rp.RoleId) &&
                                _dbcontext.Permissions.Any(p => p.Id == rp.PermissionId && p.Name == permissionName));

            return hasPermission;
        }

        public async Task<ApiResponse<List<RolePermissionDto>>> GetAllRolePermissionsAsync()
        {
            var rolePermissions = await _dbcontext.RolePermissions
                .Join(_dbcontext.Roles,
                      rp => rp.RoleId,
                      r => r.role_Id,
                      (rp, r) => new { rp, r })
                .Join(_dbcontext.Permissions,
                      rpr => rpr.rp.PermissionId,
                      p => p.Id,
                      (rpr, p) => new RolePermissionDto
                      {
                          RolePermissionId = rpr.rp.Id,
                          RoleId = rpr.r.role_Id,
                          RoleName = rpr.r.role_name,
                          PermissionId = p.Id,
                          PermissionName = p.Name
                      })
                .ToListAsync();

            return new ApiResponse<List<RolePermissionDto>>(true, "Role-permission mappings retrieved successfully", rolePermissions);
        }

    }
}
