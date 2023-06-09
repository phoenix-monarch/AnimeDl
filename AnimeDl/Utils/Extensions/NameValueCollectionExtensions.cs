﻿using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace AnimeDl.Utils.Extensions;

public static class NameValueCollectionExtensions
{
    public static Dictionary<string, string> ToDictionary(
        this NameValueCollection source)
    {
        return source.AllKeys.ToDictionary(k => k!, k => source[k])!;
    }
}