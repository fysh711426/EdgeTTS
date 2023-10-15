using EdgeTTS;
using Newtonsoft.Json;
using System;
using System.IO;
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

        public static async Task StreamExample(string[] args)
        {
            var TEXT = "hello world";
            var OUTPUT_FILE = "hello.mp3";

            var communicate = new Communicate(TEXT, "zh-CN-YunxiNeural");

            using (var stream = new FileStream(
                OUTPUT_FILE, FileMode.Create, FileAccess.Write))
            {
                await communicate.Stream((result) =>
                {
                    if (result.Type == "audio")
                        result.Data?.CopyTo(stream);

                    if (result.Type == "WordBoundary")
                        Console.WriteLine(JsonConvert.SerializeObject(result));
                });
            }
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

        public static async Task SubMakerExample(string[] args)
        {
            var TEXT = "hello world";
            var OUTPUT_FILE = "hello.mp3";
            var WEBVTT_FILE = "hello.vtt";

            var submaker = new SubMaker();
            var communicate = new Communicate(TEXT, "zh-CN-YunxiNeural");

            using (var stream = new FileStream(
                OUTPUT_FILE, FileMode.Create, FileAccess.Write))
            {
                await communicate.Stream((result) =>
                {
                    if (result.Type == "audio")
                        result.Data?.CopyTo(stream);

                    if (result.Type == "WordBoundary")
                        submaker.CreateSub(Tuple.Create(
                            result.Offset, result.Duration), result.Text);
                });
            }

            using (var stream = new FileStream(
                WEBVTT_FILE, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(submaker.GenerateSubs());
                }
            }
        }
    }
}
