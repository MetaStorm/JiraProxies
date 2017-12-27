using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {

  public class Group {
    public string name { get; set; }
    public string self { get; set; }
    public static Group FromUserName(string groupName) { return string.IsNullOrWhiteSpace(groupName) ? null : new Group { name = groupName }; }
    public static User FromObject(object groupObject) {
      var jUser = (groupObject as JObject);
      try {
        if (jUser != null) return jUser.ToObject<User>();
        var json = groupObject + "";
        if (!string.IsNullOrWhiteSpace(json))
          Newtonsoft.Json.JsonConvert.DeserializeObject<Group>(json);
        throw new Exception("Unknown value type");
      }
      catch (Exception exc) {
        throw new Exception(new { groupObject } + "", exc);
      }
    }
  }
}
