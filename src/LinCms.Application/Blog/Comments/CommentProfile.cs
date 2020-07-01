﻿using AutoMapper;
using LinCms.Application.Contracts.Blog.Comments.Dtos;
using LinCms.Core.Entities.Blog;

namespace LinCms.Application.Blog.Comments
{
    public class CommentProfile : Profile
    {
        public CommentProfile()
        {
            CreateMap<CreateCommentDto, Comment>();
            CreateMap<Comment, CommentDto>().ForMember(d=>d.TopComment,opts=>opts.Ignore());
        }
    }
}
