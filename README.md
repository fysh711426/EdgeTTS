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

---  

### Declare  

This repo is not an official [edge-tts](https://github.com/rany2/edge-tts) product.  