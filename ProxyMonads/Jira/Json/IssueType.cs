using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class IssueType {
    public string self { get; set; }
    public string id { get; set; }
    public string description { get; set; }
    public string iconUrl { get; set; }
    public string name { get; set; }
    public bool subtask { get; set; }
    public override string ToString() {
      return new { id, name } + "";
    }
    public static IssueType FromId(int id) => new IssueType { id = id + "" };
  }
}
