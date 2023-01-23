﻿using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using AnimeDl.Models;
using AnimeDl.Utils.Extensions;

namespace AnimeDl.Extractors;

/// <summary>
/// Video extractor for tenshi.
/// </summary>
public class TenshiVideoExtractor : VideoExtractor
{
    public TenshiVideoExtractor(HttpClient http,
        VideoServer server) : base(http, server)
    {
    }

    public override async Task<List<Video>> Extract()
    {
        var url = _server.Embed.Url;
        var headers = _server.Embed.Headers;

        var html = await _http.SendHttpRequestAsync(url, headers);

        var list = new List<Video>();

        var unSanitized = html.SubstringAfter("player.source = ").SubstringBefore(";");
        var jObj = JObject.Parse(unSanitized);
        var sources = JArray.Parse(jObj["sources"]!.ToString());

        foreach (var source in sources)
        {
            list.Add(new Video()
            {
                Headers = headers,
                Format = VideoType.Container,
                FileType = source["type"]!.ToString(),
                Resolution = source["size"]!.ToString(),
                VideoUrl = source["src"]!.ToString(),
            });
        }

        return list;
    }
}