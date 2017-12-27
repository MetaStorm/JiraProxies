using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class AuthSession {
    public string self { get; set; }
    public string name { get; set; }
    public Session session { get; set; }
    public class Session {
      public string name { get; set; }
      public string value { get; set; }
      public bool IsAuthenticated { get { return !string.IsNullOrWhiteSpace(name); } }
    }
    public bool IsAuthenticated { get { return !string.IsNullOrWhiteSpace(name) || (session != null && session.IsAuthenticated); } }
  }
}
