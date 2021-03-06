using OpenRasta.Tests.Unit.Infrastructure;
using Shouldly;
#pragma warning disable 618
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using OpenRasta.Authentication;
using OpenRasta.DI;
using OpenRasta.Pipeline;
using OpenRasta.Pipeline.Contributors;
using OpenRasta.Tests;
using OpenRasta.Web;

namespace Authentication_Specification
{
  [TestFixture]
  public class Authentication_Specification : openrasta_context
  {
    [Test]
    public void Authentication_IsInvokedAfterIBegin()
    {
      // given
      given_pipeline_contributor<AuthenticationContributor>();

      // when
      when_sending_notification<KnownStages.IBegin>();

      // then
      IsContributorExecuted.ShouldBeTrue();
    }

    [Test]
    public void NoAuthHeader()
    {
      // given
      given_pipeline_contributor<AuthenticationContributor>();

      // when
      var result = when_sending_notification<KnownStages.IBegin>();

      // then
      result.ShouldBe(PipelineContinuation.Continue);
    }

    [Test]
    public void AuthHeaderWithUnsupportedScheme()
    {
      // given
      given_pipeline_contributor<AuthenticationContributor>();

      Context.Request.Headers.Add("Authorization", "BASIC anythinghere");

      // when
      var result = when_sending_notification<KnownStages.IBegin>();

      // then
      Context.Response.Headers["Warning"].ShouldBe("199 Unsupported Authentication Scheme");
      result.ShouldBe(PipelineContinuation.Continue);
    }

    [Test]
    public void AuthHeaderWithMalformedHeader()
    {
      // given
      given_pipeline_contributor<AuthenticationContributor>();

      var mockScheme = new Mock<IAuthenticationScheme>();

      mockScheme.SetupGet(s => s.Name).Returns("BASIC");

      mockScheme
          .Setup(s => s.Authenticate(It.IsAny<IRequest>()))
          .Returns(new AuthenticationResult.MalformedCredentials());

      given_dependency(mockScheme.Object);

      Context.Request.Headers.Add("Authorization", "BASIC anythinghere");

      // when
      var result = when_sending_notification<KnownStages.IBegin>();

      // then
      Context.Response.Headers["Warning"].ShouldBe("199 Malformed credentials");
      Context.OperationResult.ShouldBeAssignableTo<OperationResult.BadRequest>();
      result.ShouldBe(PipelineContinuation.RenderNow);
    }

    [Test]
    public void AuthHeaderWithInvalidCredentials()
    {
      // given
      given_pipeline_contributor<AuthenticationContributor>();

      var mockScheme = new Mock<IAuthenticationScheme>();

      mockScheme.SetupGet(s => s.Name).Returns("BASIC");

      mockScheme
          .Setup(s => s.Authenticate(It.IsAny<IRequest>()))
          .Returns(new AuthenticationResult.Failed());

      given_dependency(mockScheme.Object);

      Context.Request.Headers.Add("Authorization", "BASIC anythinghere");

      // when
      var result = when_sending_notification<KnownStages.IBegin>();

      // then
      Context.OperationResult.ShouldBeAssignableTo<OperationResult.Unauthorized>();
      result.ShouldBe(PipelineContinuation.Continue);
    }

    [Test]
    public void AuthHeaderWithValidCredentials()
    {
      // given
      given_pipeline_contributor<AuthenticationContributor>();

      var mockScheme = new Mock<IAuthenticationScheme>();

      mockScheme.SetupGet(s => s.Name).Returns("BASIC");

      var username = "someUsername";
      var roles = new[] { "role1", "role2" };

      mockScheme
          .Setup(s => s.Authenticate(It.IsAny<IRequest>()))
          .Returns(new AuthenticationResult.Success(username, roles));

      given_dependency(mockScheme.Object);

      Context.Request.Headers.Add("Authorization", "BASIC anythinghere");

      // when
      var result = when_sending_notification<KnownStages.IBegin>();

      // then
      result.ShouldBe(PipelineContinuation.Continue);

      Context.User.Identity.Name.ShouldBe(username);
      Context.User.IsInRole(roles[0]);
      Context.User.IsInRole(roles[1]);
    }
  }
}
#pragma warning restore 618