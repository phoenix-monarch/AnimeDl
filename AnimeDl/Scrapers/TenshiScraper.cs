﻿using System;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using HtmlAgilityPack;
using AnimeDl.Exceptions;
using AnimeDl.Utils.Extensions;
using AnimeDl.Models;
using AnimeDl.Extractors;
using AnimeDl.Extractors.Interfaces;
using AnimeDl.Utils;
using System.Text;
using Newtonsoft.Json;

namespace AnimeDl.Scrapers;

/// <summary>
/// Scraper for interacting with tenshi.
/// </summary>
public class TenshiScraper : BaseScraper
{
    public override string Name { get; set; } = "Tenshi";

    public override bool IsDubAvailableSeparately { get; set; } = false;

    //public override string BaseUrl => "https://tenshi.moe";
    public override string BaseUrl => "https://marin.moe";

    public Dictionary<string, string> DdosCookie = new()
    {
        //{ "Cookie", "__ddg1_=;__ddg2_=;loop-view=thumb" }
        { "Cookie", ";__ddg1_=;__ddg2_=;" }
    };

    //private readonly List<Cookie> Cookies = new()
    //    {
    //        new Cookie("__ddg1_", "") { Domain = "tenshi" },
    //        new Cookie("__ddg2_", "") { Domain = "tenshi" },
    //        new Cookie("loop-view", "thumb") { Domain = "tenshi" },
    //    };

    public override HttpClient _http { get => new(); }

    public TenshiScraper(HttpClient http) : base(http)
    {
    }

    public override async Task<List<Anime>> SearchAsync(string query,
        SearchFilter searchFilter,
        int page,
        bool selectDub)
    {
        var animes = new List<Anime>();

        //query = query.Replace(" ", "%20");
        query = query.Replace(" ", "+");

        //var payload = @"{""filter"":{""type"":[],""status"":[],""content_rating"":[],""genre"":[],""group"":[],""production"":[],""source"":[],""resolution"":[],""audio"":[],""subtitle"":[]},""search"":""anohana""}";

        var data = new
        {
            search = query,
            sort = "vtt-d"
        };

        var payload = JsonConvert.SerializeObject(data);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        for (int i = 0; i < DdosCookie.Count; i++)
            content.Headers.Add(DdosCookie.ElementAt(i).Key, DdosCookie.ElementAt(i).Value);

        var json = await _http.PostAsync($"{BaseUrl}/anime", content);

        var response = searchFilter switch
        {
            SearchFilter.Find => await _http.SendHttpRequestAsync($"{BaseUrl}/anime?q={query}&s=vtt-d", DdosCookie),
            SearchFilter.NewSeason => await _http.SendHttpRequestAsync($"{BaseUrl}/anime?s=rel-d&page=" + page, DdosCookie),
            _ => throw new SearchFilterNotSupportedException("Search filter not supported")
        };

        if (string.IsNullOrEmpty(response))
            return animes;

        var doc = new HtmlDocument();
        doc.LoadHtml(response);

        var nodes = doc.DocumentNode
            .SelectNodes(".//ul[@class='loop anime-loop thumb']/li").ToList();

        foreach (var node in nodes)
        {
            var anime = new Anime();
            anime.Id = node.SelectSingleNode(".//a").Attributes["href"].Value;
            anime.Site = AnimeSites.Tenshi;
            anime.Title = node.SelectSingleNode(".//a").Attributes["title"].Value;
            anime.Link = node.SelectSingleNode(".//a").Attributes["href"].Value;
            anime.Image = node.SelectSingleNode(".//img").Attributes["src"].Value;
            //anime.Summary = node.SelectSingleNode(".//a").Attributes["data-content"].Value;

            animes.Add(anime);
        }

        return animes;
    }

    public override async Task<Anime> GetAnimeInfoAsync(string id)
    {
        var response = await _http.SendHttpRequestAsync(id, DdosCookie);

        var anime = new Anime() { Id = id };

        if (string.IsNullOrEmpty(response))
            return anime;

        var document = new HtmlDocument();
        document.LoadHtml(response);

        var synonymNodes = document.DocumentNode.SelectNodes
            (".//ul[@class='info-list']/li[@class='synonym meta-data']/div[@class='info-box']/span[@class='value']");
        if (synonymNodes is not null)
            anime.OtherNames = synonymNodes[0].InnerText.Trim();

        var typeNode = document.DocumentNode.SelectSingleNode
            (".//ul[@class='info-list']/li[@class='type meta-data']/span[@class='value']/a");
        if (typeNode is not null)
            anime.Type = typeNode.InnerText.Trim();

        var statusNode = document.DocumentNode.SelectSingleNode
            (".//ul[@class='info-list']/li[@class='status meta-data']/span[@class='value']/a");
        if (statusNode is not null)
            anime.Status = statusNode.InnerText.Trim();

        var releasedDateNodes = document.DocumentNode.SelectSingleNode
            (".//ul[@class='info-list']/li[@class='release-date meta-data']/span[@class='value']");
        if (releasedDateNodes is not null)
            anime.Released = releasedDateNodes.InnerText.Trim();

        var productionsNodes = document.DocumentNode.SelectNodes
            (".//ul[@class='info-list']/li[@class='production meta-data']/span[@class='value']")?
            .ToList();
        if (productionsNodes is not null)
            productionsNodes.ForEach(x => anime.Productions.Add(x.InnerText.Trim()));

        var genreNodes = document.DocumentNode.SelectNodes
            (".//ul[@class='info-list']/li[@class='genre meta-data']//a");
        if (genreNodes is not null)
            anime.Genres.AddRange(genreNodes.Select(x => new Genre(x.InnerHtml.Trim())));

        anime.Image = document.DocumentNode.SelectSingleNode(".//img[contains(@class, 'cover-image')]")?
            .Attributes["src"].Value ?? "";

        return anime;
    }

