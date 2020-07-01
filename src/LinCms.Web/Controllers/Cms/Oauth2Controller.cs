﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.Security;
using LinCms.Application.Contracts.Cms.Users;
using LinCms.Core.Entities;
using LinCms.Core.Exceptions;
using LinCms.Core.Extensions;
using LinCms.Core.IRepositories;
using LinCms.Core.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LinCms.Web.Controllers.Cms
{
    [Route ("cms/oauth2")]
    [ApiController]
    public class Oauth2Controller : ControllerBase
    {
        private const string LoginProviderKey = "LoginProvider";
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IUserIdentityService _userCommunityService;
        private readonly ILogger<Oauth2Controller> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IJsonWebTokenService _jsonWebTokenService;

        public Oauth2Controller (IHttpContextAccessor contextAccessor, IUserIdentityService userCommunityService, ILogger<Oauth2Controller> logger, IUserRepository userRepository, IJsonWebTokenService jsonWebTokenService)
        {
            _contextAccessor = contextAccessor;
            _userCommunityService = userCommunityService;
            _logger = logger;
            _userRepository = userRepository;
            _jsonWebTokenService = jsonWebTokenService;
        }

        /// <summary>
        /// 授权成功后自动回调的地址
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="redirectUrl">授权成功后的跳转地址</param>
        /// <returns></returns>
        [HttpGet ("signin-callback")]
        public async Task<IActionResult> Home (string provider, string redirectUrl = "")
        {
            if (string.IsNullOrWhiteSpace (provider))
            {
                return BadRequest ();
            }

            if (!await HttpContext.IsProviderSupportedAsync (provider))
            {
                return BadRequest ();
            }

            AuthenticateResult authenticateResult = await _contextAccessor.HttpContext.AuthenticateAsync (provider);
            if (!authenticateResult.Succeeded) return Redirect (redirectUrl);
            var openIdClaim = authenticateResult.Principal.FindFirst (ClaimTypes.NameIdentifier);
            if (openIdClaim == null || string.IsNullOrWhiteSpace (openIdClaim.Value))
                return Redirect (redirectUrl);
            long id = 0;
            switch (provider)
            {
                case LinUserIdentity.GitHub:
                    id = await _userCommunityService.SaveGitHubAsync (authenticateResult.Principal, openIdClaim.Value);
                    break;

                case LinUserIdentity.QQ:
                    id = await _userCommunityService.SaveQQAsync (authenticateResult.Principal, openIdClaim.Value);
                    break;
                case LinUserIdentity.WeiXin:

                    break;
                default:
                    _logger.LogError ($"未知的privoder:{provider},redirectUrl:{redirectUrl}");
                    throw new LinCmsException ($"未知的privoder:{provider}！");
            }
            List<Claim> authClaims = authenticateResult.Principal.Claims.ToList ();

            LinUser user = await _userRepository.Select.IncludeMany (r => r.LinGroups)
                .WhereCascade (r => r.IsDeleted == false).Where (r => r.Id == id).FirstAsync ();

            if (user == null)
            {
                throw new LinCmsException ("第三方登录失败！");
            }
            List<Claim> claims = new List<Claim> ()
            {
                new Claim (ClaimTypes.NameIdentifier, user.Id.ToString ()),
                new Claim (ClaimTypes.Email, user.Email?? ""),
                new Claim (ClaimTypes.GivenName, user.Nickname?? ""),
                new Claim (ClaimTypes.Name, user.Username?? ""),
            };

            user.LinGroups?.ForEach (r =>
            {
                claims.Add (new Claim (LinCmsClaimTypes.Groups, r.Id.ToString ()));
            });

            claims.AddRange (authClaims);
            string token = _jsonWebTokenService.Encode (claims);
            return Redirect ($"{redirectUrl}?token={token}#login-result");
        }

        /// <summary>
        /// https://localhost:5001/cms/oauth2/signin?provider=GitHub&redirectUrl=http://localhost:8080/
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="redirectUrl"></param>
        /// <returns></returns>
        [HttpGet ("signin")]
        public async Task<IActionResult> SignIn (string provider, string redirectUrl)
        {
            // Note: the "provider" parameter corresponds to the external
            // authentication provider choosen by the user agent.
            if (string.IsNullOrWhiteSpace (provider))
            {
                return BadRequest ();
            }

            if (!await HttpContext.IsProviderSupportedAsync (provider))
            {
                return BadRequest ();
            }

            HttpRequest request = _contextAccessor.HttpContext.Request;
            string url = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}-callback?provider={provider}"
                + $"&redirectUrl={redirectUrl}";

            _logger.LogInformation ($"SignIn-url:{url}");
            var properties = new AuthenticationProperties { RedirectUri = url };
            properties.Items[LoginProviderKey] = provider;
            return Challenge (properties, provider);

        }

        [HttpGet ("signout"), HttpPost ("signout")]
        public IActionResult SignOut ()
        {
            // Instruct the cookies middleware to delete the local cookie created
            // when the user agent is redirected from the external identity provider
            // after a successful authentication flow (e.g Google or Facebook).
            return SignOut (new AuthenticationProperties { RedirectUri = "/" }, CookieAuthenticationDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// 通过axios请求得到null，浏览器直接打开能得到github的id 
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        [HttpGet ("OpenId")]
        public async Task<string> OpenId (string provider)
        {
            AuthenticateResult authenticateResult = await _contextAccessor.HttpContext.AuthenticateAsync (provider);
            if (!authenticateResult.Succeeded) return null;
            Claim openIdClaim = authenticateResult.Principal.FindFirst (ClaimTypes.NameIdentifier);
            return openIdClaim?.Value;

        }

        /// <summary>
        /// 通过axios请求，请在header（请求头）携带上文中signin-callback生成的Token值.可以得到openid
        /// </summary>
        /// <returns></returns>
        [HttpGet ("GetOpenIdByToken")]
        public string GetOpenIdByToken ()
        {
            return User.FindFirst (ClaimTypes.NameIdentifier)?.Value;
        }
    }

}