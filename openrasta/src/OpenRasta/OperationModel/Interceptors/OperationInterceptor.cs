using System;
using System.Collections.Generic;

namespace OpenRasta.OperationModel.Interceptors
{
#pragma warning disable 618

  public abstract class OperationInterceptor : IOperationInterceptor
  {
    public virtual bool AfterExecute(IOperation operation, IEnumerable<OutputMember> outputMembers)
    {
      return true;
    }

    public virtual bool BeforeExecute(IOperation operation)
    {
      return true;
    }

    public virtual Func<IEnumerable<OutputMember>> RewriteOperation(Func<IEnumerable<OutputMember>> operationBuilder)
    {
      return operationBuilder;
    }
  }
#pragma warning restore 618
}