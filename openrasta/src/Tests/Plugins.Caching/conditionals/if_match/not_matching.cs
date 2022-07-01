using OpenRasta.Configuration;
using OpenRasta.Plugins.Caching.Pipeline;
using Shouldly;
using Tests.Plugins.Caching.contexts;
using Xunit;

namespace Tests.Plugins.Caching.conditionals.if_match
{
  public class not_matching : caching
  {
    public not_matching()
    {
      given_resource<TestResource>(map => map.Etag(_ => "v2"));
      given_request_header("if-match", Etag.StrongEtag("v1"));

      when_executing_request("/TestResource");
    }

    [Fact]
    public void returns_not_modified()
    {
      response.StatusCode.ShouldBe(expected: 304);
    }
  }
}