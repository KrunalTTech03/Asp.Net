namespace StudentCoreWebApi.Enums
{
    public class ApiMessageExtensions
    {
        public const string UserAlreadyExist = "User Already Exist";
        public const string UserNotFound = "User not Found";
        public const string PasswordRequired = "Password is Required";
        public const string RoleNotFound = "Role Not Found";
        public const string UserRoleNotAssigned = "User role not assigned";
        public const string UserAddedSuccessfully = "User Added Successfully";
        public const string UserUpdatedSuccessfully = "User Updated Successfully";
        public const string UserRetriveSuccessfully = "User Retrieve Successfully";
        public const string UserDeleteSuccessfully = "User Delete Successfully";
        public const string UserRegisterSuccessfully = "User registered successfully with default role 'Manager'.";
        public const string UserLoginSuccessfully = "User Login Successfully";
        public const string InvalidCredentials = "Invalid Crendentials";
        public const string DefaultRoleNotFound = "Manage Role not Found";
        public const string RestrictedByAdmin = "Access Denied. Resctricted by Admin";
        public const string UserWithPhoneNumberAlreadyExist = "User With Phone Number Already Exists!";
    }
}