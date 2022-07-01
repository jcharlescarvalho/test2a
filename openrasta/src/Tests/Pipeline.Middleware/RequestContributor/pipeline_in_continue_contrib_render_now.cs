﻿using System.Threading.Tasks;
using OpenRasta.Pipeline;
using Shouldly;
using Tests.Pipeline.Middleware.Infrastructrure;
using Xunit;

namespace Tests.Pipeline.Middleware.RequestContributor
{
  public class pipeline_in_continue_contrib_render_now : middleware_context
  {
    [Fact]
    public async Task pipeline_is_in_RenderNow()
    {
      Env.PipelineData.PipelineStage.CurrentState = PipelineContinuation.Continue;
      var middleware = new RequestMiddleware(Contributor(e => Task.FromResult(PipelineContinuation.RenderNow)))
        .Compose(Next);

      await middleware.Invoke(Env);

      ContributorCalled.ShouldBeTrue();
      NextCalled.ShouldBeTrue();

      Env.PipelineData.PipelineStage.CurrentState.ShouldBe(PipelineContinuation.RenderNow);
    }
  }
}