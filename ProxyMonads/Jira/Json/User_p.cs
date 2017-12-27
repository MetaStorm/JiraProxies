using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wcf.ProxyMonads;

namespace Jira.Json {
  partial class User {
    string _expand = "groups";
    public string expand {
      get { return _expand; }
      set { _expand = value; }
    }
    public List<string> applicationKeys = new List<string>();
    public static User FromKey(string key) { return string.IsNullOrWhiteSpace(key) ? null : new User { key = key }; }
    public static User FromUserName(string userName) { return string.IsNullOrWhiteSpace(userName) ? null : new User { name = userName }; }
    public static User FromObject(object userObject) {
      var jUser = (userObject as JObject);
      try {
        if (jUser != null) return jUser.ToObject<User>();
        var json = userObject + "";
        if (!string.IsNullOrWhiteSpace(json))
          Newtonsoft.Json.JsonConvert.DeserializeObject<User>(json);
        throw new Exception("Unknown value type");
      }catch(Exception exc) {
        throw new Exception(new { userObject } + "", exc);
      }
    }
    public static User New(string userName,string displayName,string emailAddress) =>
      new User {
        name = userName,
        emailAddress = emailAddress,
        displayName = displayName,
        applicationKeys = new[] { "jira-core" }.ToList()
      };
    public bool InGroup(string group) {
      return groups.items.Any(g => g.name.ToLower() == group.ToLower());
    }
  }
}