using API_Stream.Models;
using Microsoft.AspNetCore.Mvc;

namespace API_Stream.Controllers
{
    [ApiController]
    [Route("/api/movie")]    
    public class MovieController : ControllerBase
    {
        private readonly string _videoFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/movie");
        [HttpGet("stream/{fileName}")]
        public async Task<IActionResult> StreamVideo(string fileName = "bauTroiMoi.mp4")
        {
            var filePath = Path.Combine(_videoFolder, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found");

            // Lấy thông tin file
            var fileInfo = new FileInfo(filePath);
            long fileSize = fileInfo.Length;
            string contentType = "video/mp4";

            // Kiểm tra xem có yêu cầu Byte-Range từ client không
            if (Request.Headers.ContainsKey("Range"))
            {
                var rangeHeader = Request.Headers["Range"].ToString();
                var range = rangeHeader.Replace("bytes=", "").Split('-');
                long start = long.Parse(range[0]);
                long end = range.Length > 1 && !string.IsNullOrEmpty(range[1]) ? long.Parse(range[1]) : fileSize - 1;

                // Điều chỉnh phạm vi dữ liệu
                end = end >= fileSize ? fileSize - 1 : end;
                long contentLength = end - start + 1;

                // Trả về mã trạng thái Partial Content
                Response.StatusCode = 206;
                Response.ContentType = contentType;
                Response.Headers.Add("Content-Range", $"bytes {start}-{end}/{fileSize}");
                Response.Headers.Add("Accept-Ranges", "bytes");

                // Stream dữ liệu từ file tới client
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                fileStream.Seek(start, SeekOrigin.Begin);
                var buffer = new byte[8192]; // Buffer nhỏ hơn giúp tối ưu hiệu suất
                int bytesRead;

                // Dùng buffer để stream dữ liệu, giảm bớt tải cho server
                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await Response.Body.WriteAsync(buffer, 0, bytesRead);
                }

                return new EmptyResult(); // Trả về kết quả trống sau khi stream xong
            }

            // Trường hợp không có header Range, trả toàn bộ file
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(stream, contentType);
        }




        // Mảng tạm thời lưu video trong bộ nhớ
        private static List<Model> movieList = new List<Model>();
        private static int currentId = 1;

        [HttpGet("all")]
        public IActionResult GetAllMovies()
        {
            return Ok(movieList);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> SaveMovieAsync(IFormFile movieFile)
        {
            if (movieFile == null || movieFile.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Đọc dữ liệu video vào byte array
            using (var memoryStream = new MemoryStream())
            {
                await movieFile.CopyToAsync(memoryStream);
                var music = new Model
                {
                    Id = currentId++,
                    FileName = movieFile.FileName,
                    Data = memoryStream.ToArray()
                };

                // Lưu vào mảng videoList
                movieList.Add(music);

                return Ok(new { music.Id, music.FileName });
            }
        }


        [HttpGet("streamInMem/{id}")]
        public IActionResult GetMovie(int id)
        {
            var movie = movieList.Find(v => v.Id == id);

            if (movie == null)
            {
                return NotFound("Music not found.");
            }

            // Trả video dưới dạng stream
            return File(movie.Data, "video/mp4");
        }

    }
}
