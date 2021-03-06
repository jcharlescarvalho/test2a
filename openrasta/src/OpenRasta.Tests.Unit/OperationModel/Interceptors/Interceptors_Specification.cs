using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Moq.Language.Flow;
using NUnit.Framework;
using OpenRasta.OperationModel;
using OpenRasta.OperationModel.Interceptors;
using OpenRasta.Tests.Unit.Infrastructure;
using OpenRasta.Tests.Unit.OperationModel.MethodBased;
using Shouldly;

namespace OpenRasta.Tests.Unit.OperationModel.Interceptors
{
  public class when_getting_interceptors_for_an_operation : interceptors_context<HandlerWithInterceptors>
  {
    [Test]
    public void system_interceptors_are_returned()
    {
      var systemInterceptor = new SystemInterceptor();

      given_interceptor_provider(systemInterceptor);
      given_operation("GetALife");

      when_creating_interceptors();

      Interceptors.OfType<SystemInterceptor>().Count().ShouldBe(1);
    }

    [Test]
    public void attribute_interceptor_providers_are_returned()
    {
      given_interceptor_provider();
      given_operation("GetALife");

      when_creating_interceptors();

      Interceptors.OfType<SomeKindOfInterceptorAttribute.InlineInterceptor>().Count().ShouldBe(1);
    }

    [Test]
    public void attribute_interceptors_are_returned()
    {
      given_interceptor_provider();
      given_operation("GetALife");

      when_creating_interceptors();

      Interceptors.OfType<AttributePosingAsAnInterceptor>().Count().ShouldBe(1);
    }
  }

  public class when_wrapping_an_operation_context : interceptors_context<HandlerWithInterceptors>
  {
    [Test]
    public void the_delegated_members_are_the_ones_of_the_wrapped_operation()
    {
      given_operation("GetALife");

      when_creating_wrapper();

      WrappedOperation.ExtendedProperties.ShouldBeSameAs(Operation.ExtendedProperties);
      WrappedOperation.Inputs.ShouldBeSameAs(Operation.Inputs);
      WrappedOperation.Name.ShouldBeSameAs(Operation.Name);
    }

    [Test]
    public void an_interceptor_throwing_an_exception_in_pre_condition_prevents_execution_from_continuing()
    {
      given_mock_operation(op => op.Setup(x => x.Invoke()).Throws<InvalidOperationException>());
      given_mock_interceptor(i => i.BeforeExecute = op=>{throw new ArgumentException();});
      given_wrapper();

      Executing(()=>invoking_wrapped_operation()).ShouldThrow<InterceptorException>()
        .InnerException.ShouldBeAssignableTo<ArgumentException>();
    }

    [Test]
    public void an_interceptor_returning_false_in_pre_condition_prevents_execution_from_continuing()
    {
      given_mock_operation(op => op.Setup(x => x.Invoke()).Throws<InvalidOperationException>());
      given_mock_interceptor(i => i.BeforeExecute = op=>false);
      given_wrapper();

      Executing(()=>invoking_wrapped_operation()).ShouldThrow<InterceptorException>()
        .InnerException.ShouldBeNull();
    }

    [Test]
    public void an_interceptor_throwing_an_exception_in_post_condition_prevents_execution_from_continuing()
    {
      given_mock_operation(op => op.Setup(x => x.Invoke())
        .Returns(new OutputMember[0])
        .Verifiable());
      given_mock_interceptor(i => i.AfterExecute = (op, result) => { throw new ArgumentException(); });

      given_wrapper();

      Executing(()=>invoking_wrapped_operation()).ShouldThrow<InterceptorException>()
        .InnerException.ShouldBeAssignableTo<ArgumentException>();
      MockOperation.Verify(x => x.Invoke());
    }

    [Test]
    public void   an_interceptor_returning_false_in_post_condition_prevents_execution_from_continuing()
    {
      given_mock_operation(op => op.Setup(x => x.Invoke())
        .Returns(new OutputMember[0])
        .Verifiable());
      given_mock_interceptor(interceptor =>
      {
        interceptor.AfterExecute = (op, result) => false;
      });
      given_wrapper();

      Executing(()=>invoking_wrapped_operation()).ShouldThrow<InterceptorException>()
        .InnerException.ShouldBeNull();
      MockOperation.Verify(x => x.Invoke());
    }

