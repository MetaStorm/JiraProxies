using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class AvatarUrls {
    public string __invalid_name__16x16 { get; set; }
    public string __invalid_name__24x24 { get; set; }
    public string __invalid_name__32x32 { get; set; }
    public string __invalid_name__48x48 { get; set; }
  }

  public class Author {
    public string self { get; set; }
    public string name { get; set; }
    public string emailAddress { get; set; }
    public AvatarUrls avatarUrls { get; set; }
    public string displayName { get; set; }
    public bool active { get; set; }
  }

  public class AvatarUrls2 {
    public string __invalid_name__16x16 { get; set; }
    public string __invalid_name__24x24 { get; set; }
    public string __invalid_name__32x32 { get; set; }
    public string __invalid_name__48x48 { get; set; }
  }

  public class UpdateAuthor {
    public string self { get; set; }
    public string name { get; set; }
    public string emailAddress { get; set; }
    public AvatarUrls2 avatarUrls { get; set; }
    public string displayName { get; set; }
    public bool active { get; set; }
  }

  public class Comment {
    public string self { get; set; }
    public string id { get; set; }
    public Author author { get; set; }
    public string body { get; set; }
    public UpdateAuthor updateAuthor { get; set; }
    public string created { get; set; }
    public string updated { get; set; }
  }

  public class D {
    public string __type { get; set; }
    public List<Comment> comments { get; set; }
    public int maxResults { get; set; }
    public int startAt { get; set; }
    public int total { get; set; }
  }
  public class JiraError {
    public D d { get; set; }
  }
  public class Comments {
    public int startAt { get; set; }
    public int maxResults { get; set; }
    public int total { get; set; }
    public List<Comment> comments { get; set; }
  }
}
