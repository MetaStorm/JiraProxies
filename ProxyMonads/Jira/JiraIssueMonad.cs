using Jira.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Dynamic;
using System.Threading.Tasks;
using System.Net;

namespace Wcf.ProxyMonads {
  public class JiraConfigAttribute :Foundation.CustomConfig.ConfigFileAttribute { }
  public class JiraSettingsAttribute : Foundation.CustomConfig.ConfigSectionAttribute { }
  public abstract class JiraMonad : RestMonad {
    static string _apiPath = null;

    public static string ApiPath {
      get { return _apiPath; }
      set {
        if (_apiPath != null && _apiPath != value)
          throw new InvalidOperationException(new { ApiPath, value, error = "Is already set" } + "");
        if (string.IsNullOrWhiteSpace(value))
          throw new ArgumentException("ApiPath must not be an empty string or null");
        _apiPath = value;
      }
    }
    #region Config
    class ProxyMonadsConfigAttribute : Foundation.CustomConfig.ConfigFileAttribute { }
    [ProxyMonadsConfig]
    [JiraSettings]
    public class JiraConfig : Foundation.EventLogger<JiraConfig> {
      [Foundation.CustomConfig.ConfigValue]
      public static string JiraPowerUser { get { return KeyValue(); } }
      [Foundation.CustomConfig.ConfigValue]
      public static string JiraPowerPassword { get { return KeyValue(); } }
      [Foundation.CustomConfig.ConfigValue]
      public static string JiraServiceBaseAddress { get { return KeyValue(); } }
      [Foundation.CustomConfig.ConfigValue]
      public static string JiraRestApiPath { get { return KeyValue(); } }
      [Foundation.CustomConfig.ConfigValue]
      public static string JiraMetaServiceName { get { return KeyValue(); } }
      [Foundation.CustomConfig.ConfigValue]
      public static bool TraceRequests { get { return KeyValue<bool>(new[] { false }); } }
      [Foundation.CustomConfig.ConfigValue]
      public static string[] FieldTypesToIgnore { get { return KeyValue()?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0]; } }
      [Foundation.CustomConfig.ConfigValue]
      public static string[] FieldsToIgnore { get { return KeyValue()?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0]; } }
    }
    public static void UnTest() { JiraConfig.UnTest(); }
    public static async Task<ExpandoObject> RunTestFastAsync(ExpandoObject parameters = null, params Func<ExpandoObject,ExpandoObject>[] merge) {
      return await JiraConfig.RunTestAsync(parameters, merge);
    }
    public static async Task<ExpandoObject> RunTestAsync(ExpandoObject parameters = null, params Func<ExpandoObject, ExpandoObject>[] merge) { return await JiraConfig.RunTestAsync(parameters); }
    public static string JiraPowerUser() { return JiraConfig.JiraPowerUser; }
    public static string JiraPowerPassword() { return JiraConfig.JiraPowerPassword; }
    public static string JiraServiceBaseAddress(string path = null) {
      return EnsurePath(path ?? JiraConfig.JiraServiceBaseAddress);
    }
    public static string JiraRestApiPath() {
      return EnsurePath(ApiPath ?? JiraConfig.JiraRestApiPath);
    }

    readonly static string SERVICE_NAME = "JiraService.svc/";
    static string _serviceName = null;
    public static string JiraMetaServiceName() {
      try {
        return _serviceName ?? (_serviceName = JiraConfig.JiraMetaServiceName);
      } catch (System.Security.SecurityException) {
        return _serviceName = SERVICE_NAME;
      } catch (ConfigurationErrorsException) {
        return _serviceName = SERVICE_NAME;
      }
    }
    #endregion
    public static string ParseJiraError(string json) {
      var errorMessages = new List<string>();
      var error = Newtonsoft.Json.JsonConvert.DeserializeObject<Error>(json);
      if (error.errorMessages != null) {
        errorMessages = error.errorMessages;
        errorMessages.AddRange(error.errors.Select(kv => kv.Key + ":" + kv.Value));
      }
      return string.Join("\n", errorMessages.Where(e => !string.IsNullOrWhiteSpace(e)));
    }
  }
  public class JiraMetaTicket<T> : JiraTicket<T> {
    public JiraMetaTicket() : base() { }
    public JiraMetaTicket(T value) : base(value) { }
  }
  public class JiraTicket<T> : JiraRest<T> {
    public JiraTicket() : base() { }
    public JiraTicket(T value) : base(value) { }
    public override V Clone<V>() {
      return base.Clone<V>();
    }
  }
  public class JiraRest {
    public static JiraRest<T> Create<T>(T value) { return new JiraRest<T>(value); }
  }
  public class JiraRest<T> : RestMonad<T> {
    public string Project { get; set; }
    public string IssueType { get; set; }
    public JiraRest() : base() { }
    public JiraRest(T value):base(value) { }
  }
  public class JiraMetaPost<T> : RestMonad<T> {
    public JiraMetaPost() : base() { }
    public JiraMetaPost(T value) : base(value) { }
  }

  public static class Extensions {
    public static V Clone<T, U, V>(U original, V cloner) where V : RestMonad {
      return cloner;
    }
    public static U ToPost<U>(this U post, Uri baseUri = null, string apiAddress = null, string userName = null, string password = null) where U : RestMonad {
      post.BaseAddress = baseUri;
      post.ApiAddress = apiAddress;
      post.UserName = userName;
      post.Password = password;
      return post;
    }
    public static JiraRest<T> ToJiraPost<T>(this T o) { return new JiraRest<T>(o); }
    public static JiraRest<T> ToJiraPost<T>(this T o, string baseUri = null, string apiAddress = null, string userName = null, string password = null) {
      return o.ToJiraPost(string.IsNullOrWhiteSpace(baseUri) ? null : new Uri(RestMonad.EnsurePath(baseUri)), apiAddress, userName, password);
    }
    public static JiraMetaPost<T> ToJiraMetaPost<T>(this T o) {
      return new JiraMetaPost<T>(o);
    }
    public static JiraRest<T> ToJiraPost<T>(this T o, Uri baseUri = null, string apiAddress = null, string userName = null, string password = null) {
      return new JiraRest<T>(o).ToPost(baseUri, apiAddress, userName, password);
    }
    public static JiraTicket<T> ToJiraUser<T>(this T o) { return new JiraTicket<T>(o); }
    public static JiraTicket<T> ToJiraTicket<T>(this T o) { return new JiraTicket<T>(o); }
    public static JiraTicket<T> ToJiraTicket<T>(this T o,RestMonad clone) { return clone.Copy(new JiraTicket<T>(o)); }
    public static JiraTicket<T> ToJiraTicket<T>(this T o, Cookie sessionID) { return new JiraTicket<T>(o) { SessionID = sessionID }; }
    public static JiraTicket<T> ToJiraTicket<T>(this T o, Uri baseUri = null, string apiAddress = null, string userName = null, string password = null) {
      return new JiraTicket<T>(o).ToPost(baseUri, apiAddress, userName, password);
    }
    public static JiraMetaTicket<T> ToJiraMetaTicket<T>(this T o) { return new JiraMetaTicket<T>(o); }
    public static Project ToJiraProject(this object o) { return new Project(); }
    public static IssueType ToJiraIssueType(this object o) { return new IssueType(); }
  }
}
