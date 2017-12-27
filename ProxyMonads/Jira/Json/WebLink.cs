using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {

  public class WebLink {
    public class Application {
    }

    public class Icon {
      public string url16x16 { get; set; }
    }

    public class Icon2 {
    }

    public class Status {
      public Icon2 icon { get; set; }
    }
    public class Inner {
      public string url { get; set; }
      public string title { get; set; }
      public Icon icon { get; set; }
      public Status status { get; set; }
    }
    public int id { get; set; }
    public string self { get; set; }
    public Application application { get; set; }
    public Inner @object { get; set; }
  }
}
