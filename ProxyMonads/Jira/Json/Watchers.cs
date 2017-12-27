using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class Watchers {
    public string self { get; set; }
    public bool isWatching { get; set; }
    public int watchCount { get; set; }
    public List<User> watchers { get; set; }

  }
}
