using System;
using System.Collections.Generic;
using System.Linq;

namespace EdgeTTS
{
    /// <summary>
    /// SubMaker is a class that makes the process of creating subtitles with
    /// information provided by the service easier.
    /// </summary>
    public class SubMaker
    {
        protected List<Tuple<double, double>> _offset = new();
        protected List<string> _subs = new();

        public SubMaker()
        {
        }

        public void CreateSub(Tuple<int, int> timestamp, string text)
            => CreateSub(Tuple.Create(
                (double)timestamp.Item1, 
                (double)timestamp.Item2), text);

        /// <summary>
        /// CreateSub creates a subtitle with the given timestamp and text
        /// and adds it to the list of subtitles.
        /// </summary>
        /// <param name="timestamp">The offset and duration of the subtitle.</param>
        /// <param name="text">The text of the subtitle.</param>
        public void CreateSub(Tuple<double, double> timestamp, string text)
        {
            _offset.Add(Tuple.Create(timestamp.Item1, timestamp.Item1 + timestamp.Item2));
            _subs.Add(text);
        }

        /// <summary>
        /// GenerateSubs generates the complete subtitle file.
        /// </summary>
        /// <param name="wordsInCue">Defines the number of words in a given cue.</param>
        /// <returns></returns>
        public string GenerateSubs(int wordsInCue = 10)
        {
            if (_subs.Count != _offset.Count)
                throw new Exception("subs and offset are not of the same length.");

            if (wordsInCue <= 0)
                throw new Exception("wordsInCue must be greater than 0.");

            var data = "WEBVTT\r\n\r\n";
            var subStateCount = 0;
            var subStateStart = -1.0d;
            var subStateSubs = "";

            var zip = _offset.Zip(_subs, (a, b) => Tuple.Create(a, b));

            var idx = 0;
            foreach (var tuple in zip)
            {
                var offset = tuple.Item1;
                var subs = tuple.Item2;

                var startTime = offset.Item1;
                var endTime = offset.Item2;
                subs = unescape(subs);

                // wordboundary is guaranteed not to contain whitespace
                if (subStateSubs.Length > 0)
                    subStateSubs += " ";
                subStateSubs += subs;

                if (subStateStart == -1.0d)
                    subStateStart = startTime;
                subStateCount += 1;

                if (subStateCount == wordsInCue || idx == _offset.Count - 1)
                {
                    subs = subStateSubs;

                    var splitSubs = new List<string>();
                    for (var i = 0; i < subs.Length; i += 79)
                    {
                        var end = i + 79 > subs.Length ? subs.Length : i + 79;
                        splitSubs.Add(subs.Substring(i, end - i));
                    }
                    for (var i = 0; i < splitSubs.Count - 1; i++)
                    {
                        var sub = splitSubs[i];
                        var splitAtWord = true;
                        if (sub[sub.Length - 1] == ' ')
                        {
                            splitSubs[i] = sub.Substring(0, sub.Length - 1);
                            splitAtWord = false;
                        }
                        if (sub[0] == ' ')
                        {
                            splitSubs[i] = sub.Substring(1);
                            splitAtWord = false;
                        }
                        if (splitAtWord)
                        {
                            splitSubs[i] += "-";
                        }
                    }

                    data += formatter(subStateStart, endTime,
                        string.Join("\r\n", splitSubs));
                    subStateCount = 0;
                    subStateStart = -1;
                    subStateSubs = "";
                }
                idx++;
            }
            return data;
        }

        /// <summary>
        /// formatter returns the timecode and the text of the subtitle.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="subdata"></param>
        /// <returns></returns>
        protected string formatter(double startTime, double endTime, string subdata)
        {
            return
                $"{mktimestamp(startTime)} --> {mktimestamp(endTime)}\r\n" +
                $"{escape(subdata)}\r\n\r\n";
        }

        /// <summary>
        /// mktimestamp returns the timecode of the subtitle.
        /// The timecode is in the format of 00:00:00.000.
        /// </summary>
        /// <param name="timeUnit"></param>
        /// <returns></returns>
        protected string mktimestamp(double timeUnit)
        {
            var hour = Math.Floor(timeUnit / 10_000_000 / 3600);
            var minute = Math.Floor((timeUnit / 10_000_000 / 60) % 60);
            var seconds = (timeUnit / 10_000_000) % 60;
            return $"{hour.ToString("0#")}:{minute.ToString("0#")}:{seconds.ToString("0#.000")}";
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

        /// <summary>
        /// Unescape &amp;, &lt;, and &gt; in a string of data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected static string unescape(string data)
        {
            return data
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&amp;", "&");
        }
    }
}