    public override async Task<List<Episode>> GetEpisodesAsync(string id)
    {
        var episodes = new List<Episode>();

        var response = await _http.SendHttpRequestAsync(id, DdosCookie);

        if (string.IsNullOrEmpty(response))
            return episodes;

        var document = new HtmlDocument();
        document.LoadHtml(response);

        //var nodes = document.DocumentNode
        //    .SelectNodes(".//ul[@class='loop episode-loop list']/li").ToList();

        var nodes = document.DocumentNode
            .SelectNodes(".//ul[contains(@class, 'episode-loop')]/li").ToList();

        foreach (var node in nodes)
        {
            var episode = new Episode();

            var titleNode = node.SelectSingleNode(".//div[@class='episode-title']")
                ?? node.SelectSingleNode(".//div[contains(@class, 'episode-label')]");

            var epNumberNode = node.SelectSingleNode(".//div[contains(@class, 'episode-slug')]")
                ?? node.SelectSingleNode(".//div[contains(@class, 'episode-number')]");

            var descNode = node.SelectSingleNode(".//div[contains(@class, 'desc')]")
                ?? node.SelectSingleNode(".//a");

            episode.Name = titleNode.InnerText;
            episode.Number = Convert.ToSingle(epNumberNode.InnerText.Replace("Episode ", ""));
            episode.Image = node.SelectSingleNode(".//img")?.Attributes["src"].Value ?? "";
            episode.Description = descNode.Attributes["data-content"].Value;

            episode.Id = $"{id}/{episode.Number}";
            episode.Link = $"{id}/{episode.Number}";

            if (episode.Name == "No Title")
                episode.Name = $"Ep - {episode.Number}";

            episodes.Add(episode);
        }

        /*var totalEpisodes = Convert.ToInt32(document.DocumentNode
            .SelectSingleNode(".//section[@class='entry-episodes']/h2/span[@class='badge badge-secondary align-top']").InnerText);

        for (int i = 1; i <= totalEpisodes; i++)
        {
            var episode = new Episode
            {
                EpisodeNumber = i,
                EpisodeName = $"Episode {i}",
                EpisodeLink = $"{anime.Link}/{i}"
            };

            episodes.Add(episode);
        }*/

        return episodes;
    }

    public override async Task<List<VideoServer>> GetVideoServersAsync(string episodeId)
    {
        var videoServers = new List<VideoServer>();

        var response = await _http.SendHttpRequestAsync(episodeId, DdosCookie);

        if (string.IsNullOrEmpty(response))
            return videoServers;

        var doc = new HtmlDocument();
        doc.LoadHtml(response);

        var nodes = doc.DocumentNode
            .SelectNodes(".//ul[@class='dropdown-menu']/li/a[@class='dropdown-item']")
            .ToList();

        foreach (var node in nodes)
        {
            var server = node.InnerText.Replace(" ", "").Replace("/-", "").Trim();
            var dub = node.SelectSingleNode(".//span[@title='Audio: English']") != null;
            server = dub ? $"Dub - {server}" : server;

            var urlParam = new Uri(node.Attributes["href"].Value).DecodeQueryParameters();
            var url = $"{BaseUrl}/embed?" + urlParam.FirstOrDefault().Key + "=" + urlParam.FirstOrDefault().Value;
            var headers = new Dictionary<string, string>()
            {
                { "Referer", episodeId }
            };

            for (int i = 0; i < DdosCookie.Count; i++)
                headers.Add(DdosCookie.ElementAt(i).Key, DdosCookie.ElementAt(i).Value);

            videoServers.Add(new VideoServer(server, new FileUrl(url, headers)));
        }

        return videoServers;
    }

    public override IVideoExtractor? GetVideoExtractor(VideoServer server)
        => new TenshiVideoExtractor(_http, server);
}