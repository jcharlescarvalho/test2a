using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenRasta.Hosting.AspNet
{
  public class HttpHandlerRegistration
  {
    readonly Regex _pathRegex;

    public HttpHandlerRegistration(string verb, string path, string type)
    {
      Type = type;
      Methods = verb.Split(',').Select(x => x.Trim());
      Path = path;
      _pathRegex = new Regex("^" + Regex.Escape(path).Replace("\\*", ".*") + "/?$");
    }

    public IEnumerable<string> Methods { get; }
    public string Path { get; }
    public string Type { get; }

    public bool Matches(string httpMethod, Uri path)
    {
      if (!Methods.Contains("*") && Methods.All(x => string.CompareOrdinal(x, httpMethod) != 0))
        return false;

      var simpleMatch = _pathRegex.IsMatch(path.LocalPath);
      return simpleMatch || path.Segments.Any(x => _pathRegex.IsMatch(x));
    }
  }
}