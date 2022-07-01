﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenRasta.Concordia;
using OpenRasta.Pipeline;
using Shouldly;
using Tests.Pipeline.Middleware.Examples;
using Xunit;

namespace Tests.Pipeline.Middleware.Trailers
{
  public class trailered_middleware
  {
    [Fact]
    public void middleware_is_trailered()
    {
      var calls = new[]
      {
        new ContributorCall(new DoNothingContributor(), OpenRasta.Pipeline.Middleware.IdentitySingleTap, "doNothing")
      };
      var middlewareChain = calls.ToMiddleware(
          new StartupProperties
          {
            OpenRasta =
            {
              Pipeline =
              {
                ContributorTrailers =
                {
                  [call => call.Target is DoNothingContributor] = () => new TrailerMiddleware()
                }
              }
            }
          })
        .ToArray();

      middlewareChain[0].ShouldBeOfType<PreExecuteMiddleware>();
      middlewareChain[1].ShouldBeOfType<TrailerMiddleware>();
    }
  }
}