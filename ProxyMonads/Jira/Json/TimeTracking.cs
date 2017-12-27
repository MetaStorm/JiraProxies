using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Jira.Json.IssueClasses;

namespace Jira.Json {
  public class Timetracking {
    public string originalEstimate { get; set; }
    public string remainingEstimate { get; set; }
  }

}
