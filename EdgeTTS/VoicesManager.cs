using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EdgeTTS
{
    /// <summary>
    /// A class to find the correct voice based on their attributes.
    /// </summary>
    public class VoicesManager
    {
        private List<Voice> _voices = new();
        private bool _calledCreate = false;

        private VoicesManager()
        {
        }

        /// <summary>
        /// List all available voices and their attributes.
        /// This pulls data from the URL used by Microsoft Edge to return a list of
        /// all available voices.
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<List<Voice>> ListVoices(
            string? proxy = null, CancellationToken token = default)
        {
            var handler = new HttpClientHandler();
            handler.UseCookies = false;
            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression =
                    DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
            if (proxy != null)
                handler.Proxy = new WebProxy(proxy);

            using (var client = new HttpClient(handler, true))
            {
                using (var request = new HttpRequestMessage(
                    HttpMethod.Get, Constants.VOICE_LIST))
                {
                    request.Headers.Add("Accept", "application/json");
                    request.Headers.Add("Authority", "speech.platform.bing.com");
                    request.Headers.Add("Sec-CH-UA", @""" Not;A Brand"";v=""99"", ""Microsoft Edge"";v=""91"", ""Chromium"";v=""91""");
                    request.Headers.Add("Sec-CH-UA-Mobile", "?0");
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.77 Safari/537.36 Edg/91.0.864.41");
                    request.Headers.Add("Accept", "*/*");
                    request.Headers.Add("Sec-Fetch-Site", "none");
                    request.Headers.Add("Sec-Fetch-Mode", "cors");
                    request.Headers.Add("Sec-Fetch-Dest", "empty");
                    request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                    request.Headers.Add("Accept-Language", "en-US,en;q=0.9");

                    using (var response = await client.SendAsync(request, token))
                    {
                        response.EnsureSuccessStatusCode();

                        var json = await response.Content.ReadAsStringAsync();

                        var voices = JsonConvert.DeserializeObject<List<Voice>>(json) ?? new();

                        return voices
                            .Select(it =>
                            {
                                it.Language = it.Locale.Split('-')[0];
                                return it;
                            })
                            .ToList();
                    }
                }
            }
        }

        public static async Task<VoicesManager> Create(
            List<Voice>? customVoices = null, CancellationToken token = default)
        {
            var manager = new VoicesManager();
            manager._voices = customVoices != null ?
                customVoices : await ListVoices(token: token);
            manager._calledCreate = true;
            return manager;
        }

        /// <summary>
        /// Finds all matching voices based on the provided attributes.
        /// </summary>
        /// <param name="gender"></param>
        /// <param name="language"></param>
        /// <param name="locale"></param>
        /// <param name="shortName"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<Voice> Find(
            string? gender = null,
            string? language = null,
            string? locale = null,
            string? shortName = null,
            string? name = null)
        {
            if (!_calledCreate)
                throw new Exception("VoicesManager.find() called before VoicesManager.create().");

            var matchVoices = _voices as IEnumerable<Voice>;
            if (!string.IsNullOrWhiteSpace(gender))
                matchVoices = matchVoices.Where(it => it.Gender == gender);
            if (!string.IsNullOrWhiteSpace(language))
                matchVoices = matchVoices.Where(it => it.Language == language);
            if (!string.IsNullOrWhiteSpace(locale))
                matchVoices = matchVoices.Where(it => it.Locale == locale);
            if (!string.IsNullOrWhiteSpace(shortName))
                matchVoices = matchVoices.Where(it => it.ShortName == shortName);
            if (!string.IsNullOrWhiteSpace(name))
                matchVoices = matchVoices.Where(it => it.Name == name);
            return matchVoices.ToList();
        }
    }

    public class Voice
    {
        public string Name { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string Gender { get; set; } = "";
        public string Locale { get; set; } = "";
        public string SuggestedCodec { get; set; } = "";
        public string FriendlyName { get; set; } = "";
        public string Status { get; set; } = "";
        public string Language { get; set; } = "";
    }
}
