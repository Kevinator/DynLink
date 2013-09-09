using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Research.DynamicLink.Server
{
  /*
   * https://developer.mozilla.org/en-US/docs/WebSockets/Writing_WebSocket_server
   */
  public class DynLinkServer
  {

    private int PORT = 8181;
    private List<DynLinkConnection> Connections = new List<DynLinkConnection>();
    private Dictionary<string, Type> CallHandlerTypes;

    public DynLinkServer(Dictionary<string, Type> CallHandlerTypes)
    {
      this.CallHandlerTypes = CallHandlerTypes;
    }

    public void Run()
    {
      new Thread(RunAcceptConnectionLoop).Start();
    }

    public void RunAcceptConnectionLoop()
    {
      var Listener = new TcpListener(IPAddress.Loopback, PORT);
      Listener.Start();
      while (true)
      {
        var Client = Listener.AcceptTcpClient();
        var Connection = new DynLinkConnection(Client, this);
        Connections.Add(Connection);
        new Thread(Connection.RunCommunicationLoop).Start();
      }

      //Listener.Stop();
    }

    public Dictionary<string, Type> GetCallHandlerTypes()
    {
      return CallHandlerTypes;
    }
  }
}
