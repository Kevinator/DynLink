using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Research.DynamicLink.Server
{
  public class Program
  {


    static void Main(string[] args)
    {
      var CallHandlerTypes = new Dictionary<string, Type>();
      CallHandlerTypes.Add("TaskList", typeof(TaskListGuiBackend));
      var Server = new DynLinkServer(CallHandlerTypes);
      Server.Run();
    }
  }
}
