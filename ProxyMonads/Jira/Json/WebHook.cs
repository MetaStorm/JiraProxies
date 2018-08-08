using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {

  public class WebHook {
    public enum issue_event_type_names { issue_commented, issue_updated };
    public long timestamp { get; set; }
    public string webhookEvent { get; set; }
    public string issue_event_type_name { get; set; }
    public User user { get; set; }
    public IssueClasses.Issue issue { get; set; }
    public Changelog changelog { get; set; }
    public IssueClasses.Comment comment { get; set; }
    public Transition transition { get; set; }
    public class Transition {
      public int workflowId { get; set; }
      public string workflowName { get; set; }
      public int transitionId { get; set; }
      public string transitionName { get; set; }
      public string from_status { get; set; }
      public string to_status { get; set; }
    }
  }

  public class Changelog {
    public string id { get; set; }
    public Item[] items { get; set; }
  }

  public class Item {
    public string field { get; set; }
    public string fieldtype { get; set; }
    public string from { get; set; }
    public string fromString { get; set; }
    public string to { get; set; }
    public string toString { get; set; }
  }

}
