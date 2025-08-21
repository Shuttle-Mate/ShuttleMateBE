using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.AuthModelViews;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUnitOfWork _unitOfWork;

        public TokenService(IHttpContextAccessor httpContextAccessor, IUnitOfWork unitOfWork)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            _configuration = builder.Build();
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
        }

        public TokenResponse GenerateTokens(User user, string role)
        {
            DateTime now = DateTime.UtcNow;

            // Common claims for both tokens
            List<Claim> claims = new List<Claim>
        {
            new Claim("id", user.Id.ToString()),
            new Claim("role", role)
        };

            var keyString = _configuration.GetSection("JwtSettings:SecretKey").Value ?? string.Empty;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

            var claimsIdentity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(new[] { claimsIdentity });
            _httpContextAccessor.HttpContext.User = principal;

            Console.WriteLine("Check Key:", key);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            // Generate access token
            var accessToken = new JwtSecurityToken(
                claims: claims,
                issuer: _configuration.GetSection("JwtSettings:Issuer").Value,
                audience: _configuration.GetSection("JwtSettings:Audience").Value,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );
            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

            // Generate refresh token
            var refreshToken = new JwtSecurityToken(
                claims: claims,
            issuer: _configuration.GetSection("JwtSettings:Issuer").Value,
            audience: _configuration.GetSection("JwtSettings:Audience").Value,
            expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );
            var refreshTokenString = new JwtSecurityTokenHandler().WriteToken(refreshToken);
            UserRole roleUser = _unitOfWork.GetRepository<UserRole>().Entities.Where(x => x.UserId == user.Id).FirstOrDefault()
                                    ?? throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Lỗi Authorize");
            string roleName = _unitOfWork.GetRepository<Role>().GetById(roleUser.RoleId).Name ?? "Unknow";
            //Lưu refesh vào db để check.
            user.RefeshToken = refreshTokenString;
             _unitOfWork.GetRepository<User>().Update(user);
             _unitOfWork.Save();
            // Return the tokens and user information
            return new TokenResponse
            {
                AccessToken = accessTokenString,
                RefreshToken = refreshTokenString,

                User = new ResponseUserModel
                {
                    Id = user.Id.ToString(),
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    CreatedTime = user.CreatedTime,
                    Role = roleName.ToUpper(),
                    ProfileImageUrl = user.ProfileImageUrl,
                    SchoolId = user.SchoolId,
                }
            };
        }

    }
}
