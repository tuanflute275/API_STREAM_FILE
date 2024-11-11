using System.Diagnostics;

namespace API_Stream.utils
{
    public class Utils
    {
        public void CompressVideo(string inputPath, string outputPath)
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg";  // Chỉ định tên của chương trình FFmpeg
            ffmpeg.StartInfo.Arguments = $"-i \"{inputPath}\" -vcodec h264 -acodec aac \"{outputPath}\""; // Tham số cho FFmpeg để nén video
            ffmpeg.Start(); // Khởi động quá trình FFmpeg
            ffmpeg.WaitForExit(); // Chờ FFmpeg kết thúc trước khi tiếp tục
        }
    }
}
