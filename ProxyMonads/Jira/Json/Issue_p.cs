using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions;

namespace Jira.Json {
  public partial class IssueClasses {
    static readonly string _suffixLock = "{{locked}}";
    static readonly string _suffixError = "{{error}}";
    static readonly string _postfixSkip = "{{skip}}";
    static readonly string _postfixDev = "{{dev}}";
    static readonly string _postfixTest = "{{test}}";
    public partial class Issue {
      public const string START_STATE = "{Start}";
      public object Get(string field) {
        switch (field) {
          case "assignee": return fields.assignee.name;
          default:
            var value = fields.GetType().GetAllProperties().Where(p => p.Name == field).Select(p => new Func<object>(() => p.GetValue(fields)));
            return value.DefaultIfEmpty(() => (object)ExtractCustomField<object>(field)).First()();
        }
      }
      public List<Outwardissue> HasLinks(string projectTo, string issueTo) {
        return fields?.issuelinks?
          .SelectMany(li => (new[] { li.inwardIssue, li.outwardIssue }))
          .Where(x => (x?.IsIt(projectTo, issueTo)).GetValueOrDefault())
          .ToList();
      }

      public IList<Jira.Json.IssueTransitions.Transition> GetTransitions(string transitionName) {
        return this.transitions.Where(t => t.name.ToLower() == transitionName.ToLower()).ToArray();
      }
      public Jira.Json.IssueTransitions.Transition GetTransition(string transitionName) {
        var tran = this.transitions.SingleOrDefault(t => t.name.ToLower() == transitionName.ToLower());
        if (tran == null) {
          var trans = transitions.Select(t => t + "").ToArray().ToJson(false);
          throw new Exception(new { ticket = this.key, transitionName, Error = "Transition Not Found", transitions = trans } + "");
        }
        return tran;
      }
      public Jira.Json.IssueTransitions.Transition GetTransition(int transitionId) {
        var tran = this.transitions.SingleOrDefault(t => t.id == transitionId);
        if (tran == null)
          throw new Exception(new { ticket = this.key, transitionId, error = "Transition Not Found", transitions = transitions.ToJson() } + "");
        return tran;
      }
      bool CommentsEndWith(Func<Comment, bool> predicate) {
        return this != null &&
          fields != null &&
          fields.comment != null &&
          fields.comment.comments
          .TakeLast(1)
          .Any(predicate);
      }
      bool SubjectEndsWith(Func<string, bool> predicate) {
        return this != null && fields != null
          ? predicate(fields.summary)
          : false;
      }
      public bool IsLocked {
        get {
          return CommentsEndWith(c => c.body.EndsWith(_suffixLock));
        }
      }
      public bool IsSkip {
        get {
          return SubjectEndsWith(s => (s ?? "").ToLower().EndsWith(_postfixSkip));
        }
      }
      public bool IsDev {
        get {
          return SubjectEndsWith(s => (s ?? "").ToLower().EndsWith(_postfixDev));
        }
      }
      public bool IsTest {
        get {
          return SubjectEndsWith(s => (s ?? "").ToLower().Contains(_postfixTest));
        }
      }
      public bool IsError {
        get {
          return CommentsEndWith(c => c.body.EndsWith(_suffixError));
        }
      }
      public bool IsInYellowStatus {
        get {
          return fields.status?.statusCategory.colorName == "yellow";
        }
      }
      static readonly string _suffix = @"
----
";
      public bool IsSms { get { return CommentsEndWith(c => c.IsSms); } }
      public static string DoTest(string text) { return text + _postfixTest; }
      public static string DoSkip(string text) { return DoSkip(text, true); }
      static string CodeTemplate(object comment, string color = "", string bgColor = "", string title = "", bool isBold = false) {
        var bold = isBold ? "*" : "";
        return bold + "{color:" + color + "}{code:none|bgColor=" + bgColor + "|title=" + title + "}" + comment + "{code}{color}" + bold;
      }
      public static string DoCode(object comment) { return CodeTemplate(comment); }
      //*{color:darkred}{{dimok}}{color}*
      public static string DoCode(object comment, string color) {
        return CodeTemplate(comment, color);
      }
      public static string DoCode(object comment, string color, string backGroundColor) {
        return CodeTemplate(comment, color, backGroundColor, "", true);
      }
      public static string DoSkip(string text, bool doSkip) { return text + DoSkip(doSkip); }
      public static string DoSkip(bool doSkip) { return doSkip ? _postfixSkip : ""; }

