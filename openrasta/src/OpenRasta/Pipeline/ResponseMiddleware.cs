﻿using System.Threading.Tasks;
using OpenRasta.Web;

namespace OpenRasta.Pipeline
{
  public class ResponseMiddleware : AbstractContributorMiddleware
  {
    public ResponseMiddleware(ContributorCall call)
      : base(call)
    {
    }

    public override Task Invoke(ICommunicationContext env)
    {
      if (env.PipelineData.TryGetValue("skipToCleanup",out var isSkip) && isSkip is bool skip && skip)
        return Next.Invoke(env);


      if (env.PipelineData.PipelineStage.CurrentState == PipelineContinuation.RenderNow)
        env.PipelineData.PipelineStage.CurrentState = PipelineContinuation.Continue;

      return InvokeCore(env);
    }
    
    async Task InvokeCore(ICommunicationContext env)
    {
      var contribState = await ContributorInvoke(env);

#pragma warning disable 618
      
      switch (contribState)
      {
        case PipelineContinuation.Abort:
          env.PipelineData.PipelineStage.CurrentState = PipelineContinuation.Abort;
          throw new PipelineAbortedException(new[]{new Error()
          {
            Title = "Aborted pipeline",
            Message = "A middleware or contributor aborted the pipeline while rendering the response. It didn't give any reason."
          } });
        case PipelineContinuation.RenderNow:
          env.PipelineData.PipelineStage.CurrentState = PipelineContinuation.RenderNow;
          return;
        default:
          await Next.Invoke(env);
          break;
      }
      
#pragma warning restore 618
    }
  }
}
