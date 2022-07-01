using System;
using System.Collections.Generic;
using System.Linq;
using OpenRasta.Codecs;
using OpenRasta.Diagnostics;
using OpenRasta.TypeSystem;
using OpenRasta.Web;

namespace OpenRasta.Pipeline.Contributors
{
  public class ResponseEntityCodecResolverContributor : KnownStages.ICodecResponseSelection
  {
    const string HEADER_ACCEPT = "Accept";
    readonly ICodecRepository _codecs;
    readonly ITypeSystem _typeSystem;

    public ResponseEntityCodecResolverContributor(ICodecRepository repository, ITypeSystem typeSystem)
    {
      _codecs = repository;
      _typeSystem = typeSystem;
    }

    // ReSharper disable once MemberCanBePrivate.Global - ioc
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public ILogger Log { get; set; }

    PipelineContinuation FindResponseCodec(ICommunicationContext context)
    {
      if (context.Response.Entity.Instance == null || context.PipelineData.ResponseCodec != null)
      {
        LogNoResponseEntity();
        return PipelineContinuation.Continue;
      }

      var acceptHeader = context.Request.Headers[HEADER_ACCEPT];

      var responseEntityType = _typeSystem.FromInstance(context.Response.Entity.Instance);
      IEnumerable<MediaType> acceptedContentTypes;

      try
      {
        acceptedContentTypes = MediaType.Parse(string.IsNullOrEmpty(acceptHeader) ? "*/*" : acceptHeader).ToList();
      }
      catch (FormatException)
      {
        Log.WriteWarning("Accept header: {0} is malformed", acceptHeader);

        context.Response.Headers["Warning"] = "199 Malformed accept header";
        context.OperationResult = new OperationResult.BadRequest();
        return PipelineContinuation.RenderNow;
      }

      var sortedCodecs = _codecs.FindMediaTypeWriter(responseEntityType, acceptedContentTypes).ToList();
      
      var ext = context.PipelineData.RequestUriFileTypeExtension;
      if (sortedCodecs.Count > 0 && ext != null)
      {
        var matchingExtension =
          sortedCodecs.Where(codec => codec.Extensions.Contains(ext, StringComparer.OrdinalIgnoreCase)).ToList();
        if (matchingExtension.Any())
          sortedCodecs = matchingExtension;
      }
      
      var codecsCount = sortedCodecs.Count;
      
      var negotiatedCodec = sortedCodecs.FirstOrDefault();

      if (negotiatedCodec != null)
      {
        LogCodecSelected(responseEntityType, negotiatedCodec, codecsCount);
        context.Response.Entity.ContentType =
          negotiatedCodec.MediaType.IsWildCard
            ? acceptedContentTypes
              .OrderByDescending(c => c)
              .Where(c => c.IsWildCard == false)
              .DefaultIfEmpty(MediaType.ApplicationOctetStream)
              .FirstOrDefault()
            : negotiatedCodec.MediaType.WithoutQuality();
        context.PipelineData.ResponseCodec = negotiatedCodec;

        context.Response.Headers["Vary"] = "Accept";
      }
      else
      {
        Log.WriteWarning($"Cound not find codec for {responseEntityType} with Accept header {acceptHeader}");
        context.OperationResult = ResponseEntityHasNoCodec(acceptHeader, responseEntityType);
        return PipelineContinuation.RenderNow;
      }

      return PipelineContinuation.Continue;
    }

    public void Initialize(IPipeline pipeline)
    {
      pipeline.Notify(FindResponseCodec);
    }

    static OperationResult.ResponseMediaTypeUnsupported ResponseEntityHasNoCodec(string acceptHeader,
      IType responseEntityType)
    {
      return new OperationResult.ResponseMediaTypeUnsupported
      {
        Title = "The response from the server could not be sent in any format understood by the UA.",
        Description =
          $"Content-type negotiation failed. Resource {responseEntityType} doesn't have any codec for the content-types in the accept header:\r\n{acceptHeader}"
      };
    }

    void LogCodecSelected(IType responseEntityType, CodecRegistration negotiatedCodec, int codecsCount)
    {
      Log.WriteInfo(
        $"Selected codec {negotiatedCodec.CodecType.Name} out of {codecsCount} codecs for entity of type {responseEntityType.Name} and negotiated media type {negotiatedCodec.MediaType}.");
    }

    void LogNoResponseEntity()
    {
      Log.WriteInfo(
        "No response codec was searched for. The response entity is null or a response codec is already set.");
    }
  }
}