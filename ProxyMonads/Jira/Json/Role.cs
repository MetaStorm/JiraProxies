using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class Role {
    public string self { get; set; }
    public string name { get; set; }
    public int id { get; set; }
    public string description { get; set; }
    public Actor[] actors { get; set; }
    public override string ToString() {
      return name + ":" + string.Join(",", actors.Select(a => a.displayName));
    }
    public class Actor {
      public int id { get; set; }
      public string displayName { get; set; }
      public string type { get; set; }
      public string name { get; set; }
      public string avatarUrl { get; set; }
      public string Type => type?.Split('-').Skip(1).FirstOrDefault();
      public bool IsUser => Type == "user";
    }
  }
}
