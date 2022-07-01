﻿using NUnit.Framework;
using OpenRasta.OperationModel;
using OpenRasta.Tests.Unit.Infrastructure;
using Shouldly;

namespace OpenRasta.Tests.Unit.OperationModel.MethodBased.Operation
{
  public class when_using_required_members : operation_context<MockOperationHandler>
  {
    [Test]
    public void the_operation_is_not_ready_for_invocation()
    {
      given_operation("Post", typeof(int), typeof(string));

      Operation.Inputs.AllReady().ShouldBeFalse();
    }
    [Test]
    public void no_parameter_is_satisfied()
    {
      given_operation("Post", typeof(int), typeof(string));

      Operation.Inputs.CountReady().ShouldBe(0);
    }
  }
}
