using Microsoft.EntityFrameworkCore;
using StudentCoreWebApi.Data;
using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Interface;
using StudentCoreWebApi.Model;

namespace StudentCoreWebApi.Repository
{
    public class MenuRepository : IMenuRepository
    {
        private ApplicationDbContext _dbContext;
        public MenuRepository(ApplicationDbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        public async Task<List<MenuDTO>> GetMenuByUserAsync(Guid userId)
        {
            var roleIds = await _dbContext.UsersRoles
                .Where(ur => ur.User_Id == userId)
                .Select(ur => ur.Role_Id)
                .ToListAsync();

            var permissionIds = await _dbContext.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.PermissionId)
                .Distinct()
                .ToListAsync();

            var accessibleMenuIds = await _dbContext.MenuPermissions
                .Where(mp => permissionIds.Contains(mp.PermissionId))
                .Select(mp => mp.MenuId)
                .Distinct()
                .ToListAsync();

            var allMenus = await _dbContext.Menus
                .Where(m => accessibleMenuIds.Contains(m.Id))
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
                    Title = m.Title,
                    Icon = m.Icon,
                    Path = m.Path,
                    SubMenus = BuildMenuTree(
                        allMenus.Where(sm => sm.ParentMenuId == m.Id).OrderBy(sm => sm.Order).ToList()
                    )
                }).ToList();
            }

            return BuildMenuTree(parentMenus);
        }

        public async Task<bool> CreateMenuAsync(MenuDTO menuDto)
        {
            var menu = new Menu
            {
                Id = Guid.NewGuid(),
                Title = menuDto.Title,
                Icon = menuDto.Icon,
                Path = menuDto.Path,
                Order = menuDto.Order,
                ParentMenuId = menuDto.ParentMenuId
            };

            await _dbContext.Menus.AddAsync(menu);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task AssignPermissionToMenuAsync(Guid menuId, List<Guid> permissionIds)
        {
            var menu = await _dbContext.Menus.FindAsync(menuId);
            if (menu == null)
                throw new ArgumentException("Menu not found.");

            foreach (var permissionId in permissionIds)
            {
                var exists = await _dbContext.MenuPermissions
                    .AnyAsync(mp => mp.MenuId == menuId && mp.PermissionId == permissionId);

                if (!exists)
                {
                    _dbContext.MenuPermissions.Add(new MenuPermission
                    {
                        MenuId = menuId,
                        PermissionId = permissionId
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
        }

    }
}
