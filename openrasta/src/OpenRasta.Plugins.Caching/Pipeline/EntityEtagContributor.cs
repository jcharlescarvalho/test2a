using System;
using System.Linq;
using OpenRasta.Configuration.MetaModel;
using OpenRasta.Pipeline;
using OpenRasta.Plugins.Caching.Configuration;
using OpenRasta.Web;

namespace OpenRasta.Plugins.Caching.Pipeline
{
  public class EntityEtagContributor : IPipelineContributor
  {
    readonly IMetaModelRepository _config;

    public EntityEtagContributor(IMetaModelRepository config)
    {
      _config = config;
    }

    public void Initialize(IPipeline pipelineRunner)
    {
      pipelineRunner.Notify(PostExecution).After<KnownStages.ICodecResponseSelection>()
        .And.Before<ConditionalEtagContributor>();
    }

    PipelineContinuation PostExecution(ICommunicationContext context)
    {
      if (!ShouldSendETag(context)) return PipelineContinuation.Continue;

      var matchingRegistration =
        _config.ResourceRegistrations.FindAll(context.OperationResult.ResponseResource.GetType());

      string nullReader(object resource) => null;
      var reader = matchingRegistration.Select(_ => _.GetEtagMapper())
        .Aggregate((Func<object, string>) nullReader, (src, read) => resource => src(resource) ?? read(resource));

      var partialEtag = reader(context.OperationResult.ResponseResource);
      if (partialEtag == null) return PipelineContinuation.Continue;

      context.Response.Headers[CachingHttpHeaders.Etag] = GenerateEtag(partialEtag);
      return PipelineContinuation.Continue;
    }

    static string GenerateEtag(string partialEtag)
    {
      /* TODO: we should only include components for the headers present in the Vary header
       can't do it now as Vary is not set by OR, bug to fix in 2.1 */

      return Etag.StrongEtag(partialEtag);
    }

    static bool ShouldSendETag(ICommunicationContext context)
    {
      return context.OperationResult.StatusCode == 200 &&
             context.OperationResult.ResponseResource != null &&
             !context.Response.Headers.ContainsKey(CachingHttpHeaders.Etag);
    }
  }
}