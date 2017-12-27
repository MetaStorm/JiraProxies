
namespace Jira.Json {

  public class IssueLink {
    public string id { get; set; }
    public string self { get; set; }
    public Type type { get; set; }
    public Outwardissue inwardIssue { get; set; }
    public Outwardissue outwardIssue { get; set; }

    public class Type {
      public string id { get; set; }
      public string name { get; set; }
      public string inward { get; set; }
      public string outward { get; set; }
      public string self { get; set; }
    }
  }
}