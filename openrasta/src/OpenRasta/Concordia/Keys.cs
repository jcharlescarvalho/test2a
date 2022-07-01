﻿namespace OpenRasta.Concordia
{
  public static class Keys
  {
    public static class Request
    {
      public const string PipelineTask = "openrasta.pipeline.completion";
      public const string ResolverRequestScope = "openrasta.di.resolverRequestScope";
    }

    public const string HandleCatastrophicExceptions = "openrasta.errors.HandleCatastrophicExceptions";
    public const string HandleAllExceptions = "openrasta.errors.HandleAllExceptions";
  }
}