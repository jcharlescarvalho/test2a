using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenRasta.Concordia;
using OpenRasta.Configuration;
using OpenRasta.Configuration.MetaModel.Handlers;
using OpenRasta.Hosting.InMemory;
using OpenRasta.Plugins.Hydra;
using OpenRasta.Plugins.Hydra.Internal.Serialization.Utf8JsonPrecompiled;
using OpenRasta.Web;
using Shouldly;
using Tests.Plugins.Hydra.Implementation;
using Xunit;

namespace Tests.Plugins.Hydra.nodes
{
  public class iri_node : IAsyncLifetime
  {
    IResponse response;
    JToken body;
    readonly InMemoryHost server;

    public iri_node()
    {
      server = new InMemoryHost(() =>
        {
          ResourceSpace.Uses.Hydra(options =>
          {
            options.Vocabulary = "https://schemas.example/schema#";
            options.Serializer = ctx =>
              ctx.Transient(() => new PreCompiledUtf8JsonHandler()).As<IMetaModelHandler>();
          });

          ResourceSpace.Has.ResourcesOfType<Event>()
            .Vocabulary("https://schemas.example/schema#")
            .AtUri("/events/{id}")
            .HandledBy<EventHandler>();

          ResourceSpace.Has.ResourcesOfType<Customer>();
        },
        startup: new StartupProperties()
        {
          OpenRasta =
          {
            Diagnostics = {TracePipelineExecution = false},
            Errors = {HandleAllExceptions = false, HandleCatastrophicExceptions = false}
          }
        });
    }


    [Fact]
    public void content_is_correct()
    {
      body["@id"].ShouldBe("http://localhost/events/2");
      body["@type"].ShouldBe("Event");
      body["@context"].ShouldBe("http://localhost/.hydra/context.jsonld");
    }

    [Fact]
    public void null_strings_not_rendered()
    {
      body.ShouldNotContain(j => j.Path == "alwaysNullString");
    }

    [Fact]
    public void empty_enumerables_not_rendered()
    {
      body.ShouldNotContain(j => j.Path == "customers");
    }

    public async Task InitializeAsync()
    {
      (response, body) = await server.GetJsonLd("/events/2");
    }

    public async Task DisposeAsync() => server.Close();
  }
}


/*

{
  "@context": "http://localhost/.hydra/context.jsonld",
  "@id": "http://localhost/events/2",
  "@type": "Event",
  "id": 2,
  "firstName": "Bilbo Baggins"
}


{
  "id": 2,
  "firstName": "Bilbo Baggins"
}

{
  "@context": "http://localhost/.hydra/context.jsonld",
  "@id": "http://localhost/events/2",
  "@type": "Event",
  "Id": 2,
  "FirstName": "Bilbo Baggins"
}



{
  "@context": "http://localhost/.hydra/context.jsonld",
  "@type": "hydra:Collection",
  "member": [
    {
      "@id": "http://localhost/events/1",
      "@type": "Event",
      "id": 1
    },
    {
      "@id": "http://localhost/events/2",
      "@type": "Event",
      "id": 2
    }
  ],
  "totalItems": 2
}


*/