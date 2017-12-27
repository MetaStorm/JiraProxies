using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class SecurityLevel {
    public string self { get; set; }
    public string id { get; set; }
    public string description { get; set; }
    public string name { get; set; }
  }
  public class SecuritySchema {
    public string type { get; set; }
    public string system { get; set; }
  }

}
