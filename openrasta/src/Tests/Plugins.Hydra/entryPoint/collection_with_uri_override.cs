﻿using System.Collections.Generic;
using System.Linq;
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

namespace Tests.Plugins.Hydra.entryPoint
{
  public class collection_with_uri_override : IAsyncLifetime
  {
    IResponse response;
    JToken body;
    readonly InMemoryHost server;

    public collection_with_uri_override()
    {
      server = new InMemoryHost(() =>
      {
        ResourceSpace.Uses.Hydra(options =>
        {
          options.Serializer = ctx => ctx.Transient(() => new PreCompiledUtf8JsonHandler()).As<IMetaModelHandler>();
          options.Vocabulary = "https://schemas.example/schema#";
        });

        ResourceSpace.Has
          .ResourcesOfType<List<Event>>()
          .Vocabulary("https://schemas.example/schema#")
          .AtUri("/events/{location}/")
          .EntryPointCollection(options => options.Uri = "/events/gb/");

        ResourceSpace.Has.ResourcesOfType<Event>()
          .Vocabulary("https://schemas.example/schema#")
          .AtUri("/events/{location}/{id}");
      }, startup: new StartupProperties(){OpenRasta = { Errors = {  HandleAllExceptions = false,HandleCatastrophicExceptions = false}}});
    }


    [Fact]
    public async Task collection_is_linked()
    {
      body["collection"].ShouldBeOfType<JArray>();
      body["collection"][0]["@type"].ShouldBe("hydra:Collection");
      body["collection"][0]["@id"].ShouldBe("http://localhost/events/gb/");
      body["collection"][0].OfType<JProperty>().ShouldNotContain(jProp => jProp.Name == "totalItems");
    }

    public async Task InitializeAsync()
    {
      (response, body) = await server.GetJsonLd("/");
    }

    public Task DisposeAsync() => Task.CompletedTask;
  }
}