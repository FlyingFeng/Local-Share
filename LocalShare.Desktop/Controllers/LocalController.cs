using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace LocalShare.Desktop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocalController : ControllerBase
    {
        [HttpGet("info")]
        public IActionResult Get()
        {
            return Ok(new
            {
                GlobalShared.NodeName,
                GlobalShared.IpAddress,
            });
        }

        [HttpGet("download/{id}")]
        public IActionResult DownloadFile(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var filePath = DownloadLinkHolder.GetDownloadPath(id);
                if (!string.IsNullOrWhiteSpace(filePath) && System.IO.File.Exists(filePath))
                {
                    var fi = new FileInfo(filePath);
                    var contentType = "application/octet-stream";
                    FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    return File(fs, contentType, fi.Name);
                }
            }

            return NotFound();
        }


    }
}
