﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnimeDl
{
    class M3u8Helper
    {
        private Regex ENCRYPTION_DETECTION_REGEX = new Regex("#EXT-X-KEY:METHOD=([^,]+),");
        private Regex ENCRYPTION_URL_IV_REGEX = new Regex("#EXT-X-KEY:METHOD=([^,]+),URI=\"([^\"]+)\"(?:,IV=(.*))?");
        private Regex QUALITY_REGEX = new Regex(@"#EXT-X-STREAM-INF:(?:(?:.*?(?:RESOLUTION=\d+x(\d+)).*?\s+(.*))|(?:.*?\s+(.*)))");
        private Regex TS_EXTENSION_REGEX = new Regex(@"(.*\.ts.*|.*\.jpg.*)"); //.jpg here 'case vizcloud uses .jpg instead of .ts

        public class M3u8Stream
        {
            public string StreamUrl { get; set; }
            public string Quality { get; set; }
            public WebHeaderCollection Headers { get; set; }
        }

        string AbsoluteExtensionDetermination(string url)
        {
            var split = url.Split('/');
            var gg = split[split.Length - 1].Split('?')[0];
            if (gg.Contains("."))
            {
                return gg.Split('.')?.LastOrDefault();
            }

            return null;
        }

        bool IsNotCompleteUrl(string url)
        {
            return !url.Contains("https://") && !url.Contains("http://");
        }

        string GetParentLink(string uri)
        {
            var split = uri.Split('/').ToList();
            split.Remove(split.LastOrDefault());
            return string.Join("/", split);
        }

        public IEnumerable<M3u8Stream> M3u8Generation(M3u8Stream m3u8)
        {
            string m3u8Parent = GetParentLink(m3u8.StreamUrl);
            var response = Http.GetHtml(m3u8.StreamUrl, m3u8.Headers);
            //var response = Http.GetHtml(m3u8Parent, m3u8.Headers);

            foreach (Match match in QUALITY_REGEX.Matches(response))
            {
                //var hh = match.ToString().Split(',');
                //for (int i = 0; i < hh.Length; i++)
                //{
                //    var sdv = JToken.FromObject(hh[i]);
                //}
                //var sd = JToken.FromObject(hh);
                //var shh = JObject.FromObject(match.ToString());

                //var token = JToken.FromObject(match.ToString());

                string quality = match.Groups[1]?.Value;
                string m3u8Link = match.Groups[2]?.Value;
                string m3u8Link2 = match.Groups[3]?.Value;
                if (string.IsNullOrEmpty(m3u8Link))
                {
                    m3u8Link = m3u8Link2;
                }

                if (AbsoluteExtensionDetermination(m3u8Link) == "m3u8")
                {
                    if (IsNotCompleteUrl(m3u8Link))
                    {
                        m3u8Link = $"{m3u8Parent}/{m3u8Link}";
                    }
                }

                yield return new M3u8Stream()
                {
                    Quality = quality,
                    StreamUrl = m3u8Link,
                    Headers = m3u8.Headers
                };
            }

            //yield return new M3u8Stream()
            //{
            //    StreamUrl = m3u8.StreamUrl,
            //    Headers = m3u8.Headers
            //};
        }
    }
}
