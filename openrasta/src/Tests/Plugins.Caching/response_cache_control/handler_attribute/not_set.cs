using OpenRasta.Configuration;
using Shouldly;
using Tests.Plugins.Caching.contexts;
using Xunit;

namespace Tests.Plugins.Caching.response_cache_control.handler_attribute
{
  public class not_set : caching
  {
    public not_set()
    {
      given_has(_ => _.ResourcesOfType<Resource>().AtUri("/").HandledBy<CachingHandler>().AsJsonDataContract()
        .ForMediaType("*/*"));
      when_executing_request("/");
    }

    [Fact]
    public void cache_header_not_present()
    {
      response.Headers["cache-control"].ShouldBeNull();
    }

    [Fact]
    public void response_is_ok()
    {
      response.StatusCode.ShouldBe(expected: 200);
    }
  }
}