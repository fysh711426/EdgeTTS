using EdgeTTS;
using System.Threading.Tasks;

namespace edge_tts
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var OUTPUT_FILE = "hello.mp3";
            var communicate = new Communicate(
                "hello world", "zh-CN-YunxiNeural");
            await communicate.Save(OUTPUT_FILE);
        }
    }
}
