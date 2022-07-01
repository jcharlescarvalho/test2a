﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRasta.Plugins.ReverseProxy.HttpMessageHandlers
{
  public class OverrideHostNameResolver : DelegatingHandler
  {
    readonly Func<string, Task<IPAddress[]>> _dnsResolver;
    readonly Action<Exception> _onError;

    public OverrideHostNameResolver(
      HttpMessageHandler inner,
      Func<string, Task<IPAddress[]>> dnsResolver,
      Action<Exception> onError)
      : base(inner)
    {
      _dnsResolver = dnsResolver ?? throw new ArgumentNullException(nameof(dnsResolver));
      _onError = onError ?? throw new ArgumentNullException(nameof(onError));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
      CancellationToken cancellationToken)
    {
      var host = request.RequestUri.Host;
      try
      {
        var ip = await _dnsResolver(host);

        request.RequestUri = new UriBuilder(request.RequestUri) {Host = ip[Environment.TickCount%ip.Length].ToString()}.Uri;
        request.Headers.Host = host;
      }
      catch (Exception e)
      {
        _onError(e);
      }

      return await base.SendAsync(request, cancellationToken);
    }
  }
}