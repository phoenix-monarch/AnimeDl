﻿using System;
using System.Linq;
using System.Collections.Generic;

namespace AnimeDl.Utils;

internal static class UriHelper
{
    public static Dictionary<string, string> DecodeQueryParameters(this Uri uri)
    {
        if (uri == null)
            throw new ArgumentNullException("uri");

        if (uri.Query.Length == 0)
            return new Dictionary<string, string>();

        return uri.Query.TrimStart('?')
            .Split(new[] { '&', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(parameter => parameter.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(parts => parts[0],
                        parts => parts.Length > 2 ? string.Join("=", parts, 1, parts.Length - 1) : (parts.Length > 1 ? parts[1] : ""))
            .ToDictionary(grouping => grouping.Key,
                            grouping => string.Join(",", grouping));
    }
}