using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {

  public class WebLinkPost {
    public string globalId { get; set; }
    public Application application { get; set; }
    public string relationship { get; set; }
    public Inner @object { get; set; }

    public class Application {
      public string type { get; set; }
      public string name { get; set; }
    }

    public class Icon {
      public string url16x16 { get; set; }
      public string title { get; set; }
    }

    public class Icon2 {
      public string url16x16 { get; set; }
      public string title { get; set; }
      public string link { get; set; }
    }

    public class Status {
      public bool resolved { get; set; }
      public Icon2 icon { get; set; }
    }

    public class Inner {
      public string url { get; set; }
      public string title { get; set; }
      public string summary { get; set; }
      public Icon icon { get; set; }
      public Status status { get; set; }
    }
  }
}
