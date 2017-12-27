using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions;
namespace Jira.Json {
  public class WorkflowSchemaWorkflow {
    public string workflow { get; set; }
    public string[] issueTypes { get; set; }
    public bool defaultMapping { get; set; }
    public override string ToString() {
      return new { workflow, issueTypes = issueTypes.ToJson(false), defaultMapping } + "";
    }
  }
}
