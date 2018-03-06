using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class JiraNewIssue {
    public string key { get; set; }
    public class Add {
      public string started { get; set; }
      public string timeSpent { get; set; }
    }

    public class Worklog {
      public Add add { get; set; }
    }

    public class Update {
      public List<Worklog> worklog { get; set; }
    }

    public class Parent {
      public string id { get; set; }
      public string key { get; set; }
    }

    public class Version {
      public string id { get; set; }
    }

    public class FixVersion {
      public string id { get; set; }
    }

    public class Fields {
      public Project project { get; set; }
      public string summary { get; set; }
      public IssueType issuetype { get; set; }
      public Parent parent { get; set; }
      public User assignee { get; set; }
      public User reporter { get; set; }
      public Priority priority { get; set; }
      public List<string> labels { get; set; }
      public Timetracking timetracking { get; set; }
      public SecurityLevel security { get; set; }
      public List<Version> versions { get; set; }
      public string environment { get; set; }
      public string description { get; set; }
      public string duedate { get; set; }
      public List<FixVersion> fixVersions { get; set; }
      public List<IssueClasses.Component> components { get; set; }
    }

    public Update update { get; set; }
    public Fields fields { get; set; }

    public static JiraNewIssue Create(string projectId, string issueTypeId, string summary) {
      return Create(projectId, issueTypeId, summary, null, null);
    }
    public static JiraNewIssue Create(string projectId, string issueTypeId, string summary, string description, string[] components) {
      return Create(projectId, issueTypeId, summary, description, null, null, components);
    }
    public static JiraNewIssue Create(string projectKey, string issueType, string summary, string description, string assignee, string reporter, string[] components) {
      var newIssue = new JiraNewIssue() {
        fields = new JiraNewIssue.Fields() {
          project = new Project() {  key = projectKey?.ToUpper() },
          issuetype = new IssueType() { name = issueType },
          summary = summary,
          description = description == "" ? null : description ?? summary,
          assignee = User.FromUserName(assignee),
          reporter = User.FromUserName(reporter),
          components = components == null || !components.Any()
          ? null
          : components.Select(c => new IssueClasses.Component() { name = c }).ToList()
        }
      };
      return newIssue;
    }
  }
}
