﻿using System;
using System.Diagnostics;
using System.Threading;
using AspNetCoreRateLimit;
using CSRedis;
using DotNetCore.Security;
using FreeSql;
using FreeSql.Internal;
using LinCms.Application.Cms.Files;
using LinCms.Application.Contracts.Cms.Files;
using LinCms.Core.Aop.Middleware;
using LinCms.Core.Data.Options;
using LinCms.Core.Entities;
using LinCms.Core.Security;
using LinCms.Web.Data.Authorization;
using LinCms.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using ToolGood.Words;

namespace LinCms.Web.Startup
{
    public static class DependencyInjectionExtensions
    {
        #region FreeSql
        /// <summary>
        /// FreeSql
        /// </summary>
        /// <param name="services"></param>
        public static void AddContext(this IServiceCollection services, IConfiguration configuration)
        {
            IConfigurationSection configurationSection = configuration.GetSection("ConnectionStrings:MySql");


            IFreeSql fsql = new FreeSqlBuilder()
                   .UseConnectionString(DataType.MySql, configurationSection.Value)
                   .UseNameConvert(NameConvertType.PascalCaseToUnderscoreWithLower)
                   .UseAutoSyncStructure(true)
                   .UseNoneCommandParameter(true)
                   .UseMonitorCommand(cmd =>
                       {
                           Trace.WriteLine(cmd.CommandText + ";");
                       }
                   )
                   .Build()
                   .SetDbContextOptions(opt => opt.EnableAddOrUpdateNavigateList = true);//联级保存功能开启（默认为关闭）



            fsql.Aop.CurdAfter += (s, e) =>
            {
                Log.Debug($"ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}: FullName:{e.EntityType.FullName}" +
                          $" ElapsedMilliseconds:{e.ElapsedMilliseconds}ms, {e.Sql}");

                if (e.ElapsedMilliseconds > 200)
                {
                    //记录日志
                    //发送短信给负责人
                }
            };

            //敏感词处理
            if (configuration["AuditValue:Enable"].ToBoolean())
            {
                IllegalWordsSearch illegalWords = ToolGoodUtils.GetIllegalWordsSearch();

                fsql.Aop.AuditValue += (s, e) =>
                {
                    if (e.Column.CsType == typeof(string) && e.Value != null)
                    {
                        string oldVal = (string)e.Value;
                        string newVal = illegalWords.Replace(oldVal);
                        //第二种处理敏感词的方式
                        //string newVal = oldVal.ReplaceStopWords();
                        if (newVal != oldVal)
                        {
                            e.Value = newVal;
                        }
                    }
                };
            }

            services.AddSingleton(fsql);
            services.AddScoped<UnitOfWorkManager>();
            fsql.GlobalFilter.Apply<IDeleteAduitEntity>("IsDeleted", a => a.IsDeleted == false);
            //在运行时直接生成表结构
            try
            {
                fsql.CodeFirst.SyncStructure(ReflexHelper.GetEntityTypes(typeof(IEntity)));
            }
            catch (Exception e)
            {
                Log.Logger.Error(e+e.StackTrace+e.Message+e.InnerException);
            }
            services.AddFreeRepository();
        }

        #endregion

        #region 初始化 Redis配置
        public static void AddCsRedisCore(this IServiceCollection services, IConfiguration configuration)
        {

            IConfigurationSection csRediSection = configuration.GetSection("ConnectionStrings:CsRedis");
            CSRedisClient csRedisClient = new CSRedisClient(csRediSection.Value);
            //初始化 RedisHelper
            RedisHelper.Initialization(csRedisClient);
            //注册mvc分布式缓存
            services.AddSingleton<IDistributedCache>(new CSRedisCache(RedisHelper.Instance));
        }
        #endregion

        public static void AddSecurity(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHash(10000, 128);
            services.AddCryptography("lin-cms-dotnetcore-cryptography");
            services.AddJsonWebToken(
                new JsonWebTokenSettings(
                        configuration["Authentication:JwtBearer:SecurityKey"],
                        new TimeSpan(30, 0, 0, 0),
                        configuration["Authentication:JwtBearer:Audience"],
                        configuration["Authentication:JwtBearer:Issuer"]
                    )
                );
        }

        public static void AddDIServices(this IServiceCollection services)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddTransient<CustomExceptionMiddleWare>();
            services.AddHttpClient();

            string serviceName = configuration.GetSection("FileStorage:ServiceName").Value;


            if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentNullException("FileStorage:ServiceName未配置");

            services.Configure<FileStorageOption>(configuration.GetSection("FileStorage"));

            if (serviceName == LinFile.LocalFileService)
            {
                services.AddTransient<IFileService, LocalFileService>();
            }
            else
            {
                services.AddTransient<IFileService, QiniuService>();
            }
        }


        public static IServiceCollection AddFreeRepository(this IServiceCollection services)
        {
            services.TryAddScoped(typeof(IBaseRepository<>), typeof(GuidRepository<>));
            services.TryAddScoped(typeof(BaseRepository<>), typeof(GuidRepository<>));
            services.TryAddScoped(typeof(IBaseRepository<,>), typeof(DefaultRepository<,>));
            services.TryAddScoped(typeof(BaseRepository<,>), typeof(DefaultRepository<,>));
            return services;
        }

        /// <summary>
        /// 配置限流依赖的服务
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddIpRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            //加载配置
            services.AddOptions();
            //从IpRateLimiting.json获取相应配置
            services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
            //注入计数器和规则存储
            services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();

            //配置（计数器密钥生成器）
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            return services;
        }
    }
}
