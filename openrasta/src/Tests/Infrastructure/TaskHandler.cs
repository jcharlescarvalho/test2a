using System.Collections.Generic;
using System.Linq;

namespace Tests.Infrastructure
{
  public class TaskHandler
  {
    public IEnumerable<TaskItem> Get()
    {
      return Enumerable.Empty<TaskItem>();
    }
    public TaskItem Get(int id)
    {
      return new TaskItem();
    }
  }
}