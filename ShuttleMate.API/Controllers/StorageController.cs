using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StorageController : ControllerBase
    {
        private readonly ISupabaseService _storageService;

        public StorageController(ISupabaseService storageService)
        {
            _storageService = storageService;
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty.");

            using var stream = file.OpenReadStream();
            var filePath = await _storageService.UploadAsync(stream, file.FileName, file.ContentType);
            var publicUrl = _storageService.GetPublicUrl(filePath);

            return Ok(new
            {
                path = filePath,
                url = publicUrl
            });
        }

        [HttpDelete("delete-avatar")]
        public async Task<IActionResult> DeleteAvatar([FromQuery] string path)
        {
            if (string.IsNullOrEmpty(path))
                return BadRequest("File path is required.");

            await _storageService.DeleteAsync(path);

            return Ok(new { message = "File deleted." });
        }
    }
}
