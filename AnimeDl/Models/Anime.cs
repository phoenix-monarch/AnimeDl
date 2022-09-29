﻿using System.Collections.Generic;
using AnimeDl.Scrapers;

namespace AnimeDl;

/// <summary>
/// The Class which contains all the information about an Anime
/// </summary>
public class Anime
{
    public int Id { get; set; }

    public AnimeSites Site { get; set; }

    public string Title { get; set; } = default!;
    public int Mal_Id { get; set; }
    public string Slug { get; set; } = default!;

    public string Released { get; set; } = default!;
    public int EpisodesNum { get; set; }
    public string Category { get; set; } = default!;
    public string Link { get; set; } = default!;
    public string Image { get; set; } = default!;
    public float LastWatchedEp { get; set; }

    public string Type { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string OtherNames { get; set; } = default!;
    public string Summary { get; set; } = default!;
    public List<Genre> Genres { get; set; } = new();

    public List<string> Productions { get; set; } = new();
}