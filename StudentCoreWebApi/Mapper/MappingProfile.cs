using AutoMapper;
using StudentCoreWebApi.Model;
using StudentCoreWebApi.DTOs;

namespace StudentCoreWebApi.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));
            CreateMap<User, UserDTO>().ReverseMap();
            CreateMap<AddUser, User>();
            CreateMap<UserResponseDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordSalt, opt => opt.Ignore());
            CreateMap<UpdateUser, User>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<User, UserResponseDto>().ReverseMap();
            CreateMap<UpsertDto, AddUser>().ReverseMap();
            CreateMap<UpsertDto, UpdateUser>().ReverseMap();
            CreateMap<AddUserResponseDto, UpsertDto>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles.Select(r => r.Role_Id).ToList()));

        }
    }
}