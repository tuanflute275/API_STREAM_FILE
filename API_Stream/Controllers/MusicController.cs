using API_Stream.Models;
using Microsoft.AspNetCore.Mvc;

namespace API_Stream.Controllers
{
    [ApiController]
    [Route("/api/music")]    
    public class MusicController : ControllerBase
    {
        private readonly string _musicFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/music");
        [HttpGet("stream/{fileName}")]
        public IActionResult StreamMusic(string fileName = "bauTroiMoi.mp3")
        {
            // Tạo đường dẫn đầy đủ đến file dựa trên tên file và thư mục chứa nhạc
            var filePath = Path.Combine(_musicFolder, fileName);

            // Kiểm tra nếu file không tồn tại, trả về mã lỗi 404 (NotFound)
            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found");

            // Lấy thông tin về file nhạc như kích thước file
            var fileInfo = new FileInfo(filePath);
            long fileSize = fileInfo.Length; // Kích thước file tính bằng bytes
            string contentType = "audio/mpeg"; // Định dạng MIME cho file MP3

            // Kiểm tra xem client có yêu cầu "Range" không (yêu cầu tải một phần của file)
            if (Request.Headers.ContainsKey("Range"))
            {
                // Lấy giá trị Range từ header của request
                var rangeHeader = Request.Headers["Range"].ToString();
                var range = rangeHeader.Replace("bytes=", "").Split('-');

                // Xác định vị trí byte bắt đầu và kết thúc của dải dữ liệu cần tải
                long start = long.Parse(range[0]);
                long end = range.Length > 1 && !string.IsNullOrEmpty(range[1]) ? long.Parse(range[1]) : fileSize - 1;

                // Điều chỉnh phạm vi kết thúc nếu end vượt quá kích thước file
                end = end >= fileSize ? fileSize - 1 : end;
                long contentLength = end - start + 1; // Độ dài nội dung gửi về

                // Cấu hình header trả về mã trạng thái 206 (Partial Content)
                Response.StatusCode = 206;
                Response.ContentType = contentType;
                Response.Headers.Add("Content-Range", $"bytes {start}-{end}/{fileSize}"); // Chỉ rõ dải byte trả về
                Response.Headers.Add("Accept-Ranges", "bytes");

                // Tạo stream từ file và di chuyển vị trí đọc đến byte bắt đầu
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                fileStream.Seek(start, SeekOrigin.Begin);

                // Trả về phần của file nhạc theo yêu cầu (partial stream)
                return File(fileStream, contentType, enableRangeProcessing: true);
            }

            // Trường hợp không có header "Range", trả về toàn bộ file
            return PhysicalFile(filePath, contentType);
        }






        // Mảng tạm thời lưu video trong bộ nhớ
        private static List<Model> musicList = new List<Model>();
        private static int currentId = 1;

        [HttpGet("all")]
        public IActionResult GetAllMusics()
        {
            return Ok(musicList);
        }
      
        [HttpPost("upload")]
        public async Task<IActionResult> SaveMusicAsync(IFormFile musicFile)
        {
            if (musicFile == null || musicFile.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Đọc dữ liệu video vào byte array
            using (var memoryStream = new MemoryStream())
            {
                await musicFile.CopyToAsync(memoryStream);
                var music = new Model
                {
                    Id = currentId++,
                    FileName = musicFile.FileName,
                    Data = memoryStream.ToArray()
                };

                // Lưu vào mảng videoList
                musicList.Add(music);

                return Ok(new { music.Id, music.FileName });
            }
        }


        [HttpGet("streamInMem/{id}")]
        public IActionResult GetMusic(int id)
        {
            var music = musicList.Find(v => v.Id == id);

            if (music == null)
            {
                return NotFound("Music not found.");
            }

            // Trả video dưới dạng stream
            return File(music.Data, "audio/mpeg");
        }

    }
}
