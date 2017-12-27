using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Wcf.ProxyMonads;

namespace Jira.Json {
  public partial class IssueClasses {
    public class Progress {
      public int progress { get; set; }
      public int total { get; set; }
    }


    public class Rootobject {
      public SecurityLevel security { get; set; }
    }

    public class Votes {
      public string self { get; set; }
      public int votes { get; set; }
      public bool hasVoted { get; set; }
    }

    public class Priority {
      public string self { get; set; }
      public string iconUrl { get; set; }
      public string name { get; set; }
      public string id { get; set; }
    }

    public class Watches {
      public string self { get; set; }
      public int watchCount { get; set; }
      public bool isWatching { get; set; }
    }

    public class WorklogField {
      public int startAt { get; set; }
      public int maxResults { get; set; }
      public int total { get; set; }
      public List<WorklogOut> worklogs { get; set; }

    }

    public class WorklogOut {
      public string self { get; set; }
      public Author author { get; set; }
      public Author updateAuthor { get; set; }
      public DateTime created { get; set; }
      public DateTime updated { get; set; }
      public DateTime started { get; set; }
      public string timeSpent { get; set; }
      public int timeSpentSeconds { get; set; }
      public string id { get; set; }
      public string issueId { get; set; }
      public string comment { get; set; }
    }
    public class WorklogIn {
      public string comment { get; set; }
      public string self { get; set; }
      public Author author { get; set; }
      public Author updateAuthor { get; set; }
      public DateTime created { get; set; }
      public DateTime updated { get; set; }
      public string started { get; set; }
      public string timeSpent { get; set; }
      public int timeSpentSeconds { get; set; }
      public string id { get; set; }
      public string issueId { get; set; }
      public static WorklogIn Create(DateTime started, TimeSpan timeSpent, string comment)
        => new WorklogIn { started = started.ToJiraDateTime(), timeSpent = TimeSpan.FromMinutes(Math.Ceiling(timeSpent.TotalMinutes)).ToJiraTime(), comment = comment };
    }

    public class Status {
      public string self { get; set; }
      public string description { get; set; }
      public string iconUrl { get; set; }
      public string name { get; set; }
      public string id { get; set; }
      public StatusCategory statusCategory { get; set; }
    }

    public class Assignee {
      public string self { get; set; }
      public string key { get; set; }
      public string name { get; set; }
      public string emailAddress { get; set; }
      public string displayName { get; set; }
      public bool active { get; set; }
    }

    public class AvatarUrls4 {
      public string __invalid_name__16x16 { get; set; }
      public string __invalid_name__24x24 { get; set; }
      public string __invalid_name__32x32 { get; set; }
      public string __invalid_name__48x48 { get; set; }
    }

    public class Aggregateprogress {
      public int progress { get; set; }
      public int total { get; set; }
    }

    public class AvatarUrls5 {
      public string __invalid_name__16x16 { get; set; }
      public string __invalid_name__24x24 { get; set; }
      public string __invalid_name__32x32 { get; set; }
      public string __invalid_name__48x48 { get; set; }
    }

    public class Author {
      public string self { get; set; }
      public string name { get; set; }
      public string emailAddress { get; set; }
      public AvatarUrls5 avatarUrls { get; set; }
      public string displayName { get; set; }
      public bool active { get; set; }
    }

    public class AvatarUrls6 {
      public string __invalid_name__16x16 { get; set; }
      public string __invalid_name__24x24 { get; set; }
      public string __invalid_name__32x32 { get; set; }
      public string __invalid_name__48x48 { get; set; }
    }


    public class Visibility {
      public string type { get; set; }
      public string value { get; set; }
    }

    public class Comment {
      public string self { get; set; }
      public int id { get; set; }
      public Author author { get; set; }
      public string body { get; set; }
      public Author updateAuthor { get; set; }
      public string created { get; set; }
      public string updated { get; set; }
      public Visibility visibility { get; set; }
      public bool IsSms { get { return GetSms().Success; } }
      public string SmsName { get { return GetSmsName(); } }
      public string SmsPhone { get { return GetSmsPhone(); } }

      public static readonly string smsPattern = @"\ssms(:\[~(?<name>.+)\]|:(?<phone>\d+))*\W*$";
      string GetSmsName() { return GetSms().Groups["name"].Value; }
      string GetSmsPhone() { return GetSms().Groups["phone"].Value; }
      //sms:[~ipbldit]
      Match GetSms() { return Regex.Match(body ?? "", smsPattern); }
    }



    public class Comments {
      public int startAt { get; set; }
      public int maxResults { get; set; }
      public int total { get; set; }
      public List<Comment> comments { get; set; }
    }

    public class Component {
      public string self { get; set; }
      public string id { get; set; }
      public string name { get; set; }
      public Assignee lead { get; set; }
      public string assigneeType { get; set; }
      public Assignee assignee { get; set; }
      public string realAssigneeType { get; set; }
      public Assignee realAssignee { get; set; }
      public bool isAssigneeTypeValid { get; set; }
    }

    public partial class Fields {
      public string summary { get; set; }
      public Progress progress { get; set; }
      public Timetracking timetracking { get; set; }
      public IssueType issuetype { get; set; }
      public Votes votes { get; set; }
      public object resolution { get; set; }
      public List<object> fixVersions { get; set; }
      public object resolutiondate { get; set; }
      public object timespent { get; set; }
      public User creator { get; set; }
      public User reporter { get; set; }
      public object aggregatetimeoriginalestimate { get; set; }
      public DateTimeOffset created { get; set; }
      public DateTimeOffset updated { get; set; }
      public object description { get; set; }
      public Priority priority { get; set; }
      public object duedate { get; set; }
      public List<IssueLink> issuelinks { get; set; }
      public Watches watches { get; set; }
      public WorklogField worklog { get; set; }
      public List<object> subtasks { get; set; }
      public Status status { get; set; }
      public List<object> labels { get; set; }
      public int workratio { get; set; }
      public Assignee assignee { get; set; }
      public List<Attachment> attachment { get; set; }
      public object aggregatetimeestimate { get; set; }
      public Project project { get; set; }
      public List<object> versions { get; set; }
      public object environment { get; set; }
      public object timeestimate { get; set; }
      public Aggregateprogress aggregateprogress { get; set; }
      public string lastViewed { get; set; }
      public List<Component> components { get; set; }
      public Comments comment { get; set; }
      public object timeoriginalestimate { get; set; }
      public object aggregatetimespent { get; set; }
      public SecurityLevel security { get; set; }
    }

    public class History {
      public string id { get; set; }
      public Author author { get; set; }
      public DateTimeOffset created { get; set; }
      public List<HistoryItem> items { get; set; }
    }
    public class HistoryItem {
      public string field { get; set; }
      public string fieldtype { get; set; }
      public string from { get; set; }
      public string fromString { get; set; }
      public string to { get; set; }
      public string toString { get; set; }
    }

    public class Changelog {
      public int startAt { get; set; }
      public int maxResults { get; set; }
      public int total { get; set; }
      public List<History> histories { get; set; }
    }

    public partial class Issue {
      public string expand { get; set; }
      public string id { get; set; }
      public string self { get; set; }
      public string key { get; set; }
      public Fields fields { get; set; }
      public List<IssueTransitions.Transition> transitions { get; set; }
      public Changelog changelog { get; set; }
    }
  }
}
