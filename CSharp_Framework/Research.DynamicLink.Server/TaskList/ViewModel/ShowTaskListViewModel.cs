using Research.DynamicLink.Server.TaskList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Research.DynamicLink.Server
{
  public class ShowTaskListViewModel
  {

    public String Name
    {
      get;
      set;
    }

    public List<Task> Tasks
    {
      get;
      set;
    }
  }
}
