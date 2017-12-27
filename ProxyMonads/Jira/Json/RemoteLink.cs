using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class RemoteLink {
    public int id { get; set; }
    public string self { get; set; }
    public LinkObject @object { get; set; }
  }
 
  public class LinkObject {
    public string url { get; set; }
    public string title { get; set; }
  }

}
