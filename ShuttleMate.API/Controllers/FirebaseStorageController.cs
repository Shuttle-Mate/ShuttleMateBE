using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FirebaseStorageController : ControllerBase
    {
        private readonly IFirebaseStorageService _storageService;

        public FirebaseStorageController(IFirebaseStorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File not selected");

            using var stream = file.OpenReadStream();
            var url = await _storageService.UploadAvatarAsync(stream, file.FileName, file.ContentType);

            return Ok(new { AvatarUrl = url });
        }

    }
}
