﻿using AutoMapper;
using LinCms.Application.Contracts.Blog.Notifications.Dtos;
using LinCms.Core.Entities.Blog;

namespace LinCms.Application.Blog.Notifications
{
    public class NotificationProfile : Profile
    {
        public NotificationProfile()
        {
            CreateMap<Notification, NotificationDto>();
            CreateMap<Article, ArticleEntry>();
            CreateMap<Comment, CommentEntry>();
            CreateMap<CreateNotificationDto, Notification>();
        }
    }
}
