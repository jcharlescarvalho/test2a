﻿using System;
using System.Diagnostics;
using System.Security.Principal;
using Moq;
using NUnit.Framework;
using OpenRasta.Hosting.InMemory;
using OpenRasta.OperationModel;
using OpenRasta.Security;
using OpenRasta.Tests.Unit.Infrastructure;
using OpenRasta.Web;
using Shouldly;

namespace RequiresAuthenticationInterceptor_Specification
{
    public class when_the_user_is_not_authenticated : context
    {
        [Test]
        public void execution_is_denied()
        {
            var context = new InMemoryCommunicationContext();

            var authenticationInterceptor = new RequiresAuthenticationInterceptor(context);
          authenticationInterceptor.BeforeExecute(new Mock<IOperation>().Object).ShouldBeFalse();
          context.OperationResult.ShouldBeAssignableTo<OperationResult.Unauthorized>();
        }
    }
    public class when_the_user_is_authenticated : context
    {
        [Test]
        public void execution_is_allowed()
        {
            var context = new InMemoryCommunicationContext();
            context.User = new GenericPrincipal(new GenericIdentity("name"), null);

            var authenticationInterceptor = new RequiresAuthenticationInterceptor(context);

          authenticationInterceptor.BeforeExecute(new Mock<IOperation>().Object).ShouldBeTrue();
        }
    }
}

