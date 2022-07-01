﻿using System;
using Shouldly;
using Tests.OperationModel.Interceptors.Support;
using Xunit;

namespace Tests.OperationModel.Interceptors.async_attributes
{
  public class throws : interceptor_scenario
  {
    public throws()
    {
      given_operation<GeneralProductsGuaranteeHandler>(handler => handler.AttackWithLaser());
      when_invoking_operation();
    }

    [Fact]
    public void rewritten_value_returned()
    {
      Error.ShouldBeOfType<InvalidOperationException>();
    }
  }
}