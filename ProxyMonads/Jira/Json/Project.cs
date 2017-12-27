using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {  
  public class Project {
    public string expand { get; set; }
    public string self { get; set; }
    public string id { get; set; }
    public string key { get; set; }
    public string description { get; set; }
    public Lead lead { get; set; }
    public object[] components { get; set; }
    public IssueType[] issueTypes { get; set; }
    public string url { get; set; }
    public string assigneeType { get; set; }
    public object[] versions { get; set; }
    public string name { get; set; }
    public Roles roles { get; set; }
    public Avatarurls1 avatarUrls { get; set; }
    public Projectcategory projectCategory { get; set; }
    public string projectTypeKey { get; set; }
    public string projectTemplateKey { get; set; }
  }

  public class ProjectNew {
    public string key { get; set; }
    public string description { get; set; }
    public string lead { get; set; }
    public string url { get; set; }
    public string name { get; set; }
    public int avatarId { get; set; }
    public int categoryId { get; set; }
    public string assigneeType { get; set; }
    public string projectTypeKey { get; set; }
    public string projectTemplateKey { get; set; }
    public int issueSecurityScheme { get; set; }
    public int notificationScheme { get; set; }
    public int permissionScheme { get; set; }
  }

  public class Lead {
    public string self { get; set; }
    public string key { get; set; }
    public string name { get; set; }
    public Avatarurls avatarUrls { get; set; }
    public string displayName { get; set; }
    public bool active { get; set; }
  }

  public class Avatarurls {
    public string _16x16 { get; set; }
    public string _24x24 { get; set; }
    public string _32x32 { get; set; }
    public string _48x48 { get; set; }
  }

  public class Roles {
  }

  public class Avatarurls1 {
    public string _48x48 { get; set; }
    public string _24x24 { get; set; }
    public string _16x16 { get; set; }
    public string _32x32 { get; set; }
  }

  public class Projectcategory {
    public string self { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
  }

}
