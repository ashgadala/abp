﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.CmsKit.EntityFrameworkCore;
using System.Linq;
using System.Data.Common;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.CmsKit.Users;

namespace Volo.CmsKit.Blogs
{
    public class EfCoreBlogPostRepository : EfCoreRepository<CmsKitDbContext, BlogPost, Guid>, IBlogPostRepository
    {
        public EfCoreBlogPostRepository(IDbContextProvider<CmsKitDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<BlogPost> GetBySlugAsync(Guid blogId, [NotNull] string slug,
            CancellationToken cancellationToken = default)
        {
            Check.NotNullOrEmpty(slug, nameof(slug));

            return await (await WithDetailsAsync())
                       .Where(x =>
                           x.BlogId == blogId && x.Slug.ToLower() == slug)
                       .FirstOrDefaultAsync(cancellationToken: GetCancellationToken(cancellationToken))
                   ?? throw new EntityNotFoundException(typeof(BlogPost));
        }

        public async Task<int> GetCountAsync(Guid blogId, CancellationToken cancellationToken = default)
        {
            return await (await GetQueryableAsync()).CountAsync(
                x => x.BlogId == blogId,
                GetCancellationToken(cancellationToken));
        }

        public async Task<List<BlogPost>> GetPagedListAsync(Guid blogId, int skipCount, int maxResultCount,
            string sorting, bool includeDetails = false, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var blogPostsDbSet = dbContext.Set<BlogPost>();
            var usersDbSet = dbContext.Set<CmsUser>();

            var queryable = blogPostsDbSet
                .Where(x => x.BlogId == blogId);

            if (!sorting.IsNullOrWhiteSpace())
            {
                queryable = queryable.OrderBy(sorting);
            }

            var combinedResult = await queryable
                                        .Join(
                                            usersDbSet,
                                            o => o.AuthorId,  
                                            i => i.Id, 
                                            (blogPost,user) => new { blogPost, user })
                                        .Skip(skipCount)
                                        .Take(maxResultCount)
                                        .ToListAsync();

            return combinedResult.Select(s =>
                                        {
                                            s.blogPost.Author = s.user;
                                            return s.blogPost;
                                        }).ToList();
        }

        public async Task<bool> SlugExistsAsync(Guid blogId, [NotNull] string slug,
            CancellationToken cancellationToken = default)
        {
            Check.NotNullOrEmpty(slug, nameof(slug));

            return await (await GetDbSetAsync()).AnyAsync(x => x.BlogId == blogId && x.Slug.ToLower() == slug,
                GetCancellationToken(cancellationToken));
        }
    }
}