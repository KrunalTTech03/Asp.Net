using Microsoft.EntityFrameworkCore;
using StudentCoreWebApi.Data;
using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Interface;
using StudentCoreWebApi.Model;
using StudentCoreWebApi.Response;
using Microsoft.Extensions.Logging;

namespace StudentCoreWebApi.Repository
{
    public class MenuRepository : IMenuRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<MenuRepository> _logger;

        public MenuRepository(ApplicationDbContext dbContext, ILogger<MenuRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        private async Task<bool> IsUserAdminAsync(Guid userId)
        {
            var userRole = await _dbContext.UsersRoles
                .Where(ur => ur.User_Id == userId)
                .Join(_dbContext.Roles,
                    ur => ur.Role_Id,
                    r => r.role_Id,
                    (ur, r) => r.role_name)
                .FirstOrDefaultAsync();

            return userRole == "Admin";
        }

        public async Task<List<MenuDTO>> GetAllMenusAsync()
        {
            try
            {
                var allMenus = await _dbContext.Menus
                    .Include(m => m.SubMenus)
                    .ToListAsync();

                var parentMenus = allMenus
                    .Where(m => m.ParentMenuId == null)
                    .OrderBy(m => m.Order)
                    .ToList();

                List<MenuDTO> BuildMenuTree(List<Menu> menus)
                {
                    return menus.Select(m => new MenuDTO
                    {
                        Id = m.Id,
                        Title = m.Title,
                        Icon = m.Icon,
                        Path = m.Path,
                        Order = m.Order,
                        SubMenus = BuildMenuTree(
                            allMenus.Where(sm => sm.ParentMenuId == m.Id).OrderBy(sm => sm.Order).ToList()
                        )
                    }).ToList();
                }

                return BuildMenuTree(parentMenus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all menus.");
                throw new Exception("Error retrieving all menus.", ex);
            }
        }

        public async Task<List<CreateMenuPermission>> GetAllPermissionsAsync()
        {
            try
            {
                var permissions = await _dbContext.CreateMenuPermissions
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all permissions.");
                throw new Exception("Error retrieving all permissions.", ex);
            }
        }

        public async Task<List<MenuDTO>> GetMenuByUserAsync(Guid userId)
        {
            try
            {
                var roleIds = await _dbContext.UsersRoles
                    .Where(ur => ur.User_Id == userId)
                    .Select(ur => ur.Role_Id)
                    .ToListAsync();

                if (!roleIds.Any())
                {
                    _logger.LogWarning("User {UserId} has no roles.", userId);
                    return new List<MenuDTO>();
                }

                var roleMenuIds = await _dbContext.MenuRoles
                    .Where(mr => roleIds.Contains(mr.RoleId))
                    .Select(mr => mr.MenuId)
                    .Distinct()
                    .ToListAsync();

                if (!roleMenuIds.Any())
                {
                    _logger.LogWarning("User {UserId} has no menus via roles.", userId);
                    return new List<MenuDTO>();
                }

                var accessibleMenuIds = await _dbContext.MenuPermissions
                    .Where(mp => roleMenuIds.Contains(mp.MenuId))
                    .Select(mp => mp.MenuId)
                    .Distinct()
                    .ToListAsync();

                if (!accessibleMenuIds.Any())
                {
                    _logger.LogWarning("User {UserId} has no accessible menus with permissions.", userId);
                    return new List<MenuDTO>();
                }

                var accessibleMenus = await _dbContext.Menus
                    .Where(m => accessibleMenuIds.Contains(m.Id))
                    .ToListAsync();

                var parentMenuIds = new HashSet<Guid?>();
                foreach (var menu in accessibleMenus.Where(m => m.ParentMenuId.HasValue))
                {
                    var currentParentId = menu.ParentMenuId;
                    while (currentParentId.HasValue)
                    {
                        parentMenuIds.Add(currentParentId);
                        var parentMenu = await _dbContext.Menus.FirstOrDefaultAsync(m => m.Id == currentParentId);
                        if (parentMenu == null)
                            break;

                        currentParentId = parentMenu.ParentMenuId;
                    }
                }

                if (parentMenuIds.Any())
                {
                    var parentMenus = await _dbContext.Menus
                        .Where(m => parentMenuIds.Contains(m.Id) && !accessibleMenuIds.Contains(m.Id))
                        .ToListAsync();

                    accessibleMenus.AddRange(parentMenus);
                }

                List<MenuDTO> BuildMenuTree(List<Menu> menus, Guid? parentId = null)
                {
                    return menus
                        .Where(m => m.ParentMenuId == parentId)
                        .OrderBy(m => m.Order)
                        .Select(m => new MenuDTO
                        {
                            Id = m.Id,
                            Title = m.Title,
                            Icon = m.Icon,
                            Path = m.Path,
                            Order = m.Order,
                            SubMenus = BuildMenuTree(menus, m.Id)
                        })
                        .Where(m => accessibleMenuIds.Contains(m.Id) || m.SubMenus.Any())
                        .ToList();
                }

                return BuildMenuTree(accessibleMenus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving menu by user: {UserId}", userId);
                throw new Exception("Error retrieving menu by user.", ex);
            }
        }

        public async Task<ApiResponse<Menu>> CreateMenuAsync(Guid userId, MenuDTO menuDto)
        {
            try
            {
                if (!await IsUserAdminAsync(userId))
                {
                    _logger.LogWarning("Access denied. User {UserId} attempted to create menu without admin rights", userId);
                    return new ApiResponse<Menu>(false, "Access denied. Only Admins can create menus.");
                }

                var menu = new Menu
                {
                    Id = Guid.NewGuid(),
                    Title = menuDto.Title,
                    Icon = menuDto.Icon,
                    Path = menuDto.Path,
                    Order = menuDto.Order ?? 0,
                    ParentMenuId = menuDto.ParentMenuId
                };

                _logger.LogInformation("Admin {UserId} creating menu: {Title}", userId, menuDto.Title);
                await _dbContext.Menus.AddAsync(menu);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Menu created successfully: {Title}", menuDto.Title);

                return new ApiResponse<Menu>(true, "Menu created successfully!", menu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating menu: {Title}", menuDto.Title);
                return new ApiResponse<Menu>(false, "Error creating menu: " + ex.Message);
            }
        }

        public async Task<ApiResponse<Menu>> UpdateMenuAsync(Guid userId, Guid menuId, MenuDTO menuDto)
        {
            try
            {
                if (!await IsUserAdminAsync(userId))
                {
                    _logger.LogWarning("Access denied. User {UserId} attempted to update menu without admin rights", userId);
                    return new ApiResponse<Menu>(false, "Access denied. Only Admins can update menus.");
                }

                var menu = await _dbContext.Menus.FindAsync(menuId);
                if (menu == null)
                {
                    _logger.LogWarning("Menu with ID {MenuId} not found for update.", menuId);
                    return new ApiResponse<Menu>(false, "Menu not found.");
                }

                menu.Title = menuDto.Title;
                menu.Icon = menuDto.Icon;
                menu.Path = menuDto.Path;
                menu.Order = menuDto.Order ?? menu.Order;
                menu.ParentMenuId = menuDto.ParentMenuId;

                _dbContext.Menus.Update(menu);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Menu {MenuId} updated successfully by admin {UserId}", menuId, userId);
                return new ApiResponse<Menu>(true, "Menu updated successfully.", menu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating menu: {MenuId}", menuId);
                return new ApiResponse<Menu>(false, "Error updating menu: " + ex.Message);
            }
        }

        public async Task<ApiResponse<string>> DeleteMenuAsync(Guid userId, Guid menuId)
        {
            try
            {
                if (!await IsUserAdminAsync(userId))
                {
                    _logger.LogWarning("Access denied. User {UserId} attempted to delete menu without admin rights", userId);
                    return new ApiResponse<string>(false, "Access denied. Only Admins can delete menus.");
                }

                var menu = await _dbContext.Menus.FindAsync(menuId);
                if (menu == null)
                {
                    _logger.LogWarning("Menu with ID {MenuId} not found for deletion.", menuId);
                    return new ApiResponse<string>(false, "Menu not found.");
                }

                var hasSubMenus = await _dbContext.Menus.AnyAsync(m => m.ParentMenuId == menuId);
                if (hasSubMenus)
                {
                    _logger.LogWarning("Cannot delete menu {MenuId} as it has sub-menus.", menuId);
                    return new ApiResponse<string>(false, "Cannot delete menu that has sub-menus.");
                }

                var menuPermissions = await _dbContext.MenuPermissions
                    .Where(mp => mp.MenuId == menuId)
                    .ToListAsync();

                if (menuPermissions.Any())
                {
                    _dbContext.MenuPermissions.RemoveRange(menuPermissions);
                }

                _dbContext.Menus.Remove(menu);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Menu {MenuId} deleted successfully by admin {UserId}", menuId, userId);
                return new ApiResponse<string>(true, "Menu deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting menu: {MenuId}", menuId);
                return new ApiResponse<string>(false, "Error deleting menu: " + ex.Message);
            }
        }

        public async Task<ApiResponse<List<CreateMenuPermission>>> GetPermissionsByMenuIdAsync(Guid menuId)
        {
            try
            {
                var permissions = await _dbContext.MenuPermissions
                    .Where(mp => mp.MenuId == menuId)
                    .Join(_dbContext.CreateMenuPermissions,
                        mp => mp.PermissionId,
                        p => p.Id,
                        (mp, p) => p)
                    .ToListAsync();

                return new ApiResponse<List<CreateMenuPermission>>(true, "Permissions retrieved successfully.", permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions for menu {MenuId}", menuId);
                return new ApiResponse<List<CreateMenuPermission>>(false, "Error retrieving permissions: " + ex.Message);
            }
        }

        public async Task<ApiResponse<object>> AssignPermissionToMenuAsync(Guid userId, Guid menuId, Guid roleId, List<Guid> permissionIds)
        {
            try
            {
                if (!await IsUserAdminAsync(userId))
                {
                    _logger.LogWarning("Access denied. User {UserId} attempted to assign menu permissions without admin rights", userId);
                    return new ApiResponse<object>(false, "Access denied. Only Admins can assign menu permissions.");
                }

                var menu = await _dbContext.Menus.FindAsync(menuId);
                if (menu == null)
                {
                    _logger.LogError("Menu with ID {MenuId} not found.", menuId);
                    return new ApiResponse<object>(false, "Menu not found.");
                }

                var existingMenuPermissions = await _dbContext.MenuPermissions
                    .Where(mp => mp.MenuId == menuId)
                    .ToListAsync();

                _dbContext.MenuPermissions.RemoveRange(existingMenuPermissions);

                foreach (var permissionId in permissionIds)
                {
                    var permissionExists = await _dbContext.CreateMenuPermissions.AnyAsync(p => p.Id == permissionId);
                    if (!permissionExists)
                    {
                        _logger.LogWarning("Permission with ID {PermissionId} not found.", permissionId);
                        return new ApiResponse<object>(false, $"Permission with ID {permissionId} not found.");
                    }

                    var menuPermission = new MenuPermission
                    {
                        Id = Guid.NewGuid(),
                        MenuId = menuId,
                        PermissionId = permissionId,
                    };

                    await _dbContext.MenuPermissions.AddAsync(menuPermission);
                }

                var existingMenuRole = await _dbContext.MenuRoles
                    .FirstOrDefaultAsync(mr => mr.MenuId == menuId && mr.RoleId == roleId);

                if (existingMenuRole == null)
                {
                    var newMenuRole = new MenuRoles
                    {
                        Id = Guid.NewGuid(),
                        MenuId = menuId,
                        RoleId = roleId
                    };

                    await _dbContext.MenuRoles.AddAsync(newMenuRole);
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Permissions and menu-role assignment completed for Menu {MenuId}, Role {RoleId} by Admin {UserId}.", menuId, roleId, userId);
                return new ApiResponse<object>(true, "Permissions and role assignment successfully completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning permissions to menu: {Message}", ex.Message);
                return new ApiResponse<object>(false, "Error assigning permissions to menu: " + ex.Message);
            }
        }

        public async Task<ApiResponse<object>> CreatePermissionAsync(Guid userId, CreateMenuPermission createMenuPermission)
        {
            try
            {
                if (!await IsUserAdminAsync(userId))
                {
                    _logger.LogWarning("Access denied. User {UserId} attempted to create permission without admin rights", userId);
                    return new ApiResponse<object>(false, "Access denied. Only Admins can create permissions.");
                }

                if (createMenuPermission == null)
                {
                    return new ApiResponse<object>(false, "Permission cannot be null.");
                }

                if (createMenuPermission.Id == Guid.Empty)
                {
                    createMenuPermission.Id = Guid.NewGuid();
                }

                var existingPermission = await _dbContext.CreateMenuPermissions
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == createMenuPermission.Name.ToLower());

                if (existingPermission != null)
                {
                    _logger.LogWarning("Permission with name {PermissionName} already exists.", createMenuPermission.Name);
                    return new ApiResponse<object>(false, $"Permission with name '{createMenuPermission.Name}' already exists.");
                }

                var permission = new CreateMenuPermission
                {
                    Id = createMenuPermission.Id,
                    Name = createMenuPermission.Name
                };

                _logger.LogInformation("Admin {UserId} creating permission: {PermissionName}", userId, permission.Name);
                await _dbContext.CreateMenuPermissions.AddAsync(createMenuPermission);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Permission created successfully: {PermissionId}", permission.Id);
                return new ApiResponse<object>(true, "Permission created successfully.", permission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission");
                return new ApiResponse<object>(false, "Error creating permission: " + ex.Message);
            }
        }

        public async Task<ApiResponse<string>> DeletePermissionAsync(Guid userId, Guid permissionId)
        {
            try
            {
                if (!await IsUserAdminAsync(userId))
                {
                    _logger.LogWarning("Access denied. User {UserId} attempted to delete permission without admin rights", userId);
                    return new ApiResponse<string>(false, "Access denied. Only Admins can delete permissions.");
                }

                var permission = await _dbContext.CreateMenuPermissions.FindAsync(permissionId);
                if (permission == null)
                {
                    _logger.LogWarning("Permission with ID {PermissionId} not found for deletion.", permissionId);
                    return new ApiResponse<string>(false, "Permission not found.");
                }

                var menuPermissions = await _dbContext.MenuPermissions
                    .Where(mp => mp.PermissionId == permissionId)
                    .ToListAsync();

                if (menuPermissions.Any())
                {
                    _dbContext.MenuPermissions.RemoveRange(menuPermissions);
                }

                var rolePermissions = await _dbContext.RolePermissions
                    .Where(rp => rp.PermissionId == permissionId)
                    .ToListAsync();

                if (rolePermissions.Any())
                {
                    _dbContext.RolePermissions.RemoveRange(rolePermissions);
                }

                _dbContext.CreateMenuPermissions.Remove(permission);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Permission {PermissionId} deleted successfully by admin {UserId}", permissionId, userId);
                return new ApiResponse<string>(true, "Permission deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission: {PermissionId}", permissionId);
                return new ApiResponse<string>(false, "Error deleting permission: " + ex.Message);
            }
        }

        public async Task<ApiResponse<object>> DeleteMenuPermissionsAsync(Guid userId, Guid menuId, List<Guid> permissionIds)
        {
            try
            {
                if (!await IsUserAdminAsync(userId))
                {
                    _logger.LogWarning("Access denied. User {UserId} attempted to delete menu permissions without admin rights", userId);
                    return new ApiResponse<object>(false, "Access denied. Only Admins can delete menu permissions.");
                }

                var menuPermissions = await _dbContext.MenuPermissions
                    .Where(mp => mp.MenuId == menuId && permissionIds.Contains(mp.PermissionId))
                    .ToListAsync();

                if (!menuPermissions.Any())
                {
                    _logger.LogWarning("No menu permissions found for deletion.");
                    return new ApiResponse<object>(false, "No matching menu permissions found.");
                }

                _dbContext.MenuPermissions.RemoveRange(menuPermissions);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Menu permissions deleted successfully for Menu {MenuId} by admin {UserId}", menuId, userId);
                return new ApiResponse<object>(true, "Menu permissions deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting multiple menu permissions.");
                return new ApiResponse<object>(false, "Error deleting menu permissions: " + ex.Message);
            }
        }

    }
}