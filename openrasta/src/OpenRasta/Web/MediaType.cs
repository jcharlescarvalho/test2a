using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mime;

namespace OpenRasta.Web
{
  /// <summary>
  /// Represents an internet media-type as defined by RFC 2046.
  /// </summary>
  public class MediaType : ContentType, IComparable<MediaType>, IEquatable<MediaType>
  {
    const int MOVE_DOWN = -1;
    const int MOVE_UP = 1;

    public bool Equals(MediaType other)
    {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return other.TopLevelMediaType.Equals(TopLevelMediaType) &&
             other.Subtype.Equals(Subtype) &&
             ParametersAreEqual(other);
    }

    bool ParametersAreEqual(MediaType other)
    {
      if (other.Parameters.Count != Parameters.Count) return false;
      return other.Parameters.Keys
          .Cast<string>()
          .All(parameter => Parameters.ContainsKey(parameter) && Parameters[parameter] == other.Parameters[parameter]);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      return Equals(obj as MediaType);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        int result = base.GetHashCode();
        result = (result * 397) ^ (TopLevelMediaType != null ? TopLevelMediaType.GetHashCode() : 0);
        result = (result * 397) ^ (Subtype != null ? Subtype.GetHashCode() : 0);
        foreach (string parameterName in Parameters.Keys)
          result = (result * 397) ^ (parameterName + Parameters[parameterName]).GetHashCode();
        return result;
      }
    }

    public static bool operator ==(MediaType left, MediaType right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(MediaType left, MediaType right)
    {
      return !Equals(left, right);
    }

    public static readonly MediaType ApplicationOctetStream = new MediaType("application/octet-stream");
    public static readonly MediaType ApplicationXWwwFormUrlencoded = new MediaType("application/x-www-form-urlencoded");
    public static readonly MediaType Html = new MediaType("text/html");
    public static readonly MediaType Json = new MediaType("application/json");
    public static readonly MediaType MultipartFormData = new MediaType("multipart/form-data");
    public static readonly MediaType TextPlain = new MediaType("text/plain");
    public static readonly MediaType Xhtml = new MediaType("application/xhtml+xml");
    public static readonly MediaType XhtmlFragment = new MediaType("application/vnd.openrasta.htmlfragment+xml");
    public static readonly MediaType Xml = new MediaType("application/xml");
    public static readonly MediaType Javascript = new MediaType("text/javascript");

    float _quality;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaType"/> class.
    /// </summary>
    /// <param name="contentType">A <see cref="T:System.String"/>, for example, "text/plain; charset=us-ascii", that contains the internet media type, subtype, and optional parameters.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// 	<paramref name="contentType"/> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// 	<paramref name="contentType"/> is <see cref="F:System.String.Empty"/> ("").
    /// </exception>
    /// <exception cref="T:System.FormatException">
    /// 	<paramref name="contentType"/> is in a form that cannot be parsed.
    /// </exception>
    public MediaType(string contentType)
        : base(contentType)
    {
      if (Parameters.ContainsKey("q"))
      {
        _quality = float.TryParse(Parameters["q"], NumberStyles.Float, CultureInfo.InvariantCulture, out var floatResult)
            ? Math.Min(1, Math.Max(0, floatResult))
            : 0F;
      }
      else
      {
        _quality = 1.0F;
      }

      int slashPos = MediaType.IndexOf('/');
      int semiColumnPos = MediaType.IndexOf(';', slashPos);

      TopLevelMediaType = MediaType.Substring(0, slashPos).Trim();
      Subtype =
          MediaType.Substring(slashPos + 1,
              (semiColumnPos != -1 ? semiColumnPos : MediaType.Length) - slashPos - 1).Trim();
    }

    public float Quality
    {
      get => _quality;
      private set
      {
        _quality = value;
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (value != 1.0f)
          Parameters["q"] = value.ToString("0.###");
        else if (Parameters.ContainsKey("q"))
          Parameters.Remove("q");
      }
    }

    public string TopLevelMediaType { get; private set; }
    public string Subtype { get; private set; }

    public bool IsWildCard => IsTopLevelWildcard && IsSubtypeWildcard;

    public bool IsTopLevelWildcard => TopLevelMediaType == "*";

    public bool IsSubtypeWildcard => Subtype == "*";

    public int CompareTo(MediaType other)
    {
      if (other == null)
        return MOVE_UP;
      if (Equals(other))
        return 0;

      // first, always move down */*
      if (IsWildCard)
      {
        return other.IsWildCard ? 0 : MOVE_DOWN;
      }

      // then sort by quality
      if (Quality != other.Quality)
        return Quality > other.Quality ? MOVE_UP : MOVE_DOWN;

      // then, if the quality is the same, always move application/xml at the end
      if (MediaType == "application/xml")
        return MOVE_DOWN;
      if (other.MediaType == "application/xml")
        return MOVE_UP;

      if (TopLevelMediaType != other.TopLevelMediaType)
      {
        if (IsTopLevelWildcard)
          return MOVE_DOWN;
        return other.IsTopLevelWildcard ? MOVE_UP : TopLevelMediaType.CompareTo(other.TopLevelMediaType);
      }

      if (Subtype != other.Subtype)
      {
        if (IsSubtypeWildcard)
          return MOVE_DOWN;
        if (other.IsSubtypeWildcard)
          return MOVE_UP;
        return Subtype.CompareTo(other.Subtype);
      }

      return 0;
    }

    public MediaType WithQuality(float quality)
    {
      var newMediaType = new MediaType(ToString());
      newMediaType.Quality = quality;
      return newMediaType;
    }

    public MediaType WithoutQuality()
    {
      var newMediatype = new MediaType(ToString());
      newMediatype.Quality = 1.0f;
      return newMediatype;
    }

    public bool Matches(MediaType typeToMatch)
    {
      return (typeToMatch.IsTopLevelWildcard || IsTopLevelWildcard
                                             || TopLevelMediaType == typeToMatch.TopLevelMediaType)
             && (typeToMatch.IsSubtypeWildcard || IsSubtypeWildcard || Subtype == typeToMatch.Subtype);
    }

    public static IEnumerable<MediaType> Parse(string contentTypeList)
    {
      if (contentTypeList == null)
        return new List<MediaType>();

      var contentTypes = contentTypeList.Split(',');
      var mediaTypes = new List<MediaType>();
      foreach (var contentType in contentTypes)
      {
        try
        {
          var trimmed = contentType.Trim();
          if (trimmed.Length == 0)
            continue;
          mediaTypes.Add(new MediaType(trimmed));
        }
        catch (FormatException)
        {
        }
      }

      if (mediaTypes.Any() == false)
        throw new FormatException($"Invalid list of media types: '{contentTypeList}'");

      return mediaTypes.OrderByDescending(m => m).ToArray();
    }

    public class MediaTypeEqualityComparer : IEqualityComparer<MediaType>
    {
      public bool Equals(MediaType x, MediaType y)
      {
        return x.Matches(y);
      }

      public int GetHashCode(MediaType obj)
      {
        return obj.TopLevelMediaType.GetHashCode() ^ obj.Subtype.GetHashCode();
      }
    }
  }
}