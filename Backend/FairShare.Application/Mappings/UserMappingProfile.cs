using AutoMapper;
using FairShare.Application.DTOs.Profile;
using FairShare.Application.DTOs.Users;
using FairShare.Domain.Entities;

namespace FairShare.Application.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            // Admin view of a user (search, block/unblock).
            CreateMap<User, UserResponse>();

            // The user's own profile. Profile updates are applied manually in ProfileService
            // because the bank account number has to be normalized first.
            CreateMap<User, ProfileResponse>();
        }
    }
}