    [Test]
    public async Task an_interceptor_can_replace_throwing_call()
    {
      var emptyResult = new OutputMember[1]
        {new OutputMember(){Member = TypeSystem.FromClr(typeof(string)),Value = "Calm down dear!"}};
      given_operation("ThrowOnCall");
      given_mock_interceptor(() => emptyResult);
      given_wrapper();
      when_creating_wrapper();
      invoking_wrapped_operation();

      InvokeResult.Single().Value.ShouldBe("Calm down dear!");
    }
  }

  public class MockInterceptor : IOperationInterceptor
  {
    public Func<IOperation, bool> BeforeExecute { get; set; } = op => true;
    public Func<IOperation, IEnumerable<OutputMember>, bool> AfterExecute { get; set; } = (op,output) => true;
    public Func<Func<IEnumerable<OutputMember>>, Func<IEnumerable<OutputMember>>> RewriteOperation = input => input;
    bool IOperationInterceptor.BeforeExecute(IOperation operation)
    {
      return this.BeforeExecute(operation);
    }

    Func<IEnumerable<OutputMember>> IOperationInterceptor.RewriteOperation(Func<IEnumerable<OutputMember>> operationBuilder)
    {
      return RewriteOperation(operationBuilder);
    }

    bool IOperationInterceptor.AfterExecute(IOperation operation, IEnumerable<OutputMember> outputMembers)
    {
      return AfterExecute(operation, outputMembers);
    }
  }
  public abstract class interceptors_context<T> : sync_operation_context<T>
  {
    SystemAndAttributesOperationInterceptorProvider InterceptorProvider;
    protected IEnumerable<IOperationInterceptor> Interceptors;
    protected IOperation WrappedOperation;
    protected Mock<IOperation> MockOperation;
    protected IEnumerable<OutputMember> InvokeResult;

    protected void when_creating_interceptors()
    {
      Interceptors = InterceptorProvider.GetInterceptors(Operation);
    }

    protected void given_interceptor_provider(params IOperationInterceptor[] interceptors)
    {
      InterceptorProvider = new SystemAndAttributesOperationInterceptorProvider(()=>interceptors);
    }

    protected void given_mock_interceptor(Func<IEnumerable<OutputMember>> overriddenMethod)
    {
      given_mock_interceptor(i => i.RewriteOperation = op=>overriddenMethod);
    }

    protected void given_mock_interceptor(Action<MockInterceptor> interceptorConfig)
    {
      var mock = new MockInterceptor();
      interceptorConfig(mock);
      Interceptors = new[] { mock };
    }

    protected void given_mock_operation(Action<Mock<IOperation>> mockConfig)
    {
      MockOperation = new Mock<IOperation>();
      mockConfig(MockOperation);
      Operation = MockOperation.Object;
    }

    protected void invoking_wrapped_operation()
    {
      InvokeResult = WrappedOperation.Invoke();
    }


    protected void given_wrapper()
    {
      when_creating_wrapper();
    }

    void given_interceptors(params IOperationInterceptor[] interceptors)
    {
      Interceptors = interceptors;
    }

    protected void when_creating_wrapper()
    {
      WrappedOperation = new SyncOperationWithInterceptors(Operation, Interceptors);
    }
  }

  public class HandlerWithInterceptors
  {
    [SomeKindOfInterceptor, AttributePosingAsAnInterceptor]
    public int GetALife()
    {
      return 0;
    }

    public void ThrowOnCall()
    {
      throw new InvalidOperationException();
    }
  }

  public class AttributePosingAsAnInterceptor : InterceptorAttribute
  {
  }

  public class SomeKindOfInterceptorAttribute : Attribute, IOperationInterceptorProvider
  {
    public IEnumerable<IOperationInterceptor> GetInterceptors(IOperation operation)
    {
      yield return new InlineInterceptor();
    }

    public class InlineInterceptor : OperationInterceptor
    {
    }
  }


  public class SystemInterceptor : OperationInterceptor
  {
  }
}
