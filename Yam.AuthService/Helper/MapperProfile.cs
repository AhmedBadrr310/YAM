using AutoMapper;
using Yam.AuthService.Core.Dtos;
using Yam.Core.sql.Entities;

namespace Yam.AuthService.Helper
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<RegisterDto, ApplicationUser>();
        }
    }
}
