﻿using System.Collections.Generic;
using System.Linq;
using OpenRasta.Concordia;
using OpenRasta.DI;
using OpenRasta.Pipeline;
using OpenRasta.Pipeline.CallGraph;
using OpenRasta.Pipeline.Contributors;
using Shouldly;
using Xunit;

namespace Tests.Pipeline.Middleware.Diagnostics
{
  public class enabled
  {
    [Fact]
    public void logging_is_injected()
    {
      IDependencyResolver resolver = new InternalDependencyResolver();
      resolver.AddDependency<IGenerateCallGraphs, TopologicalSortCallGraphGenerator>();
      resolver.AddDependency<IPipelineContributor, PreExecutingContributor>();
      resolver.AddDependency<IPipelineContributor, First>();
      var initialiser = new ThreePhasePipelineInitializer(
        resolver.Resolve<IEnumerable<IPipelineContributor>>(),
        resolver.Resolve<IGenerateCallGraphs>());

      var pipeline = initialiser
        .Initialize(new StartupProperties
        {
          OpenRasta =
          {
            Diagnostics = {TracePipelineExecution = true},
            Pipeline = {Validate = false}
          }
        });

      var factories = pipeline.MiddlewareFactories
        .Where((factory, i) => i % 2 == 0)
        .ToList();
      factories.Count().ShouldBe(pipeline.MiddlewareFactories.Count() / 2);
      factories.ShouldAllBe(factory => factory is LoggingMiddlewareFactory);
    }
  }
}