using System.ComponentModel.DataAnnotations;

namespace StudentCoreWebApi.Model
{
    public class Role
    {
        [Key]
        public Guid role_Id { get; set; }
        public string role_name { get; set; }
    }
}