﻿using System.Threading.Tasks;
using Shouldly;
using Tests.Plugins.ReverseProxy.Implementation;
using Xunit;

namespace Tests.Plugins.ReverseProxy.via_headers
{
  public class pseudonym_via_header_request_absent
  {
    [Fact]
    public async Task rp_is_added()
    {
      var response = await new ProxyServer()
        .FromServer("/proxy", options=>options.Via.Pseudonym = "componentName")
        .ToServer("/proxied", async ctx => ctx.Request.Headers["Via"])
        .GetAsync("proxy");
      response.Content.ShouldMatch("(\\S+) componentName");
    }
  }
}