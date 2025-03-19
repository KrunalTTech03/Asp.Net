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

    public UserRepository(ApplicationDbContext dbContext, IMapper mapper, PasswordService passwordService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _passwordService = passwordService;
    }

    public async Task<ApiResponse<object>> GetAllUsersAsync(string query, string sortBy, string sortOrder, int pageNumber, int pageSize)
    {
        var usersQuery = _dbContext.Users.AsQueryable();

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

        var users = await usersQuery.Skip((pageNumber - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

        return new ApiResponse<object>(true, ApiMessageExtensions.UserRetriveSuccessfully, users);
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

    public async Task<ApiResponse<AddUserResponseDto>> AddUserAsync(AddUser userDto, string userrole)
    {

        if (userrole != "Admin")
        {
            return new ApiResponse<AddUserResponseDto>(false, ApiMessageExtensions.RestrictedByAdmin);
        }

        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
        if (existingUser != null)
        {
            return new ApiResponse<AddUserResponseDto>(false, ApiMessageExtensions.UserAlreadyExist);
        }

        if (userDto.Phone != null)
        {
            var existingPhoneUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Phone == userDto.Phone);
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

        if (userDto.Roles != null && userDto.Roles.Count > 0)
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

            await _dbContext.SaveChangesAsync();        }
        else
        {
            return new ApiResponse<AddUserResponseDto>(false, ApiMessageExtensions.RoleNotFound);
        }

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

    public async Task<ApiResponse<object>> AddOrEditUserAsync(Guid? id, UpsertDto userDto, string role)
    {
        if (id == null || id == Guid.Empty)
        {
            var addUser = _mapper.Map<AddUser>(userDto);
            var response = await AddUserAsync(addUser, role);

            if (!response.Success)
            {
                return new ApiResponse<object>(false, response.Message, null);
            }

           var addUserResponse = response.Data as AddUserResponseDto;
            return new ApiResponse<object>(true, ApiMessageExtensions.UserAddedSuccessfully, addUserResponse);
        }
        else
        {
            var existingUser = await _dbContext.Users.FindAsync(id);
            if (existingUser == null)
            {
                return new ApiResponse<object>(false, ApiMessageExtensions.UserNotFound, null);
            }

            var updateUser = _mapper.Map<UpdateUser>(userDto);
            var response = await UpdateAsync(id.Value, updateUser, role);

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

        return new ApiResponse<User>(true, ApiMessageExtensions.UserRegisterSuccessfully, user);
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginUserAsync(LoginRequest request, JwtTokenService jwtTokenService)
    {
        var user = await _dbContext.Users
            .Where(s => !s.IsDeleted && s.Email == request.Email)
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
            .Where(ur => ur.User_Id == user.Id)
            .Select(ur => new
            {
                ur.Role_Id,
                RoleName = _dbContext.Roles
                            .Where(r => r.role_Id == ur.Role_Id)
                            .Select(r => r.role_name)
                            .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (userRole == null)
        {
            return new ApiResponse<LoginResponseDto>(false, ApiMessageExtensions.UserRoleNotAssigned);
        }

        var token = jwtTokenService.GenerateToken(user.Id.ToString(), user.Email, userRole.RoleName);

        var loginResponse = new LoginResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Role_Id = userRole.Role_Id,
            Role_Name = userRole.RoleName,
            Token = token
        };

        return new ApiResponse<LoginResponseDto>(true, ApiMessageExtensions.UserLoginSuccessfully, loginResponse);
    }


    public async Task<ApiResponse<AddUserResponseDto>> UpdateAsync(Guid id, UpdateUser updateUser, string userrole)
    {
        if (userrole != "Admin")
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

        var responseDto = new AddUserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = (long)user.Phone,
            PasswordHash = user.PasswordHash,
            PasswordSalt = user.PasswordSalt,
        };

        return new ApiResponse<AddUserResponseDto>(true, ApiMessageExtensions.UserUpdatedSuccessfully, responseDto);
    }


    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _dbContext.Users
            .Where(s => !s.IsDeleted && s.Email == email)
            .FirstOrDefaultAsync();
    }

    public async Task<ApiResponse<User>> DeleteAsync(Guid id, string userrole)
    {

        if (userrole != "Admin")
        {
            return new ApiResponse<User>(false, ApiMessageExtensions.RestrictedByAdmin);
        }

        var user = await _dbContext.Users.FindAsync(id);

        if (user == null)
        {
            return new ApiResponse<User>(false, ApiMessageExtensions.UserNotFound);
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        return new ApiResponse<User>(true, ApiMessageExtensions.UserDeleteSuccessfully, user);
    }
}