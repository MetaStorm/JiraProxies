using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class Attachment {
    public string self { get; set; }
    public string id { get; set; }
    public string filename { get; set; }
    public User author { get; set; }
    public string created { get; set; }
    public int size { get; set; }
    public string mimeType { get; set; }
    public string content { get; set; }
  }
}
