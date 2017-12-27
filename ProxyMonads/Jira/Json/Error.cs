using System.Collections.Generic;

namespace Jira.Json {
  public class Errors :Dictionary<string,string>{
  }

  public class Error {
    public List<string> errorMessages { get; set; }
    public Errors errors { get; set; }
  }
}
