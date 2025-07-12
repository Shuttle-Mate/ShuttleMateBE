using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchoolController : ControllerBase
    {
        private readonly ISchoolService _schoolService;
        public SchoolController(ISchoolService schoolService)
        {
            _schoolService = schoolService;
        }

    }
}
