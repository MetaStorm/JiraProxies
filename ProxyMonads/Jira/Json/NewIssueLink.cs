using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {

  public class NewIssueLink {
    public static string[] IssueLinkTypes {
      get {
        return new[]{
          "Action Items",
          "Duplication",
          "Relationship"
        };
      }
    }
    public static NewIssueLink Create( string inwardIssue, string outwardIssue,string comment,string issueLinkType = "Relationship") {
      return new NewIssueLink {
        type = new IssueLinkType { name = "Relationship" },
        inwardIssue = new Inwardissue { key = inwardIssue },
        outwardIssue = new Outwardissue { key = outwardIssue },
        comment = new IssueClasses.Comment { body = comment }
      };
    }
    public IssueLinkType type { get; set; }
    public Inwardissue inwardIssue { get; set; }
    public Outwardissue outwardIssue { get; set; }
    public IssueClasses.Comment comment { get; set; }
  }

  public class IssueLinkType {
    public string name { get; set; }
  }

  public class Inwardissue {
    public string key { get; set; }
  }


}