      public static string DoDev(string text, bool doDev) { return text + DoDev(doDev); }
      public static string DoDev(bool doDev) { return doDev ? _postfixDev : ""; }

      public static string DoLock(bool doLock) { return doLock ? _suffix + _suffixLock : ""; }
      public static string DoSms(bool doSms) { return doSms ? "\nsms" : ""; }
      public static string LockComment(string comment, bool doLock) { return comment + DoLock(doLock); }
      public static string ErrorComment(string comment) { return ErrorComment(comment, true); }
      public static string ErrorComment(string comment, bool doError) { return (doError ? CodeTemplate(comment, "darkred", "", "Error") : comment); }
      public static string SmsComment(string comment, bool doSms) { return comment + DoSms(doSms); }
      public static string CodeComment(object comment, string color = "", string bgColor = "", string title = "", bool isBold = false) {
        return CodeTemplate(comment, color, bgColor, title, isBold);
      }
      public static string CodeRedComment(object comment) {
        return CodeTemplate(comment, color: "white", bgColor: "maroon", isBold: true);
      }
      public static string CodeGreenComment(object comment) {
        return CodeTemplate(comment, color: "white", bgColor: "darkgreen", isBold: true);
      }

      public T[] ExtractCustomField<T>(string field) {
        return ExtractCustomField<T>(field, true, true);
      }
      public T[] ExtractCustomField<T>(string field, bool throwNotFound, bool throwIfEmpty) {
        var type = typeof(T);
        var isNullable = type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        var custom = fields.custom.Where(kv => kv.Key.ToLower() == field.ToLower());
        if (throwNotFound)
          custom = custom
            .ThrowIfEmpty(new Exception(new { issue = key, field, message = "Not found", fields = fields.custom.ToJson() } + ""));
        if (throwIfEmpty)
          custom = custom
          .ThrowIf(v => v == null || v.IsEmpty(), field + " field is empty.");
        try {
          return custom
            .SelectMany(kv => kv.Value)
            .SelectMany(v => v == null || !v.GetType().IsArray ? new object[] { v } : (object[])v)
            .Select(v => v == null && isNullable
              ? (T)v
              : v != null && isNullable && v.GetType() == Nullable.GetUnderlyingType(typeof(T))
              ? (T)v
              : ChangeType<T>(v))
            .ToArray();
        }
        catch (Exception exc) {
          throw new Exception(new { field } + "", exc);
        }
      }
      public object[] ExtractCustomFieldRaw(string field, bool throwNotFound, bool throwIfEmpty) {
        var custom = fields.custom.Where(kv => kv.Key.ToLower() == field.ToLower());
        if (throwNotFound)
          custom = custom
            .ThrowIfEmpty(new Exception(new { issue = key, field, message = "Not found", fields = fields.custom.ToJson() } + ""));
        if (throwIfEmpty)
          custom = custom
          .ThrowIf(v => v == null || v.IsEmpty(), field + " field is empty.");
        return custom
          .SelectMany(kv => kv.Value)
          .ToArray();
      }
      static T[] ChangeType<T>(object[] values) {
        return values?.Select(v => ChangeType<T>(v)).ToArray() ?? new T[0];
      }
      static T ChangeType<T>(object value) {
        try {
          return (value?.GetType().IsArray).GetValueOrDefault()
            ? (T)((object)((object[])value)?.Select(v => ChangeType<T>(v)).ToArray())
            : (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception exc) {
          throw new Exception(new { value = value.ToJson(false) } + "", exc);
        }
      }
      public override string ToString() {
        return this == null
          ? "{}"
          : fields == null
          ? new { key } + ""
          :
          new {
            key
          ,
            status = fields.status == null ? "" : fields.status.name
          ,
            project = fields.project == null ? "" : fields.project.name
          ,
            type = fields.issuetype == null ? "" : fields.issuetype.name
          ,
            transitions = transitions == null ? "[]" : "[" + string.Join(",", transitions.Select(t => t.ToString()))
          } + "";
      }
    }
    public partial class Fields {
      public Dictionary<string, object[]> custom { get; set; }
    }
  }
}
