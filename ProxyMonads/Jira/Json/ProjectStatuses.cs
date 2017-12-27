using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class ProjectIssueStatuses {
    /// <summary>
    /// Issue REST path
    /// </summary>
    public string self { get; set; }
    /// <summary>
    /// Issue ID
    /// </summary>
    public string id { get; set; }
    /// <summary>
    /// Issue Name
    /// </summary>
    public string name { get; set; }
    public bool subtask { get; set; }
    /// <summary>
    /// Availible statuses
    /// </summary>
    public List<IssueClasses.Status> statuses { get; set; }
  }
}
