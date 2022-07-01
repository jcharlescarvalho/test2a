﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenRasta.Configuration;
using OpenRasta.Configuration.Fluent;
using OpenRasta.Configuration.Fluent.Extensions;
using OpenRasta.Configuration.MetaModel;
using OpenRasta.Configuration.MetaModel.Handlers;
using OpenRasta.Plugins.ReverseProxy.HttpClientFactory;
using OpenRasta.Plugins.ReverseProxy.HttpMessageHandlers;

namespace OpenRasta.Plugins.ReverseProxy
{
  public static class FluentApiExtensions
  {
    public static void ReverseProxyFor(this IUriDefinition uriConfiguration, string uri)
    {
      uriConfiguration
        .HandledBy<ReverseProxyHandler>();

      var target = (IResourceTarget) uriConfiguration;
      target.Resource.ReverseProxyTarget(uri);
    }

    public static T ReverseProxy<T>(this T uses, ReverseProxyOptions options = null) where T : IUses
    {
      options = options ?? new ReverseProxyOptions();

      if (options.HttpClient.RoundRobin.Enabled)
      {
        var handler = options.HttpClient.Handler;
        Func<ActiveHandler, bool> shouldEvict = null;
        
        if (options.HttpClient.RoundRobin.ClientPerNode)
        {
          var hostResolver = new ServiceResolver(
            options.HttpClient.RoundRobin.DnsResolver,
            options.HttpClient.RoundRobin.OnHostEvicted,
            TimeSpan.FromSeconds(10));

          handler = () => new OverrideHostNameResolver(
            options.HttpClient.Handler(),
            hostResolver.Resolve,
            options.HttpClient.RoundRobin.OnError);

          // shouldEvict = ShouldEvict(hostResolver);
        }

        var factory = new RoundRobinHttpClientFactory(
          options.HttpClient.RoundRobin.ClientCount,
          handler,
          options.HttpClient.RoundRobin.LeaseTime,
          shouldEvict);

        uses.Dependency(d => d.Singleton(() => new ReverseProxy(
          options.Timeout,
          options.ForwardedHeaders.ConvertLegacyHeaders,
          options.Via.Pseudonym,
          factory.GetClient,
          options.OnSend,
          options.OnProxyResponse
        )));
      }
      else
      {
        uses.Dependency(d => d.Singleton(() => new ReverseProxy(
          options.Timeout,
          options.ForwardedHeaders.ConvertLegacyHeaders,
          options.Via.Pseudonym,
          options.HttpClient.Factory,
          options.OnSend,
          options.OnProxyResponse
        )));
      }

      var has = (IHas) uses;
      has.ResourcesOfType<ReverseProxyResponse>()
        .WithoutUri
        .TranscodedBy<ReverseProxyResponseCodec>()
        .ForMediaType("*/*");

      if (options.ForwardedHeaders.RunAsForwardedHost)
        uses.PipelineContributor<RewriteAppBaseUsingForwardedHeaders>();

      return uses;
    }
  }
}