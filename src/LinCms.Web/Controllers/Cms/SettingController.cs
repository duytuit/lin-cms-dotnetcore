﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using LinCms.Application.Contracts.Cms.Settings;
using LinCms.Application.Contracts.Cms.Settings.Dtos;
using LinCms.Core.Aop.Filter;
using LinCms.Core.Data;
using LinCms.Core.IRepositories;
using LinCms.Core.Security;
using Microsoft.AspNetCore.Mvc;

namespace LinCms.Web.Controllers.Cms
{
    [Route("cms/settings")]
    [ApiController]
    public class SettingController : ControllerBase
    {
        private readonly ISettingService _settingService;
        private readonly ICurrentUser _currentUser;
        private readonly ISettingRepository _settingRepository;

        public SettingController(ISettingService settingService, ICurrentUser currentUser,
            ISettingRepository settingRepository)
        {
            _settingService = settingService;
            _currentUser = currentUser;
            _settingRepository = settingRepository;
        }

        [LinCmsAuthorize("得到所有设置", "设置")]
        [HttpGet]
        public async Task<PagedResultDto<SettingDto>> GetPagedListAsync([FromQuery] PageDto pageDto)
        {
            return await _settingService.GetPagedListAsync(pageDto);
        }

        [LinCmsAuthorize("删除设置", "设置")]
        [HttpDelete("{id}")]
        public async Task Delete(Guid id)
        {
            await _settingRepository.DeleteAsync(id);
        }

        [LinCmsAuthorize("新增设置", "设置")]
        [HttpPost]
        public async Task CreateAsync([FromBody] CreateUpdateSettingDto createSetting)
        {
            await _settingService.CreateAsync(createSetting);
        }

        [LinCmsAuthorize("修改设置", "设置")]
        [HttpPut("{id}")]
        public async Task UpdateAsync(Guid id, [FromBody] CreateUpdateSettingDto updateSettingDto)
        {
            await _settingService.UpdateAsync(id, updateSettingDto);
        }

        [HttpGet("{id}")]
        public SettingDto Get(Guid id)
        {
            return _settingService.Get(id);
        }

        [HttpPost("set-values")]
        public async Task SetSettingValues(IDictionary<string, string> settingValues)
        {
            foreach (var kValue in settingValues)
            {
                string key = kValue.Key;
                CreateUpdateSettingDto createSetting = new CreateUpdateSettingDto
                {
                    Value = kValue.Value,
                    ProviderName = "U",
                    ProviderKey = _currentUser.Id.ToString(),
                    Name = key
                };
                await _settingService.SetAsync(createSetting);
            }
        }

        [HttpGet("by-key")]
        public async Task<string> GetSettingByKey(string key)
        {
            string providerName = "U";
            string providerKey = _currentUser.Id.ToString();
            return await _settingService.GetOrNullAsync(key, providerName, providerKey);
        }
    }
}