using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {

  public class ProjectConfigRequest {
    public string name { get; set; }
    public string displayName { get; set; }
    public string[] issueTypes { get; set; }
    public bool @default { get; set; } = false;
    public string description { get; set; }
    public bool system { get; set; } = false;
    public static ProjectConfigRequest WorkfkowIssueTypeDraft(string workflowName,string issueTypeId) {
      return new ProjectConfigRequest {
        displayName = workflowName,
        name=workflowName,
        issueTypes = new[] {issueTypeId}
      };
    }
  }

  public class ProjectConfigResponse {

    /// <summary>
    /// workflow scheme name
    /// </summary>
    public string name { get; set; }
    /// <summary>
    /// draft workflow scheme id
    /// </summary>
    public int id { get; set; }
    /// <summary>
    /// shared projects
    /// </summary>
    //public Project[] shared { get; set; }
    /// <summary>
    /// mapping between workflow and issue type
    /// </summary>
    public WorkflowMapping[] mappings { get; set; }
    public Issuetype[] issueTypes { get; set; }
    public bool admin { get; set; }
    public bool sysAdmin { get; set; }
    public string lastModifiedDate { get; set; }
    public Lastmodifieduser lastModifiedUser { get; set; }
    public bool defaultScheme { get; set; }
    public int totalWorkflows { get; set; }
    public bool draftScheme { get; set; }
    public string currentUser { get; set; }
    /// <summary>
    /// project id
    /// </summary>
    public int parentId { get; set; }

    public class Lastmodifieduser {
      public string name { get; set; }
      public string displayName { get; set; }
      public bool active { get; set; }
      public Avatarurls avatarUrls { get; set; }
    }

    public class Avatarurls {
      public string _16x16 { get; set; }
    }

    public class Project {
      public int id { get; set; }
      public string key { get; set; }
      public string name { get; set; }
    }

    public class WorkflowMapping {
      /// <summary>
      /// workflow name
      /// </summary>
      public string name { get; set; }
      /// <summary>
      /// workflow name
      /// </summary>
      public string displayName { get; set; }
      /// <summary>
      /// workflow description
      /// </summary>
      public string description { get; set; }
      /// <summary>
      /// issuetype ids
      /// </summary>
      public string[] issueTypes { get; set; }
      public bool jiraDefault { get; set; }
      public bool _default { get; set; }
      public bool system { get; set; }
    }

    public class Issuetype {
      public string iconUrl { get; set; }
      public string name { get; set; }
      public string description { get; set; }
      public string id { get; set; }
      public bool subTask { get; set; }
      public bool defaultIssueType { get; set; }
    }

  }


}
