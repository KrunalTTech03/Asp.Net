using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentCoreWebApi.Data;
using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Enums;
using StudentCoreWebApi.Interface;
using StudentCoreWebApi.Model;
using StudentCoreWebApi.Response;
using StudentCoreWebApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly PasswordService _passwordService;
    private readonly EmailServices _emailServices;

    public UserRepository(ApplicationDbContext dbContext, IMapper mapper, PasswordService passwordService, EmailServices emailServices)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _passwordService = passwordService;
        _emailServices = emailServices; 
    }

    public async Task<ApiResponse<object>> GetAllUsersAsync(string query, string sortBy, string sortOrder, int pageNumber, int pageSize, Guid userId)
    {
        // 1. Check for permission
        var userRoleIds = await _dbContext.UsersRoles
            .Where(ur => ur.User_Id == userId)
            .Select(ur => ur.Role_Id)
            .ToListAsync();

        var permissionName = PermissionEnum.Read.ToString();

        var permission = await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Name == permissionName);

        if (permission == null)
        {
            return new ApiResponse<object>(false, "Permission not found.");
        }

        var hasReadPermission = await _dbContext.RolePermissions
            .AnyAsync(rp => userRoleIds.Contains(rp.RoleId) && rp.PermissionId == permission.Id);

        if (!hasReadPermission)
        {
            return new ApiResponse<object>(false, ApiMessageExtensions.RestrictedByAdmin);
        }

        // 2. Get users
        var usersQuery = _dbContext.Users.Where(x => !x.IsDeleted).AsQueryable();

        if (!string.IsNullOrEmpty(query))
        {
            usersQuery = usersQuery.Where(s => s.FirstName.Contains(query) || s.LastName.Contains(query));
        }

        if (sortBy == "FirstName")
        {
            usersQuery = sortOrder == "asc" ? usersQuery.OrderBy(s => s.FirstName) : usersQuery.OrderByDescending(s => s.FirstName);
        }
        else
        {
            usersQuery = sortOrder == "asc" ? usersQuery.OrderBy(s => s.LastName) : usersQuery.OrderByDescending(s => s.LastName);
        }

        int totalCount = await usersQuery.CountAsync();

        var users = await usersQuery.Skip((pageNumber - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

        var userWithRoles = new List<object>();

        foreach (var user in users)
        {
            var roles = await _dbContext.UsersRoles
                .Where(ur => ur.User_Id == user.Id)
                .Select(ur => ur.Role_Id)
                .ToListAsync();

            var roleNames = await _dbContext.Roles
                .Where(r => roles.Contains(r.role_Id))
                .Select(r => r.role_name)
                .ToListAsync();

            userWithRoles.Add(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Phone,
                Roles = roleNames
            });
        }

        return new ApiResponse<object>(true, ApiMessageExtensions.UserRetriveSuccessfully, new
        {
            Users = userWithRoles,
            TotalCount = totalCount
        });
    }



    public async Task<ApiResponse<User>> GetByIdAsync(Guid id)
    {
        var user = await _dbContext.Users.FindAsync(id);

        if (user == null)
        {
            return new ApiResponse<User>(false, ApiMessageExtensions.UserNotFound);
        }

        return new ApiResponse<User>(true, ApiMessageExtensions.UserRetriveSuccessfully, user);
    }

    public async Task<ApiResponse<AddUserResponseDto>> AddUserAsync(AddUser userDto, Guid userId)
    {
        var userRoleIds = await _dbContext.UsersRoles
            .Where(ur => ur.User_Id == userId)
            .Select(ur => ur.Role_Id)
            .ToListAsync();

        var permissionName = PermissionEnum.Create.ToString();

        var permission = await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Name == permissionName);

        if (permission == null)
        {
            return new ApiResponse<AddUserResponseDto>(false, "Permission not found.");
        }

        var hasCreatePermission = await _dbContext.RolePermissions
            .AnyAsync(rp => userRoleIds.Contains(rp.RoleId) && rp.PermissionId == permission.Id);

        if (!hasCreatePermission)
        {
            return new ApiResponse<AddUserResponseDto>(false, ApiMessageExtensions.RestrictedByAdmin);
        }

        var existingUser = await _dbContext.Users
            .Where(x => !x.IsDeleted && x.Email == userDto.Email)
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            return new ApiResponse<AddUserResponseDto>(false, ApiMessageExtensions.UserAlreadyExist);
        }

        if (userDto.Phone != null)
        {
            var existingPhoneUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Phone == userDto.Phone);

            if (existingPhoneUser != null)
            {
                return new ApiResponse<AddUserResponseDto>(false, ApiMessageExtensions.UserWithPhoneNumberAlreadyExist);
            }
        }


        var user = _mapper.Map<User>(userDto);
        string salt;
        string passwordHash = _passwordService.HashPassword(userDto.Password, out salt);
        user.PasswordHash = passwordHash;
        user.PasswordSalt = salt;
        user.Phone = userDto.Phone ?? 0;

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var assignedRoles = new List<UserRoleDto>();

        if (userDto.Roles != null && userDto.Roles.Any())
        {
            foreach (var roleId in userDto.Roles)
            {
                var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.role_Id == roleId);
                if (role != null)
                {
                    var userRole = new Userrole
                    {
                        Id = Guid.NewGuid(),
                        User_Id = user.Id,
                        Role_Id = role.role_Id
                    };

                    await _dbContext.UsersRoles.AddAsync(userRole);

                    assignedRoles.Add(new UserRoleDto
                    {
                        Role_Id = role.role_Id,
                        Role_Name = role.role_name
                    });
                }
                else
                {
                    return new ApiResponse<AddUserResponseDto>(false, $"Role with ID '{roleId}' not found.");
                }
            }

            await _dbContext.SaveChangesAsync();
        }
        else
        {
            return new ApiResponse<AddUserResponseDto>(false, ApiMessageExtensions.RoleNotFound);
        }

        // 7. Return result
        var responseDto = new AddUserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = (long)user.Phone,
            PasswordHash = user.PasswordHash,
            PasswordSalt = user.PasswordSalt,
            Roles = assignedRoles
        };

        return new ApiResponse<AddUserResponseDto>(true, ApiMessageExtensions.UserAddedSuccessfully, responseDto);
    }

    public async Task<bool> SendTemplateEmailAsync(EmailTemplateType templateType, Dictionary<string, string> placeholders, string recipientEmail)
    {
        string templatePath;
        string subject;

        switch (templateType)
        {
            case EmailTemplateType.UserCreated:
                templatePath = "EmailTemplates/NewUserAdded.html";
                subject = "Login Credentials";
                break;

            case EmailTemplateType.UserRegistered:
                templatePath = "EmailTemplates/UserRegisteration.html";
                subject = "👤 New User Registered";
                break;

            default:
                throw new ArgumentException("Invalid template type");
        }

        if (!File.Exists(templatePath))
            return false;

        string templateContent = await File.ReadAllTextAsync(templatePath);
        foreach (var placeholder in placeholders)
        {
            templateContent = templateContent.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
        }

        return await _emailServices.SendEmailAsync(recipientEmail, subject, templateContent);
    }

    public async Task<ApiResponse<object>> AddOrEditUserAsync(UpsertDto userDto, Guid currentUserId)
    {
        string requiredPermission = (userDto.Id == null || userDto.Id == Guid.Empty)
            ? PermissionEnum.Create.ToString()
            : PermissionEnum.Update.ToString();

        var userRoleIds = await _dbContext.UsersRoles
            .Where(ur => ur.User_Id == currentUserId)
            .Select(ur => ur.Role_Id)
            .ToListAsync();

        var permission = await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Name == requiredPermission);

        if (permission == null)
        {
            return new ApiResponse<object>(false, $"{requiredPermission} permission not found.");
        }

        var hasPermission = await _dbContext.RolePermissions
            .AnyAsync(rp => userRoleIds.Contains(rp.RoleId) && rp.PermissionId == permission.Id);

        if (!hasPermission)
        {
            return new ApiResponse<object>(false, ApiMessageExtensions.RestrictedByAdmin);
        }

        if (userDto.Id == null || userDto.Id == Guid.Empty)
        {
            var addUser = _mapper.Map<AddUser>(userDto);
            var response = await AddUserAsync(addUser, currentUserId);

            if (!response.Success)
            {
                return new ApiResponse<object>(false, response.Message, null);
            }

            var addUserResponse = response.Data as AddUserResponseDto;
            string rolesNames = string.Join(", ", addUserResponse.Roles.Select(r => r.Role_Name));

            var placeholders = new Dictionary<string, string>
        {
            { "FirstName", addUserResponse.FirstName },
            { "Email", addUserResponse.Email },
            { "Password", userDto.Password },
            { "Roles", rolesNames }
        };

            bool emailSent = await SendTemplateEmailAsync(EmailTemplateType.UserCreated, placeholders, addUserResponse.Email);

            if (!emailSent)
            {
                return new ApiResponse<object>(false, "User added but email sending failed.", addUserResponse);
            }

            return new ApiResponse<object>(true, ApiMessageExtensions.UserAddedSuccessfully, addUserResponse);
        }
        else 
        {
            var existingUser = await _dbContext.Users.FindAsync(userDto.Id);
            if (existingUser == null)
            {
                return new ApiResponse<object>(false, ApiMessageExtensions.UserNotFound, null);
            }

            var updateUser = _mapper.Map<UpdateUser>(userDto);
            var response = await UpdateAsync(userDto.Id.Value, updateUser, currentUserId);

            if (!response.Success)
            {
                return new ApiResponse<object>(false, response.Message, null);
            }

            var updateUserResponse = response.Data as AddUserResponseDto;
            return new ApiResponse<object>(true, ApiMessageExtensions.UserUpdatedSuccessfully, updateUserResponse);
        }
    }


    public async Task<ApiResponse<User>> RegisterUserAsync(RegisterRequest request)
    {
        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            return new ApiResponse<User>(false, ApiMessageExtensions.UserAlreadyExist);
        }

        var user = _mapper.Map<User>(request);

        string salt;
        string passwordHash = _passwordService.HashPassword(request.Password, out salt);
        user.PasswordHash = passwordHash;
        user.PasswordSalt = salt;

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var roleName = RolesEnum.Manager.ToString();
        var managerRole = await _dbContext.Roles
                            .FirstOrDefaultAsync(r => r.role_name.ToLower() == roleName);

        if (managerRole == null)
        {
            return new ApiResponse<User>(false, ApiMessageExtensions.DefaultRoleNotFound);
        }

        var userRole = new Userrole
        {
            Id = Guid.NewGuid(),
            User_Id = user.Id,
            Role_Id = managerRole.role_Id
        };
        await _dbContext.UsersRoles.AddAsync(userRole);
        await _dbContext.SaveChangesAsync();

        var placeholders = new Dictionary<string, string>
    {
        { "FirstName", user.FirstName }
    };

        await SendTemplateEmailAsync(EmailTemplateType.UserRegistered, placeholders, user.Email);

        return new ApiResponse<User>(true, ApiMessageExtensions.UserRegisterSuccessfully, user);
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginUserAsync(LoginRequest request, JwtTokenService jwtTokenService)
    {
        var user = await _dbContext.Users
            .Where(u => !u.IsDeleted && u.Email == request.Email)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return new ApiResponse<LoginResponseDto>(false, ApiMessageExtensions.InvalidCredentials);
        }

        bool isPasswordValid = _passwordService.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return new ApiResponse<LoginResponseDto>(false, ApiMessageExtensions.InvalidCredentials);
        }

        var userRole = await _dbContext.UsersRoles
            .FirstOrDefaultAsync(ur => ur.User_Id == user.Id);

        if (userRole == null)
        {
            return new ApiResponse<LoginResponseDto>(false, ApiMessageExtensions.UserRoleNotAssigned);
        }

        var currentUserRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.role_Id == userRole.Role_Id);

        if (currentUserRole == null)
        {
            return new ApiResponse<LoginResponseDto>(false, ApiMessageExtensions.UserRoleNotAssigned);
        }

        var adminRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.role_name == "Admin");

        if (adminRole == null)
        {
            return new ApiResponse<LoginResponseDto>(false, "Admin role not found in the database.");
        }

        bool isAdmin = currentUserRole.role_Id == adminRole.role_Id;

        var token = jwtTokenService.GenerateToken(user.Id.ToString(), user.Email, currentUserRole.role_name);

        var loginResponse = new LoginResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Role_Id = currentUserRole.role_Id,
            Role_Name = currentUserRole.role_name,
            Token = token,
        };

        return new ApiResponse<LoginResponseDto>(true, ApiMessageExtensions.UserLoginSuccessfully, loginResponse);
    }

    public async Task<ApiResponse<AddUserResponseDto>> UpdateAsync(Guid id, UpdateUser updateUser, Guid currentUserId)
    {
        var userRoleIds = await _dbContext.UsersRoles
            .Where(ur => ur.User_Id == currentUserId)
            .Select(ur => ur.Role_Id)
            .ToListAsync();

        var permissionName = PermissionEnum.Update.ToString();

        var permission = await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Name == permissionName);

        if (permission == null)
        {
            return new ApiResponse<AddUserResponseDto>(false, "Edit permission not found.");
        }

        var hasEditPermission = await _dbContext.RolePermissions
            .AnyAsync(rp => userRoleIds.Contains(rp.RoleId) && rp.PermissionId == permission.Id);

        if (!hasEditPermission)
        {
            return new ApiResponse<AddUserResponseDto>(false, ApiMessageExtensions.RestrictedByAdmin);
        }

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
        {
            return new ApiResponse<AddUserResponseDto>(false, ApiMessageExtensions.UserNotFound);
        }

        user.FirstName = updateUser.FirstName;
        user.LastName = updateUser.LastName;
        user.Email = updateUser.Email;
        user.Phone = updateUser.Phone;

        await _dbContext.SaveChangesAsync();

        var assignedRoles = await _dbContext.UsersRoles
            .Where(ur => ur.User_Id == user.Id)
            .Select(ur => new UserRoleDto
            {
                Role_Id = ur.Role_Id,
                Role_Name = _dbContext.Roles
                    .Where(r => r.role_Id == ur.Role_Id)
                    .Select(r => r.role_name)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var responseDto = new AddUserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = (long)user.Phone,
            PasswordHash = user.PasswordHash,
            PasswordSalt = user.PasswordSalt,
            Roles = assignedRoles
        };

        return new ApiResponse<AddUserResponseDto>(true, ApiMessageExtensions.UserUpdatedSuccessfully, responseDto);
    }



    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _dbContext.Users
            .Where(s => !s.IsDeleted && s.Email == email)
            .FirstOrDefaultAsync();
    }

    public async Task<ApiResponse<object>> DeleteAsync(Guid userId, Guid currentUserId)
    {
        var userRoleIds = await _dbContext.UsersRoles
            .Where(ur => ur.User_Id == currentUserId)
            .Select(ur => ur.Role_Id)
            .ToListAsync();

        var permissionName = PermissionEnum.Delete.ToString();
        var permission = await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Name == permissionName);

        if (permission == null)
        {
            return new ApiResponse<object>(false, "Delete permission not found.");
        }

        var hasDeletePermission = await _dbContext.RolePermissions
            .AnyAsync(rp => userRoleIds.Contains(rp.RoleId) && rp.PermissionId == permission.Id);

        if (!hasDeletePermission)
        {
            return new ApiResponse<object>(false, ApiMessageExtensions.RestrictedByAdmin);
        }

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return new ApiResponse<object>(false, ApiMessageExtensions.UserNotFound);
        }

        user.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        return new ApiResponse<object>(true, ApiMessageExtensions.UserDeletedSuccessfully);
    }

    public async Task<List<Role>> GetRolesAsync()
    {
        return await _dbContext.Roles.ToListAsync();
    }
}