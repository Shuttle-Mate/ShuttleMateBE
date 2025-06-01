using ShuttleMate.ModelViews.RoleModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IRoleService
    {
        Task CreateRole(RoleModel model);

    }
}
