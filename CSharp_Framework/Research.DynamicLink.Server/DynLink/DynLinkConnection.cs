using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace Research.DynamicLink.Server
{
  public class DynLinkConnection
  {
    private TcpClient Client;
    private NetworkStream ClientStream;
    private Dictionary<string, Type> CallHandlerTypes;
    private Queue<DynLinkResult> DataToSend = new Queue<DynLinkResult>();
    private DynLinkServer Server;

    public DynLinkConnection(TcpClient Client, DynLinkServer Server)
    {
      this.Client = Client;
      this.ClientStream = Client.GetStream();
      this.Server = Server;
      this.CallHandlerTypes = Server.GetCallHandlerTypes();

      this.Handshake();
    }

    public void RunCommunicationLoop()
    {
      while (!ClientStream.DataAvailable && DataToSend.Count == 0)
        System.Threading.Thread.Sleep(100);

      if (DataToSend.Count != 0)
      {
        var actualData = DataToSend.Dequeue();
        SendAllToClient(new JavaScriptSerializer().Serialize(actualData), true);
      }
      else if (ClientStream.DataAvailable)
      {
        var requ = ReadAllFromClient(true);
        DynLinkRequest request = (DynLinkRequest)new JavaScriptSerializer().Deserialize(requ, typeof(DynLinkRequest));

        //Handle request
        if (request != null)
        {
          Type handler = CallHandlerTypes[request.Domain];
          object result;
          if (request.Parameters != null)
          {
            result = handler.GetMethod(request.Action).Invoke(null, request.Parameters);
          }
          else
          {
            result = handler.GetMethod(request.Action).Invoke(null, new object[0]);
          }
          var answer = new DynLinkResult() { Id = request.Id, Result = result };
          var answerString = new JavaScriptSerializer().Serialize(answer);
          SendAllToClient(answerString, true);
        }
      }
    }

    public void SendMessage(DynLinkResult Message)
    {
      DataToSend.Enqueue(Message);
    }

    private void Handshake()
    {
      //Check valid
      if (!ClientStream.DataAvailable)
      {
        Client.Close();
        return;
      }

      String request = ReadAllFromClient(false);

      if (!Regex.IsMatch(request, ".*Upgrade: websocket.*"))
      {
        Client.Close();
        return;
      }

      //answer
      String SecWebSocketKeyString = Regex.Match(request, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
      Byte[] SecWebSocketAccept = Encoding.UTF8.GetBytes(SecWebSocketKeyString + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");

      var answer =
        "HTTP/1.1 101 Web Socket Protocol Handshake" + Environment.NewLine
      + "Connection: Upgrade" + Environment.NewLine
      + "Upgrade: websocket" + Environment.NewLine
      + "Sec-WebSocket-Accept: " + Convert.ToBase64String(SHA1.Create().ComputeHash(SecWebSocketAccept)) + Environment.NewLine
      + Environment.NewLine;

      SendAllToClient(answer, false);
    }

    #region [Helper]

    private String ReadAllFromClient(bool Decode)
    {
      Byte[] data = new Byte[Client.Available];
      ClientStream.Read(data, 0, data.Length);
      if (Decode)
      {
        data = DecodeData(data);
      }
      return Encoding.UTF8.GetString(data);
    }

    public void SendAllToClient(String Message, bool Encode)
    {
      Byte[] data = Encoding.UTF8.GetBytes(Message);
      if (Encode)
        data = EncodeData(data);
      ClientStream.Write(data, 0, data.Length);
      ClientStream.Flush();
    }

    private Byte[] DecodeData(Byte[] Message)
    {
      int beginnIndexOfMessage;
      switch (Message[1] - 128)
      {
        case 126:
          beginnIndexOfMessage = 4;
          break;
        case 127:
          beginnIndexOfMessage = 5;
          break;
        default:
          beginnIndexOfMessage = 2;
          break;
      }

      Byte[] key = new Byte[4];
      key[0] = Message[beginnIndexOfMessage];
      key[1] = Message[beginnIndexOfMessage + 1];
      key[2] = Message[beginnIndexOfMessage + 2];
      key[3] = Message[beginnIndexOfMessage + 3];
      beginnIndexOfMessage = beginnIndexOfMessage + 4;

      Byte[] encode = new Byte[Message.Length - beginnIndexOfMessage];
      for (int i = beginnIndexOfMessage; i < Message.Length; i++)
      {
        encode[i - beginnIndexOfMessage] = Message[i];
      }
      Byte[] decode = new Byte[encode.Length];

      for (int i = 0; i < encode.Length; i++)
      {
        decode[i] = (Byte)(encode[i] ^ key[i % 4]);
      }

      return decode;
    }

    private Byte[] EncodeData(Byte[] Message)
    {
      int noMessageContent;
      if (Message.LongLength <= 125)
      {
        noMessageContent = 6;
      }
      else if (Message.LongLength <= 65535)
      {
        noMessageContent = 8;
      }
      else
      {
        noMessageContent = 14;
      }

      Byte[] result = new Byte[Message.LongLength + noMessageContent];
      /*
       * 1 Bit Fin?
       * 3 Bit Extension (default 0)
       * 4 Bit Opcode (text 1)
       * 
       * 1 000 0001
       */
      result[0] = (Byte)129;
      /*
       * 1 Bit Mask? (yes 1)
       * 7 Bit Length of Frame (0-125 length OR 126 next two bytes are length OR 127 next eight bytes are length)
       *  optional:
       *  16 Bit or 64 Bit Length of Frame (0-length)
       *  
       * 1 xxxxxxx
       * [xxxxxxxx xxxxxxxx[xxxxxxxx xxxxxxxx xxxxxxxx xxxxxxxx xxxxxxxx xxxxxxxx]]
       * 
       */
      if (Message.LongLength <= 125)
      {
        result[1] = (Byte)(Message.Length + 128);
      }
      else if (Message.LongLength <= 65535)
      {
        result[1] = 126 + 128;
        var bytes = BitConverter.GetBytes(Message.LongLength);
        result[2] = bytes[1];
        result[3] = bytes[0];
      }
      else
      {
        result[1] = 126 + 127;
        var bytes = BitConverter.GetBytes(Message.LongLength);
        result[2] = bytes[7];
        result[3] = bytes[6];
        result[4] = bytes[5];
        result[5] = bytes[4];
        result[6] = bytes[3];
        result[7] = bytes[2];
        result[8] = bytes[1];
        result[9] = bytes[0];
      }
      /*
       * 16 Bit 4-Byte-Mask (here: 0 0 0 0)
       * 
       * 00000000 00000000 00000000 00000000
       */
      result[noMessageContent - 4] = (Byte)0;
      result[noMessageContent - 3] = (Byte)0;
      result[noMessageContent - 2] = (Byte)0;
      result[noMessageContent - 1] = (Byte)0;
      /*
       * Message
       */
      for (int i = 0; i < Message.Length; i++)
      {
        result[i + noMessageContent] = (Byte)(Message[i] ^ (Byte)0);
      }

      return result;
    }

    #endregion
  }
}
