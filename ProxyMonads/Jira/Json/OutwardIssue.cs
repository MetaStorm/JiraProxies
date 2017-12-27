using CommonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class Outwardissue {
    public string id { get; set; }
    public string key { get; set; }
    public string self { get; set; }
    public Fields fields { get; set; }
    [JsonIgnore]
    public string projectKey {
      get { return key.Split('-')[0]; }
    }

    public bool IsIt( string projectTo, string issueTo) {
      Passager.ThrowIf(() => projectTo.IsNullOrWhiteSpace());
      Passager.ThrowIf(() => issueTo.IsNullOrWhiteSpace());
      return projectKey.ToLower() == projectTo.ToLower() && fields.issuetype.name.ToLower() == issueTo.ToLower();
    }

    public class Fields {
      public string summary { get; set; }
      public IssueClasses.Status status { get; set; }
      public IssueClasses.Priority priority { get; set; }
      public IssueType issuetype { get; set; }
    }
  }


}
