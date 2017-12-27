using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.Json {
  public class CreateIssueMeta {
    public string expand { get; set; }
    public Project[] projects { get; set; }

    public class Project {
      public string expand { get; set; }
      public string self { get; set; }
      public string id { get; set; }
      public string key { get; set; }
      public string name { get; set; }
      public Avatarurls avatarUrls { get; set; }
      public Issuetype[] issuetypes { get; set; }
    }

    public class Avatarurls {
      public string _48x48 { get; set; }
      public string _24x24 { get; set; }
      public string _16x16 { get; set; }
      public string _32x32 { get; set; }
    }

    public class Issuetype {
      public string self { get; set; }
      public string id { get; set; }
      public string description { get; set; }
      public string iconUrl { get; set; }
      public string name { get; set; }
      public bool subtask { get; set; }
      public string expand { get; set; }
      public Fields fields { get; set; }
    }

    public class FieldCollection : KeyedCollection<string, FieldDescription> {
      public FieldCollection() : base(StringComparer.OrdinalIgnoreCase) { }
      protected override string GetKeyForItem(FieldDescription item) {
        return item.name;
      }
      public void AddRange(IEnumerable<FieldDescription> fields) {
        fields.ForEach(f => Add(f));
      }
    }

    public class Fields {
      public FieldDescription summary { get; set; }
      public FieldDescription issuetype { get; set; }
      public FieldDescription description { get; set; }
      public FieldDescription project { get; set; }
      public FieldCollection customFields { get; set; } = new FieldCollection();
      public Security security { get; set; }
    }

    public class Security {
      public bool required { get; set; }
      public SecuritySchema schema { get; set; }
      public string name { get; set; }
      public bool hasDefaultValue { get; set; }
      public string[] operations { get; set; }
      public SecurityLevel[] allowedValues { get; set; }
    }


    public class FieldDescription {
      public bool required { get; set; }
      public Schema schema { get; set; }
      public string name { get; set; }
      public bool hasDefaultValue { get; set; }
      public string[] operations { get; set; }
      public JObject[] allowedValues { get; set; }
    }

    public class Schema {
      public string type { get; set; }
      public string system { get; set; }
    }


    public class Allowedvalue {
      public string self { get; set; }
      public string id { get; set; }
      public string key { get; set; }
      public string name { get; set; }
    }

  }
}
