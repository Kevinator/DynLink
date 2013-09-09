
namespace Research.DynamicLink.Server
{
  public class DynLinkRequest
  {
    public string Id { get; set; }
    public string Domain { get; set; }
    public string Action { get; set; }
    public object[] Parameters { get; set; }
  }
}
