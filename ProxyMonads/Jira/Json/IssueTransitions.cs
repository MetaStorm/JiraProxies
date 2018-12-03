using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using CommonExtensions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Jira.Json {
  public class IssueTransitions {
    public string expand { get; set; }
    public List<Transition> transitions { get; set; }

    public class To {
      public string self { get; set; }
      public string description { get; set; }
      public string iconUrl { get; set; }
      public string name { get; set; }
      public string id { get; set; }
      public StatusCategory statusCategory { get; set; }
    }

    public class Transition {
      public int id { get; set; }
      public string name { get; set; }
      public To to { get; set; }
      public bool HasId { get { return id > 0; } }
      public int SafeId() { return id.ThrowIf(() => id == 0); }
      public string SafeName() { return name.ThrowIf(() => string.IsNullOrWhiteSpace(name)); }
      public override string ToString() {
        Func<string> propsToString = () => {
          try {
            if(PropertiesImpl.error != null) return "[error]";
            return "[" + Properties.Select(p => new { p.key, p.value } + "").Flatten() + "]";
          }
          catch(Exception exc) {
            Debug.WriteLine(exc);
            return "[error]";
          }
        };
        return this == null ? "<null>" : new { id, name, to = to == null ? "{}" : to.name, properties = propsToString() } + "";
      }
      public object ToLog() {
        return this == null ? new { } : (object)new { id, name, to = to == null ? "{}" : to.name, properties = "[" + string.Join(",", Properties.Select(p => p.ToString())) + "]" };
      }
      public string ToLog(string ticket) {
        return new { ticket, transition = string.IsNullOrWhiteSpace(name) ? new { id } : (object)new { name } } + "";
      }
      //public Workflow.Transition.Property[] Properties { get; set; } = new Workflow.Transition.Property[0];
      public Workflow.Transition.Property[] Properties { get { return PropertiesImpl.value ?? new Workflow.Transition.Property[0]; } }
      public (Workflow.Transition.Property[] value,Exception error) PropertiesImpl { get { return AsyncHelpers.RunSync(() => PropertiesGetter.Value); } }
      [JsonIgnore]
      public Lazy<Task<(Workflow.Transition.Property[] value,Exception error)>> PropertiesGetter { get; set; } 
        = new Lazy<Task<(Workflow.Transition.Property[] value, Exception error)>>(() => Task.FromResult((new Workflow.Transition.Property[0],(Exception)null)));

      public static Transition Create(int id) {
        return new Transition { id = id };
      }
      public static Transition Create(string name) {
        return new Transition { name = name };
      }
      public static Transition Create(int id, string name) {
        return new Transition { id = id, name = name };
      }

    }
  }
}
