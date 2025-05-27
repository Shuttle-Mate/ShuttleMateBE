using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.AuthModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface ITokenService
    {
        TokenResponse GenerateTokens(User user, string role);

    }
}
