﻿using AutoMapper;
using LinCms.Application.Contracts.Cms.Account;
using LinCms.Application.Contracts.Cms.Admins.Dtos;
using LinCms.Application.Contracts.Cms.Users.Dtos;
using LinCms.Core.Entities;

namespace LinCms.Application.Cms.Users
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<CreateUserDto, LinUser>();
            CreateMap<UpdateUserDto, LinUser>();
            CreateMap<LinUser, UserInformation>();
            CreateMap<LinUser, UserDto>();
            CreateMap<LinUser, OpenUserDto>();
            CreateMap<LinUser, UserNoviceDto>();
            CreateMap<RegisterDto, LinUser>();
        }
    }
}
