using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.RoleModelViews;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.Services
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task CreateRole(RoleModel model)
        {
            Role role = await _unitOfWork.GetRepository<Role>().Entities.FirstOrDefaultAsync(x => x.Name == model.RoleName);
            if (role != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vai trò đã tồn tại!");
            }
            Role newRole = new Role();
            newRole.Name = model.RoleName;
            await _unitOfWork.GetRepository<Role>().InsertAsync(newRole);
            await _unitOfWork.SaveAsync();
        }
        public async Task<List<ResponseRoleModel>> GetAll()
        {
            var roles = await _unitOfWork.GetRepository<Role>().Entities.Where(x=>!x.DeletedTime.HasValue).OrderBy(x=>x.Name).ToListAsync();
            if (!roles.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có vai trò nào tồn tại!");
            }
            return _mapper.Map<List<ResponseRoleModel>>(roles);

        }
        public async Task UpdateRole(UpdateRoleModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Tên không được để trống!");
            }
            var role = await _unitOfWork.GetRepository<Role>().Entities.FirstOrDefaultAsync(x =>x.Id == model.Id &&  !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy vai trò!");

            role.Name = model.Name;
            await _unitOfWork.GetRepository<Role>().UpdateAsync(role);
            await _unitOfWork.SaveAsync();
        }
        public async Task DeleteRole(DeleteRoleModel model)
        {
            //xóa trong bảng role
            var role = await _unitOfWork.GetRepository<Role>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy vai trò!");
            role.DeletedTime = DateTime.Now;
            await _unitOfWork.GetRepository<Role>().UpdateAsync(role);
            //xóa trong bảng UserRole
            var userRoles = await _unitOfWork.GetRepository<UserRole>().Entities.Where(x=>x.RoleId == role.Id && !x.DeletedTime.HasValue).ToListAsync();
            foreach(var userRole in userRoles)
            {
                userRole.DeletedTime = DateTime.Now;
                await _unitOfWork.GetRepository<UserRole>().UpdateAsync(userRole);
            } 
            await _unitOfWork.SaveAsync();
        }
    }
}
