using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class IssueSecurityLevelsScheme {
    public string self { get; set; }
    public int id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public SecurityLevel[] levels { get; set; }
  }


}
