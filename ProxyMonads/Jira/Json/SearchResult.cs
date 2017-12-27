using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class SearchResult<TIssue> {
    public string expand { get; set; }
    public int startAt { get; set; }
    public int maxResults { get; set; }
    public int total { get; set; }
    public List<TIssue> issues { get; set; }
  }
  public class SearchResult {
    public string expand { get; set; }
    public int startAt { get; set; }
    public int maxResults { get; set; }
    public int total { get; set; }
    public List<IssueClasses.Issue> issues { get; set; }
  }
}
