using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EdgeTTS
{
    public class Communicate
    {
        protected string _text = "";
        protected string _voice = "";
        protected string _rate = "";
        protected string _volume = "";
        protected string _pitch = "";
        protected string? _proxy = null;

        /// <summary>
        /// Initializes the Communicate class.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="voice"></param>
        /// <param name="rate"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <param name="proxy"></param>
        public Communicate(
            string text = "",
            string voice = "Microsoft Server Speech Text to Speech Voice (en-US, AriaNeural)",
            string rate = "+0%",
            string volume = "+0%",
            string pitch = "+0Hz",
            string? proxy = null)
        {
            var match = null as Match;

            if (string.IsNullOrWhiteSpace(text))
                throw new Exception("text cannot be empty");
            _text = text;

            // Possible values for voice are:
            // - Microsoft Server Speech Text to Speech Voice (cy-GB, NiaNeural)
            // - cy-GB-NiaNeural
            // - fil-PH-AngeloNeural
            // Always send the first variant as that is what Microsoft Edge does.
            if (string.IsNullOrWhiteSpace(voice))
                throw new Exception("voice cannot be empty");

            _voice = voice;

            match = Regex.Match(voice, @"^([a-z]{2,})-([A-Z]{2,})-(.+Neural)$");
            if (match.Success)
            {
                var lang = match.Groups[1].Value;
                var region = match.Groups[2].Value;
                var name = match.Groups[3].Value;

                var index = name.IndexOf("-");
                if (index != -1)
                {
                    region = region + "-" + name.Substring(0, index);
                    name = name.Substring(index + 1, name.Length - index - 1);
                }
                _voice = $"Microsoft Server Speech Text to Speech Voice ({lang}-{region}, {name})";
            }

            match = Regex.Match(_voice, "^Microsoft Server Speech Text to Speech Voice \\(.+,.+\\)$");
            if (!match.Success)
                throw new Exception($"Invalid voice '{voice}'.");

            if (string.IsNullOrWhiteSpace(rate))
                throw new Exception("rate cannot be empty");
            match = Regex.Match(rate, @"^[+-]\d+%$");
            if (!match.Success)
                throw new Exception($"Invalid rate '{rate}'.");
            _rate = rate;

            if (string.IsNullOrWhiteSpace(volume))
                throw new Exception("volume cannot be empty");
            match = Regex.Match(volume, @"^[+-]\d+%$");
            if (!match.Success)
                throw new Exception($"Invalid volume '{volume}'.");
            _volume = volume;

            if (string.IsNullOrWhiteSpace(pitch))
                throw new Exception("pitch cannot be empty");
            match = Regex.Match(pitch, @"^[+-]\d+Hz$");
            if (!match.Success)
                throw new Exception($"Invalid pitch '{pitch}'.");
            _pitch = pitch;

            if (proxy != null && string.IsNullOrWhiteSpace(proxy))
                throw new Exception("proxy cannot be empty");
            _proxy = proxy;
        }

        /// <summary>
        /// Streams audio and metadata from the service.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task Stream(Action<AudioResult> callback, CancellationToken token = default)
        {
            var texts = splitTextByByteLength(
                escape(removeIncompatibleCharacters(_text)),
                calcMaxMesgSize(_voice, _rate, _volume, _pitch));

            var finalUtterance = new Dictionary<int, int>();
            var prevIdx = -1;
            var shiftTime = -1;

            var idx = 0;
            foreach (var text in texts)
            {
                using (var ws = new ClientWebSocket())
                {
                    var options = ws.Options;
                    options.SetRequestHeader("Pragma", "no-cache");
                    options.SetRequestHeader("Cache-Control", "no-cache");
                    options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
                    options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
                    options.SetRequestHeader("Accept-Language", "en-US,en;q=0.9");
                    options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.77 Safari/537.36 Edg/91.0.864.41");
                    options.SetRequestHeader("Content-Type", "application/json");

                    if (_proxy != null)
                        options.Proxy = new WebProxy(_proxy);

                    await ws.ConnectAsync(
                        new Uri($"{Constants.WSS_URL}&ConnectionId={connectId()}"), token);

                    // download indicates whether we should be expecting audio data,
                    // this is so what we avoid getting binary data from the websocket
                    // and falsely thinking it's audio data.
                    var downloadAudio = false;

                    // audio_was_received indicates whether we have received audio data
                    // from the websocket. This is so we can raise an exception if we
                    // don't receive any audio data.
                    var audioWasReceived = false;

                    // Each message needs to have the proper date.
                    var date = dateToString();

                    // Prepare the request to be sent to the service.
                    //
                    // Note sentenceBoundaryEnabled and wordBoundaryEnabled are actually supposed
                    // to be booleans, but Edge Browser seems to send them as strings.
                    //
                    // This is a bug in Edge as Azure Cognitive Services actually sends them as
                    // bool and not string. For now I will send them as bool unless it causes
                    // any problems.
                    //
                    // Also pay close attention to double { } in request (escape for f-string).
                    await ws.SendAsync(
                        $"X-Timestamp:{date}\r\n" +
                        "Content-Type:application/json; charset=utf-8\r\n" +
                        "Path:speech.config\r\n\r\n" +
                        @"{""context"":{""synthesis"":{""audio"":{""metadataoptions"":{" +
                        @"""sentenceBoundaryEnabled"":false,""wordBoundaryEnabled"":true}," +
                        @"""outputFormat"":""audio-24khz-48kbitrate-mono-mp3""" +
                        "}}}}\r\n", token);

                    await ws.SendAsync(
                        ssmlHeadersPlusData(
                            connectId(), date,
                            mkssml(text, _voice, _rate, _volume, _pitch)
                        ), token);

                    while (ws.State == WebSocketState.Open)
                    {
                        var result = await ws.ReceiveAsync(token);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, token);
                            throw new Exception($"{ws.CloseStatus}: {ws.CloseStatusDescription}");
                        }

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var receiveData = "";
                            using (var reader = new StreamReader(result.Stream))
                            {
                                receiveData = reader.ReadToEnd();
                            }

                            var headersAndData = getHeadersAndData(receiveData);
                            var parameters = headersAndData.Item1;
                            var data = headersAndData.Item2;

                            var path = "";
                            parameters.TryGetValue("Path", out path);

                            if (path == "turn.start")
                            {
                                downloadAudio = true;
                            }
                            else if (path == "turn.end")
                            {
                                downloadAudio = false;
                                break;   // End of audio data
                            }
                            else if (path == "audio.metadata")
                            {
                                var audioMetadata = JsonConvert.DeserializeObject<MetadataModel>(data);
                                if (audioMetadata == null)
                                    continue;

                                foreach (var metaObj in audioMetadata.Metadata)
                                {
                                    var metaType = metaObj.Type;
                                    if (idx != prevIdx)
                                    {
                                        var sum = 0;
                                        for (var i = 0; i < idx; i++)
                                            sum += finalUtterance[i];
                                        shiftTime = sum;
                                        prevIdx = idx;
                                    }
                                    if (metaType == "WordBoundary")
                                    {
                                        finalUtterance[idx] =
                                            metaObj.Data.Offset +
                                            metaObj.Data.Duration +
                                            // Average padding added by the service
                                            // Alternatively we could use ffmpeg to get value properly
                                            // but I don't want to add an additional dependency
                                            // if this is found to work well enough.
                                            8_750_000;

                                        callback(new AudioResult
                                        {
                                            Type = metaType,
                                            Offset = metaObj.Data.Offset + shiftTime,
                                            Duration = metaObj.Data.Duration,
                                            Text = metaObj.Data.text.Text
                                        });
                                    }
                                    else if (metaType == "SessionEnd")
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        throw new Exception($"Unknown metadata type: {metaType}");
                                    }
                                }
                            }
                            else if (path == "response")
                            {
                                // pass
                            }
                            else
                            {
                                throw new Exception(
                                    "The response from the service is not recognized.\n" + receiveData);
                            }
                        }
                        else if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            if (!downloadAudio)
                                throw new Exception("We received a binary message, but we are not expecting one.");

                            if (result.Stream.Length < 2)
                                throw new Exception("We received a binary message, but it is missing the header length.");

                            // See: https://github.com/microsoft/cognitive-services-speech-sdk-js/blob/d071d11/src/common.speech/WebsocketMessageFormatter.ts#L46
                            var flag = new byte[2];
                            result.Stream.Read(flag, 0, flag.Length);

                            // big endian
                            var headerLength = (flag[0] << 8) | (flag[1]);
                            if (result.Stream.Length < headerLength + 2)
                                throw new Exception("We received a binary message, but it is missing the audio data.");

                            if (!audioWasReceived && result.Stream.Length <= headerLength + 2)
                                throw new Exception("We received a binary message, but it is missing the audio data.");

                            result.Stream.Seek(headerLength, SeekOrigin.Current);

                            callback(new AudioResult
                            {
                                Type = "audio",
                                Data = result.Stream
                            });

                            audioWasReceived = true;
                        }
                    }

                    if (!audioWasReceived)
                        throw new Exception("No audio was received. Please verify that your parameters are correct.");
                }
                idx++;
            }
        }

        /// <summary>
        /// Save the audio and metadata to the specified files.
        /// </summary>
        /// <param name="audioFileName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task Save(string audioFileName, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(audioFileName))
                throw new Exception("audioFileName cannot be empty.");

            using (var audioStream = new FileStream(audioFileName, FileMode.Create, FileAccess.Write))
            {
                await Stream((result) =>
                {
                    if (result.Type == "audio")
                    {
                        result.Data?.CopyTo(audioStream);
                    }
                    if (result.Type == "WordBoundary")
                    {
                        Console.WriteLine($"{result.Offset}, {result.Duration}, {result.Text}");
                    }
                }, token);
            }
        }

        /// <summary>
        /// Returns the headers and data from the given data.
        /// </summary>
        /// <param name="data">The data to be parsed.</param>
        /// <returns></returns>
        protected static Tuple<Dictionary<string, string>, string> getHeadersAndData(string data)
        {
            var headers = new Dictionary<string, string>();

            var index = data.IndexOf("\r\n\r\n");

            var lines = data.Substring(0, index).Split(
                new string[] { "\r\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                var split = line.Split(
                    new string[] { ":" }, 2, StringSplitOptions.None);
                var key = split[0];
                var value = split[1];
                headers[key] = value;
            }
            return Tuple.Create(headers, data.Substring(index + 4));
        }

        /// <summary>
        /// The service does not support a couple character ranges.
        /// Most important being the vertical tab character which is
        /// commonly present in OCR-ed PDFs.Not doing this will
        /// result in an error from the service.
        /// </summary>
        /// <param name="str">The string to be cleaned.</param>
        /// <returns></returns>
        protected static string removeIncompatibleCharacters(string str)
        {
            var chars = str.ToCharArray();

            for (var idx = 0; idx < chars.Length; idx++)
            {
                var c = chars[idx];
                var code = (int)c;
                if ((code >= 0 && code <= 8) ||
                    (code >= 11 && code <= 12) ||
                    (code >= 14 && code <= 31))
                {
                    chars[idx] = ' ';
                }
            }
            return new string(chars);
        }

        /// <summary>
        /// Returns a UUID without dashes.
        /// </summary>
        /// <returns></returns>
        protected static string connectId()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Splits a string into a list of strings of a given byte length
        /// while attempting to keep words together.This function assumes
        /// text will be inside of an XML tag.
        /// </summary>
        /// <param name="text">The string to be split.</param>
        /// <param name="byteLength">The maximum byte length of each string in the list.</param>
        /// <returns></returns>
        protected static IEnumerable<string> splitTextByByteLength(string text, int byteLength)
        {
            if (byteLength < 0)
                throw new Exception("byteLength must be greater than 0.");

            while (text.Length > byteLength)
            {
                // Find the last space in the string
                var splitAt = text.LastIndexOf(' ', 0, byteLength);

                // If no space found, split_at is byte_length
                splitAt = splitAt != -1 ? splitAt : byteLength;

                // Verify all & are terminated with a ;
                while (true)
                {
                    var ampersandIndex = text.IndexOf('&', 0, splitAt);
                    if (ampersandIndex == -1)
                        break;

                    if (text.IndexOf(';', ampersandIndex, splitAt) != -1)
                        break;

                    splitAt = ampersandIndex - 1;
                    if (splitAt < 0)
                        throw new Exception("Maximum byte length is too small or invalid text.");
                    if (splitAt == 0)
                        break;
                }

                // Append the string to the list
                var _newText = text.Substring(0, splitAt).Trim();
                if (!string.IsNullOrEmpty(_newText))
                    yield return _newText;
                if (splitAt == 0)
                    splitAt = 1;
                text = text.Substring(splitAt);
            }

            var newText = text.Trim();
            if (!string.IsNullOrEmpty(newText))
                yield return newText;
        }

        /// <summary>
        /// Creates a SSML string from the given parameters.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="voice"></param>
        /// <param name="rate"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <returns></returns>
        protected static string mkssml(string text, string voice, string rate, string volume, string pitch)
        {
            return
                "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
                $"<voice name='{voice}'><prosody pitch='{pitch}' rate='{rate}' volume='{volume}'>" +
                $"{text}</prosody></voice></speak>";
        }

        /// <summary>
        /// Return Javascript-style date string.
        /// </summary>
        /// <returns></returns>
        protected static string dateToString()
        {
            return DateTime.UtcNow.ToString("ddd MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture) +
                " GMT+0000 (Coordinated Universal Time)";
        }

        /// <summary>
        /// Returns the headers and data to be used in the request.
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="timestamp"></param>
        /// <param name="ssml"></param>
        /// <returns></returns>
        protected static string ssmlHeadersPlusData(string requestId, string timestamp, string ssml)
        {
            return
                $"X-RequestId:{requestId}\r\n" +
                "Content-Type:application/ssml+xml\r\n" +
                $"X-Timestamp:{timestamp}Z\r\n" +  // This is not a mistake, Microsoft Edge bug.
                "Path:ssml\r\n\r\n" +
                $"{ssml}";
        }

        /// <summary>
        /// Calculates the maximum message size for the given voice, rate, and volume.
        /// </summary>
        /// <param name="voice"></param>
        /// <param name="rate"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <returns></returns>
        protected static int calcMaxMesgSize(string voice, string rate, string volume, string pitch)
        {
            var websocketMaxSize = (int)Math.Pow(2, 16);
            var overheadPerMessage =
                ssmlHeadersPlusData(
                    connectId(),
                    dateToString(),
                    mkssml("", voice, rate, volume, pitch)
                ).Length + 50;   // margin of error
            return websocketMaxSize - overheadPerMessage;
        }

        /// <summary>
        /// Escape &amp;, &gt;, and &lt; in a string of data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected static string escape(string data)
        {
            return data
                .Replace("&", "&amp;")
                .Replace(">", "&gt;")
                .Replace("<", "&lt;");
        }
    }
}
