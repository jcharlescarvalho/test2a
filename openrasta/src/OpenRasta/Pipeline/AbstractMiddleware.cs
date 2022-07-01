﻿using System.Threading.Tasks;
using OpenRasta.Web;

namespace OpenRasta.Pipeline
{
  public abstract class AbstractMiddleware : IPipelineMiddlewareFactory, IPipelineMiddleware
  {
    protected IPipelineMiddleware Next { get; private set; } = Middleware.Identity;

    public virtual IPipelineMiddleware Compose(IPipelineMiddleware next)
    {
      Next = next;
      return this;
    }

    public abstract Task Invoke(ICommunicationContext env);
  }
}