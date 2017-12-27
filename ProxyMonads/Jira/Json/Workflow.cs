using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class Workflow {
    public string name { get; set; }
    public string description { get; set; }
    public string lastModifiedDate { get; set; }
    public string lastModifiedUser { get; set; }
    public int steps { get; set; }

    public override string ToString() {
      return new { name } + "";
    }
    public class Transition {
      public class Property {
        public string key { get; set; }
        public string value { get; set; }
        public string id { get; set; }
        public override string ToString() {
          return new { key, value } + "";
        }
      }
    }
  }
}

