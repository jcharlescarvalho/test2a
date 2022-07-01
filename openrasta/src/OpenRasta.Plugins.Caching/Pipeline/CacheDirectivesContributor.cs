﻿using System.Linq;
using OpenRasta.Pipeline;
using OpenRasta.Plugins.Caching.Providers;
using OpenRasta.Web;

namespace OpenRasta.Plugins.Caching.Pipeline
{
  public class CacheDirectivesContributor : IPipelineContributor
  {
    const string CACHE_CONTROL = "cache-control";

    public void Initialize(IPipeline pipelineRunner)
    {
      pipelineRunner.Notify(WriteCacheDirectives)
        .After<KnownStages.IOperationResultInvocation>()
        .And.Before<KnownStages.IResponseCoding>();
    }

    static PipelineContinuation WriteCacheDirectives(ICommunicationContext arg)
    {
      if (!arg.PipelineData.TryGetValue(CacheKeys.ResponseCache, out var cacheInstructions))
        return PipelineContinuation.Continue;


      var responseCache = (ResponseCachingState) cacheInstructions;

      if (responseCache.CacheDirectives.Any())
        arg.Response.Headers[CACHE_CONTROL] = responseCache.CacheDirectives.JoinString(", ");

      return PipelineContinuation.Continue;
    }
  }
}