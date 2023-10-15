# EdgeTTS  

This repo is C# implementation of [edge-tts](https://github.com/rany2/edge-tts).  

EdgeTTS allows you to use Microsoft Edge's online text-to-speech service from C#.  

---  

### Nuget install  

```
PM> Install-Package EdgeTTS
```

---  

### Example  

```C#
var OUTPUT_FILE = "hello.mp3";
var communicate = new Communicate(
    "hello world", "zh-CN-YunxiNeural");
await communicate.Save(OUTPUT_FILE);
```

### Stream example  

```C#
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
```

### VoicesManager example  

```C#
// List voices
var list = await VoicesManager.ListVoices();

// Finds all matching voices
var manager = await VoicesManager.Create();
var voices = manager.Find(gender: "Male", language: "es");

// Also supports Locales
// var voices = manager.Find(gender: "Female", locale: "es-AR");

var communicate = new Communicate(TEXT, voices[0].Name);
await communicate.Save(OUTPUT_FILE);
```  

### SubMaker example  

```C#
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
```

---  

### Declare  

This repo is not an official [edge-tts](https://github.com/rany2/edge-tts) product.  