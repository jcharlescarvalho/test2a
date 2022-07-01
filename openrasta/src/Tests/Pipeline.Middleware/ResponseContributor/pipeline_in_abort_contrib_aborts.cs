﻿using System.Threading.Tasks;
using OpenRasta.Pipeline;
using Shouldly;
using Tests.Pipeline.Middleware.Infrastructrure;
using Xunit;

namespace Tests.Pipeline.Middleware.ResponseContributor
{
  public class pipeline_in_abort_contrib_aborts : middleware_context
  {
    [Fact]
    public async Task middleware_throws()
    {
      Env.PipelineData.PipelineStage.CurrentState = PipelineContinuation.Abort;

      var middleware = new ResponseMiddleware(Contributor(e => Task.FromResult(PipelineContinuation.Abort)));
      middleware.Invoke(Env).ShouldThrow<PipelineAbortedException>();

      ContributorCalled.ShouldBeTrue();
      Env.PipelineData.PipelineStage.CurrentState.ShouldBe(PipelineContinuation.Abort);
    }
  }
}