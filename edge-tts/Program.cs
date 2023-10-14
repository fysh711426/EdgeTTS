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

        public static async Task VoicesManagerExample(string[] args)
        {
            var TEXT = "hello world";
            var OUTPUT_FILE = "hello.mp3";

            // List voices
            var list = await VoicesManager.ListVoices();

            // Finds all matching voices
            var manager = await VoicesManager.Create();
            var voices = manager.Find(gender: "Male", language: "es");

            // Also supports Locales
            // var voices = manager.Find(gender: "Female", locale: "es-AR");

            var communicate = new Communicate(TEXT, voices[0].Name);
            await communicate.Save(OUTPUT_FILE);
        }
    }
}
