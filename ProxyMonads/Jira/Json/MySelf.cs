using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class Groups {
    public int size { get; set; }
    public List<object> items { get; set; }
  }

  public class MySelf {
    public string self { get; set; }
    public string key { get; set; }
    public string name { get; set; }
    public string emailAddress { get; set; }
    public AvatarUrls avatarUrls { get; set; }
    public string displayName { get; set; }
    public bool active { get; set; }
    public string timeZone { get; set; }
    public Groups groups { get; set; }
    public string expand { get; set; }
  }

}
