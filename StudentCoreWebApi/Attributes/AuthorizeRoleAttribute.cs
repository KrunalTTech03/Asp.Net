using Microsoft.AspNetCore.Authorization;

namespace StudentCoreWebApi.Attributes
{
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        public AuthorizeRoleAttribute(string role) 
        {
            Policy = $"RequiredRole:{role}";
        }
    }
}
