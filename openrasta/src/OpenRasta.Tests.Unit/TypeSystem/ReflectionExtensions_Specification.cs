﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OpenRasta.Tests.Unit.Infrastructure;
using OpenRasta.TypeSystem.ReflectionBased;
using Shouldly;

namespace ReflectionExtensions_Specification
{
    public class when_finding_interfaces : context
    {
        [Test]
        public void an_interface_on_a_type_is_discovered()
        {
          typeof(List<string>).FindInterface(typeof(IList<>)).ShouldBe(typeof(IList<string>));
        }
        [Test]
        public void an_interface_on_an_interface_is_discovered()
        {
          typeof(IList<string>).FindInterface(typeof(IEnumerable<>)).ShouldBe(typeof(IEnumerable<string>));
        }
        [Test]
        public void an_interface_that_is_the_provided_interface_is_discovered()
        {
          typeof(IList<string>).FindInterface(typeof(IList<>)).ShouldBe(typeof(IList<string>));
        }
    }
}
