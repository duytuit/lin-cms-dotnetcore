﻿using System;
using System.Threading.Tasks;
using LinCms.Application.Contracts.Blog.Articles.Dtos;
using LinCms.Core.Data;
using LinCms.Core.Entities.Blog;

namespace LinCms.Application.Contracts.Blog.Articles
{
    public interface IArticleService
    {
        Task<Guid> CreateAsync(CreateUpdateArticleDto createArticle);
        Task<Article> UpdateAsync(Guid id, CreateUpdateArticleDto updateArticleDto);
        Task<PagedResultDto<ArticleListDto>> GetArticleAsync(ArticleSearchDto searchDto);

        Task DeleteAsync(Guid id);

        Task<ArticleDto> GetAsync(Guid id);

        Task UpdateTagAsync(Guid id, CreateUpdateArticleDto updateArticleDto);

        Task<PagedResultDto<ArticleListDto>> GetSubscribeArticleAsync(PageDto pageDto);

        Task UpdateLikeQuantityAysnc(Guid subjectId, int likesQuantity);
    }
}
