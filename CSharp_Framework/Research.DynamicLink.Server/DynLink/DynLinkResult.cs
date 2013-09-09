using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Research.DynamicLink.Server
{
  public class DynLinkResult
  {
    public String Id
    {
      get;
      set;
    }

    public String Event
    {
      get;
      set;
    }

    public object Result
    {
      get;
      set;
    }
  }
}
