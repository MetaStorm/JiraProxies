using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Wcf.ProxyMonads;
using Jira.Json;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using CommonExtensions;
using System.Dynamic;
using System.Runtime.ExceptionServices;
using System.Net;
using static Wcf.ProxyMonads.RestExtenssions;
using static Jira.Json.IssueClasses;
using WLS = System.Collections.Generic.List<(System.DateTime startDate, System.TimeSpan duration, Jira.Rest.TicketTransitionHistory history, Jira.Json.IssueClasses.Assignee assignee)>;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using static Jira.Json.Role;
using static Wcf.ProxyMonads.JiraMonad;

namespace Jira {
  public static partial class Rest {

    static Rest() {
      if(RestMonad.UseSessionID) {
        var tcs = new TaskCompletionSource<string>();
        var user = JiraMonad.JiraPowerUser();
        JiraMonad.SessionIDs[user] = tcs.Task;
        RestConfiger.OnInit = async () => {
          JiraMonad.JiraConfig.LogEvent(new { GetSessionID = user }, System.Diagnostics.EventLogEntryType.Warning);
          tcs.SetResult(await GetSessionID(user, JiraMonad.JiraPowerPassword()));
          RestMonad.SetDefaultSessionID = SetDefaultSessionID;
        };
      }
    }

    public static async Task SetDefaultSessionID() {
      var user = JiraMonad.JiraPowerUser();
      JiraMonad.JiraConfig.LogEvent(new { GetSessionID = user }, System.Diagnostics.EventLogEntryType.Warning);
      var tcs = new TaskCompletionSource<string>();
      JiraMonad.SessionIDs[user] = tcs.Task;
      tcs.SetResult(await GetSessionID(user, JiraMonad.JiraPowerPassword()));
    }

    public static async Task<string> GetSessionID(string user, string password) {
      return (await PostAuthSession(user, password))
        .Counter(1, i => {
          throw new Exception("No session was availible for " + new { user });
        }, null)
        .Single();
      ;
    }

    static async Task<string[]> PostAuthSession(string user, string password) {
      var session = await RestMonad.Empty().PostAuthSession(user, password);
      return ParseJiraSession(session);
    }

    private static string[] ParseJiraSession(RestMonad<AuthSession> session) {
      return new[] { session.Value }
      .Where(s => s.IsAuthenticated)
      .Select(s => s.session.value)
      .ToArray();
    }

    private const string authSessionPath = "rest/auth/1/session";
    private const string EXEC_COUNT_FIELD = "ExecCount";
    private const string ERROR_COUNT_FIELD = "ErrorCount";
    #region Self Test
    class SelfTest :Foundation.Testable<SelfTest> {
      protected override async Task<ExpandoObject> _RunTestAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
        return await TestHostAsync(null, async (p, m) => {
          var b = await base._RunTestAsync(p, m);
          var jm = await JiraMonad.RunTestAsync(null);
          return (GetType() + "")
            .ToExpando(await RestMonad.Empty().GetMySelfAsync())
            .Merge(jm)
            .Merge(b);
        }, null, merge);
      }
    }
    public static async Task<ExpandoObject> RunTestAsync() {
      return await SelfTest.RunTestAsync(null);
    }
    #endregion

    public class CustomProperties :Dictionary<string, object> { }
    public enum IssueFielder { comment, components, issuelinks, status, issuetype }
    public enum IssueExpander { changelog, transitions, renderedFields, names }
    public enum SmsValue { Next, Error, Back }

    #region Path Factory Base Methods
    static string ApiPath { get { return JiraMonad.JiraRestApiPath(); } }
    static string IssuePath(string apiPath = null) { return (apiPath ?? ApiPath) + "issue"; }
    static string IssuePath() { return IssuePath(null); }
    #endregion

    #region Path Factory - Old style
    static string AddSlash(string path) {
      if(!Regex.IsMatch(path, "/$")) return path + "/";
      return path;
    }
    static string IssueTicketPath(string ticket, string apiPath = null) { return AddSlash(apiPath ?? (ApiPath + "issue/")) + ticket; }
    static string IssueTicketPath(string ticket) { return IssueTicketPath(ticket, null); }

    static string IssueCommentPath(string ticket, string apiPath) { return IssueTicketPath(ticket, apiPath) + "/comment?_=" + DateTime.Now.Ticks; }
    static string IssueCommentPath(string ticket) { return IssueCommentPath(ticket, null); }

    static string IssueAttachmentPath(string ticket, string apiPath) { return IssueTicketPath(ticket, apiPath) + "/attachments"; }
    static string IssueAttachmentPath(string ticket) { return IssueAttachmentPath(ticket, null); }

    static string IssueStatusPath(string ticket, string apiPath) { return IssueTicketPath(ticket, apiPath) + "?fields=status"; }
    static string IssueStatusPath(string ticket) { return IssueStatusPath(ticket, null); }

    static string IssueTransitionsPath(string ticket, string apiPath) { return IssueTicketPath(ticket, apiPath) + "/transitions"; }
    static string IssueTransitionsPath(string ticket) { return IssueTransitionsPath(ticket, null); }

    static string IssueWebLinkPath(string ticket, string apiPath) { return IssueTicketPath(ticket, apiPath) + "/remotelink"; }
    static string IssueWebLinkPath(string ticket) { return IssueWebLinkPath(ticket, null); }

    static string ApiPathOrDefault(string apiPath) { return apiPath ?? ApiPath; }
    static Func<string, string> UnrelatedPath(string operation, params Tuple<string, string>[] urlParams) {
      string urlQuery = (urlParams.Any() ? "?" : "") + string.Join("&", urlParams.Where(up => up.Item2 != null).Select(up => up.Item1 + "=" + Uri.EscapeUriString(up.Item2)));
      return (apiPath) => ApiPathOrDefault(apiPath) + operation + urlQuery;
    }
    static string GroupPath(string groupName) { return UnrelatedPath("group", Tuple.Create("groupname", groupName))(ApiPath); }
    static string GroupAddUserPath(string groupName) { return UnrelatedPath("group/user", Tuple.Create("groupname", groupName))(ApiPath); }
    static Func<string, string> UserPath(params Tuple<string, string>[] urlParams) { return UnrelatedPath("user", urlParams); }
    static string UserPath() { return UnrelatedPath("user")(ApiPath); }
    static Func<string, string> FieldPath(params Tuple<string, string>[] urlParams) { return UnrelatedPath("field", urlParams); }
    static Func<string, string> MySelfPath(params Tuple<string, string>[] urlParams) { return UnrelatedPath("myself", urlParams); }
    static Func<string, string> SearchPath(params Tuple<string, string>[] urlParams) { return UnrelatedPath("search", urlParams); }
    static Func<string, string> IssueLinkPath() { return UnrelatedPath("issueLink"); }
    static Func<string, string> WorkflowPath(string name) {
      return UnrelatedPath("workflow" + (name.IsNullOrWhiteSpace() ? "" : $"?workflowName={WebUtility.UrlEncode(name)}"));
    }
    static Func<string, string> WorkflowSchemaWorkflowsPath(int id) { return UnrelatedPath($"workflowscheme/{id}/workflow"); }
    static Func<string, string> WorkflowTransitionPropertiesPath(string workflowName, int transitionId) {
      return UnrelatedPath("workflow/transitions/" + transitionId + "/properties", Tuple.Create("workflowName", workflowName));
    }
    static Func<string, string> StatusesPath() { return UnrelatedPath("status"); }
    static Func<string, string> IssueCreateMetaPath() { return UnrelatedPath("issue/createmeta?expand=projects.issuetypes.fields"); }
    static Func<string, string> IssueCreateMetaWithFieldsPath(string project, string issueType) {
      Passager.ThrowIf(() => project.IsNullOrWhiteSpace());
      Passager.ThrowIf(() => issueType.IsNullOrWhiteSpace());
      return UnrelatedPath($"issue/createmeta", Tuple.Create("projectKeys", project.ToUpper()), Tuple.Create("issuetypeNames", issueType), Tuple.Create("expand", "projects.issuetypes.fields"));
    }
    static string CombinePath(params string[] pathes) {
      return string.Join("/",
        pathes
          .SkipLast(1)
          .Select(path => path.TrimEnd('/'))
          .Concat(pathes.TakeLast(1)));
    }
    public static Func<string, string> ProjectsPath() { return UnrelatedPath("project"); }
    static Func<string, string> ProjectPath(string projectIdOrKey) {
      Passager.ThrowIf(() => projectIdOrKey.IsNullOrWhiteSpace());
      return UnrelatedPath($"project/{projectIdOrKey}");
    }
    static Func<string, string> ProjectRolesPath(string project) { return UnrelatedPath("project/" + project + "/role"); }
    static Func<string, string> ProjectRolePath(string project, int roleId) { return UnrelatedPath("project/" + project + "/role/" + roleId); }
    static Func<string, string> ProjectIssueStatusesPath(string project) { return UnrelatedPath("project/" + project + "/statuses"); }
    static Func<string, string> ProjectComponentsPath(string project) { return UnrelatedPath("project/" + project.ToUpper() + "/components"); }
    static Func<string, string> UsersPath(string user, int maxResults) { return UnrelatedPath($"user/search?startAt=0&maxResults={maxResults}&includeInactive=false&username={user.IfEmpty(RestConfiger.UserWildcard)}"); }
    static Func<string, string> UserWithPermissionPath(string ticket, string user, string permission) {
      Passager.ThrowIf(() => permission.IsEmpty());
      return UnrelatedPath($"user/permission/search?username={user}&permissions={permission}&issueKey={ticket.ToUpper()}&startAt=0&maxResults=10000");
    }
    static Func<string, string> RolesPath => UnrelatedPath("role");

    //static Func<string, string> IssueTypePath() { return UnrelatedPath("issuetype"); }
    static string IssueTypePath(int id) { return IssueTypePath(null) + "/" + id; }
    static string IssueTypePath() { return IssueTypePath(null); }
    static string IssueTypePath(string apiPath = null) { return (apiPath ?? ApiPath) + "issuetype"; }
    static string RestApiPath(string segment) { return ApiPath + segment; }
    public static string ProjectPath() { return RestApiPath("project"); }
    public static string ProjectRoleByIdPath(string projectKey, int roleId) { return $"{RestApiPath("project")}/{projectKey}/role/{roleId}"; }


    //static string IssueTypePath() { return ApiPath + "issuetype"; }
    static Func<string, string> SecurityLevelPath(int securityLevelID) { return UnrelatedPath("securitylevel/" + securityLevelID); }
    static Func<string, string> IssueSecurityLevelSchemePath(string projectKey) { return UnrelatedPath("project/" + projectKey + "/issuesecuritylevelscheme"); }
    #endregion

    #region New way to provide Path Factory
    static string IssueRelatedPath(string ticket, string apiPath, string operation) { return IssueTicketPath(ticket, apiPath) + "/" + operation; }
    static Func<string, string, string> IssueRelatedPath(string operation) { return (ticket, apiAddress) => IssueRelatedPath(ticket, apiAddress, operation); }

    static Func<string, string, string> IssueWatchersPath() { return IssueRelatedPath("watchers"); }
    static Func<string, string, string> IssueWorklogPath() { return IssueRelatedPath("worklog"); }
    static Func<string, string, string> IssueEditMetaPath() { return IssueRelatedPath("editmeta"); }
    #endregion

    #region ProjectConfig path
    static string PathCaller([CallerMemberName] string memberName = "") {
      return memberName;
    }
    static string ProjectConfigPath { get { return "rest/projectconfig/latest"; } }
    static string WorkflowScheme(string projectKey) => ProjectConfigPath + $"/{PathCaller().ToLower()}/{projectKey}";
    public static string DraftWorkflowScheme(string projectKey) => ProjectConfigPath + $"/{PathCaller().ToLower()}/{projectKey}";
    #endregion


    #region Base method to GET/POST Issue
    #region GET

    #endregion
    #region POST
    /// <summary>
    /// Post a new JIRA Ticket
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="post"></param>
    /// <param name="customProperties"></param>
    /// <param name="pathFactory"></param>
    /// <param name="jsonSettings"></param>
    /// <param name="doPut"></param>
    /// <returns></returns>
    async public static Task<RestMonad<HttpResponseMessage>> PostAsync<T>(this RestMonad<T> post, Field<object>[] customProperties, Func<string> pathFactory, JsonSerializerSettings jsonSettings = null, bool doPut = false) {
      var settings = jsonSettings ?? RestMonad.JsonSerializerSettingsFactory();
      dynamic jPost = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(post.Value, settings));
      customProperties.ToList().ForEach(cp => {
        var jiraValue = cp.GetJiraValue();
        if(jiraValue != null)
          jPost.fields[cp.field.id] = JToken.FromObject(jiraValue);
      });
      return await post.PostAsync(pathFactory, (object)jPost, settings, doPut);
    }

    async public static Task<RestMonad<HttpResponseMessage>> PostAsync<T>(this RestMonad<T> post, IDictionary<string, object> customProperties, Func<string> pathFactory, JsonSerializerSettings jsonSettings = null) {
      return await post.PostAsync((await post.ResolveCustomFields(customProperties)).Value, pathFactory, jsonSettings);
    }

    #endregion
    #endregion

    public static async Task ProjectIssueTypeSchemeSetAsync(int projectId, int issueTypeSchemeId) {
      var values = new Dictionary<string, string> {
        { "createType", "chooseScheme" },
        { "projectId", projectId + "" },
        { "fieldId", "" },
        { "schemeId", issueTypeSchemeId+"" },
        { "sameAsProjectId", "" },
        { "OK ", "OK" },
      };
      var requestUri = "/secure/admin/SelectIssueTypeSchemeForProject.jspa";
      await Core.PostFormAsync(requestUri, values);
    }
    public static async Task<string> DeleteIssueTypeSchemeAsync(int issueTypeSchemeId) {
      if(issueTypeSchemeId == 10000)
        throw new Exception(new { defaultIssueTypeScheme = new { issueTypeSchemeId } } + "");
      var values = new Dictionary<string, string> {
        { "Delete", "Delete" },
        { "schemeId", issueTypeSchemeId+"" },
      };
      var requestUri = "/secure/admin/DeleteOptionScheme.jspa";
      await Core.PostFormAsync(requestUri, values);
      return new { issueTypeSchemeId } + "";
    }
    public static async Task<string> DeleteIssueTypeScreenSchemeAsync(int issueTypeScreenSchemeId) {
      if(issueTypeScreenSchemeId > 0) {
        var values = new Dictionary<string, string> {
        { "Delete", "Delete" },
        { "confirm", "true" },
        { "id", issueTypeScreenSchemeId+"" },
      };
        var requestUri = "/secure/admin/DeleteIssueTypeScreenScheme.jspa";
        await Core.PostFormAsync(requestUri, values);
      }
      return new { issueTypeScreenSchemeId } + "";
    }
    public static async Task<string> DeleteScreenScheme(int screenSchemeId) {
      var values = new Dictionary<string, string> {
        { "Delete", "Delete" },
        { "confirm", "true" },
        { "id", screenSchemeId+"" },
      };
      var requestUri = "/secure/admin/DeleteFieldScreenScheme.jspa";
      await Core.PostFormAsync(requestUri, values);
      return new { screenSchemeId } + "";
    }
    public static async Task<string> DeleteScreen(int screenId) {
      var values = new Dictionary<string, string> {
        { "Delete", "Delete" },
        { "confirm", "true" },
        { "id", screenId+"" },
      };
      var requestUri = "/secure/admin/DeleteFieldScreen.jspa";
      await Core.PostFormAsync(requestUri, values);
      return new { screenId, was = "deleted" } + "";
    }
    public static Task ScreenDeleteByProject(string projectKey, bool throwOnEmpty = true) =>
      from rm in RestMonad.Empty().GetScreensByProjectPrefix(projectKey, "delete", throwOnEmpty)
      from t in (from sid in rm.Value from t1 in DeleteScreen(sid) select t1)
      select t;

    public static async Task<string> DeleteWorkflowSchemeAsync(int workflowSchemeId) {
      var values = new Dictionary<string, string> {
        { "inline", "true" },
        { "decorator", "dialog" },
        { "confirmed", "true" },
        { "schemeId", workflowSchemeId+"" },
      };
      var requestUri = "/secure/admin/DeleteWorkflowScheme.jspa";
      await Core.PostFormAsync(requestUri, values);
      return new { workflowSchemeId } + "";
    }
    public static async Task<string> DeleteStatusAsync(int statusId) {
      var values = new Dictionary<string, string> {
        { "inline", "true" },
        { "decorator", "dialog" },
        { "id", statusId+"" },
        { "confirm", "true" }
      };
      var requestUri = "secure/admin/DeleteStatus.jspa";
      await Core.PostFormAsync(requestUri, values);
      return new { statusId } + "";
    }
    public static async Task<string> PostStatusAsync(string name, string description, int category = 2) {
      var values = new Dictionary<string, string> {
        { "name", name },
        { "description", description },
        { "statusCategory", category+"" },
        { "inline", "true" },
        { "decorator", "dialog" }
      };
      var requestUri = "secure/admin/AddStatus.jspa";
      await Core.PostFormAsync(requestUri, values);
      return new { name, description, category } + "";
    }
    public static async Task<string> GetWorkflowXmlAsync(string name) {
      var requestUri = "secure/admin/workflows/ViewWorkflowXml.jspa?workflowMode=live&workflowName=" + System.Net.WebUtility.UrlEncode(name);
      var xml = await new RestMonad(new Uri(JiraServiceBaseAddress())).GetStringAsync(requestUri);
      return xml.Value;
    }
    public static async Task<string> PostWorkflowAsync(string name, string description, string workflowXML) {
      var values = new Dictionary<string, string> {
        { "name", name },
        { "description", description },
        { "workflowXML", workflowXML },
        { "inline", "true" },
        { "decorator", "dialog" }
      };
      var requestUri = "/secure/admin/workflows/ImportWorkflowFromXml.jspa";
      await Core.PostFormAsync2(requestUri, values);
      return new { name, description, workflowXML } + "";
    }
    public static async Task<string> DeleteWorkflowAsync(string name) {
      var values = new Dictionary<string, string> {
        { "inline", "true" },
        { "decorator", "dialog" },
        { "workflowName", name },
        {"workflowMode","live" },
        { "confirmedDelete", "true" }
      };
      var requestUri = "/secure/admin/workflows/DeleteWorkflow.jspa";
      await Core.PostFormAsync(requestUri, values);
      return new { name } + "";
    }

    public static async Task<RestMonad<IssueClasses.Issue>> GetIssueWithHistoryAsync(this JiraTicket<string> ticket) {
      return await ticket.GetIssueAsync(new[] { IssueExpander.changelog });
    }
    public struct TicketTransitionHistory {
      public int Id { get; set; }
      public string TicketKey { get; set; }
      public string FromState { get; set; }
      public string ToState { get; set; }
      public DateTimeOffset Date { get; set; }
      public Author Author { get; set; }
    }
    public static async Task<IList<TicketTransitionHistory>> GetIssueTransitionHistoryAsync(this JiraTicket<string> ticket) {
      return (await ticket.GetIssueAsync(new[] { IssueExpander.changelog })).Value.TransitionHistory();
    }
    public static IList<TicketTransitionHistory> TransitionHistory(this IssueClasses.Issue issue) {
      Passager.ThrowIf(() => issue == null);
      Passager.ThrowIf(() => issue.changelog == null);
      var history = (from h in issue.changelog.histories
                     from i in h.items
                     where i.field == "status"
                     select new TicketTransitionHistory() {
                       TicketKey = issue.key,
                       Date = h.created,
                       FromState = i.fromString,
                       ToState = i.toString,
                       Author = h.author,
                       Id = h.id.IsNullOrWhiteSpace() ? 0 : int.Parse(h.id)
                     }
              ).ToList();
      var created = issue.fields.created;
      var createStatus = history.Select(h => h.FromState).DefaultIfEmpty(issue.fields.status.name).First();
      var history0 = new TicketTransitionHistory { TicketKey = issue.key, Date = created, FromState = IssueClasses.Issue.START_STATE, ToState = createStatus };
      history.Insert(0, history0);
      return history;
    }

    public static async Task<RestMonad<Watchers>> GetIssueWatchersAsync(this JiraTicket<string> ticket) {
      return await (ticket.GetIssueRelatedAsync<Watchers>(IssueWatchersPath()));
    }
    public static async Task<IssueClasses.Issue> GetIssueStatusAsync(this JiraTicket<string> ticket) {
      return await
        (await ticket.GetIssueAsync(t => IssueStatusPath(t, ticket.ApiAddress)))
        .HandleExecutedAsync((response, json) => JsonConvert.DeserializeObject<IssueClasses.Issue>(json), null, null);
    }

    public static IssueClasses.Comments GetComments(string ticket) { return Task.Run(() => Jira.Rest.GetCommentsAsync(ticket)).Result; }
    public static async Task<IssueClasses.Comments> GetCommentsAsync(string ticket) {
      return await
        (await ticket.ToJiraTicket().GetIssueAsync(IssueCommentPath))
        .HandleExecutedAsync((response, json) => JsonConvert.DeserializeObject<IssueClasses.Comments>(json), null, null);
    }

    #region Issue-Related
    /// <summary>
    /// Creates new ticket.
    /// Project ID can be either number(project id) or string(project key)
    /// </summary>
    /// <param name="newIssuePost"></param>
    /// <param name="customProperties"></param>
    /// <returns></returns>
    public static async Task<RestMonad<IssueClasses.Issue>> PostIssueAsync(this RestMonad<JiraNewIssue> newIssuePost, IDictionary<string, object> customProperties) {
      var cpss = LazyMonad.Create(async () => await newIssuePost.ResolveCustomFieldsValues(customProperties));
      var js = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
      return await (
        from rm in newIssuePost.ResolveProjectAndIssueType()
        from custProps in cpss.Value
        from sl in newIssuePost.ResolveSecurityLevel()
        let pathFactory = (Func<string>)(() => IssuePath(newIssuePost.ApiAddress))
        from rm1 in rm.PostAsync(custProps.ToArray(), pathFactory)
        from rm2 in rm1.HandleExecutedAsync((response, json) =>
          response.Clone<RestMonad<IssueClasses.Issue>, IssueClasses.Issue>(JsonConvert.DeserializeObject<IssueClasses.Issue>(json)),
          (hrm, x) => {
            var jJson = JObject.FromObject(new { });
            jJson.Add("issue", JToken.FromObject(rm.Value, js));
            var fields = custProps.Select(cf => new { cf.field.name, cf.field.id, value = cf.GetRawValue() }).ToArray();
            jJson.Add("customFields", JToken.FromObject(fields, js));
            throw new HttpResponseMessageException(hrm.Value.Response, hrm.Value.Json, hrm.Value.Address, hrm.Value.Message + "\n" + jJson, hrm.Value.InnerException);
          },
          null)
        select rm2);
    }
    //public static async Task<int> ExecCountGoNext(this JiraTicket<string> jiraTicket, string workflow,string comment = "", int goNextValue = 0, string field = EXEC_COUNT_FIELD) {
    //  var issue = (await jiraTicket.GetIssueAsync(workflow)).Value;
    //  var execCount = issue.ExtractCustomField<int>(field).Single();
    //  if (execCount == goNextValue) {
    //    var counterTran = issue.TransitionByProperty("counter", "0", true);
    //    var e = field.ToExpando(execCount).Merge(new { next = counterTran.to.name });
    //    await jiraTicket.PostIssueTransitionAsync(counterTran, IssueClasses.Issue.DoCode(e.Stringify()) + comment, null);
    //  }
    //  return execCount;
    //}
    /// <summary>
    /// Transition issue to transition with property <paramref name="transitionProperty"/>
    /// </summary>
    /// <param name="jiraTicket"></param>
    /// <param name="comment"></param>
    /// <param name="goNextCondition">condition(current,transParamValue)</param>
    /// <param name="field">Counter field name</param>
    /// <param name="transitionProperty">with value used in <paramref name="goNextCondition"/></param>
    /// <returns></returns>
    static async Task<RestMonad<IssueTransitions.Transition>[]> CounterGoNext(this JiraTicket<string> jiraTicket, Func<int, int, bool> goNextCondition, string comment = "", string field = ERROR_COUNT_FIELD, string transitionProperty = "counter-up") {
      var issue = (await jiraTicket.GetIssueAsync()).Value;
      var execCount = issue.ExtractCustomField<int>(field).Single();
      return (await (await issue.FindTransitionByProperty(transitionProperty))
        .Select(t => new { t = t.Item1, p = t.Item2 })
        .Select(async x => {
          var goNextValue = int.Parse(x.p.value);
          if(goNextCondition(execCount, goNextValue)) {
            return new[] { await jiraTicket.PostIssueTransitionAsync(x.t, IssueClasses.Issue.DoCode(field.ToExpando(execCount).Merge(new { next = x.t.to.name }).Stringify()) + comment, null) };
          }
          return new RestMonad<IssueTransitions.Transition>[0];
        })
        .WhenAll())
        .DefaultIfEmpty(new RestMonad<IssueTransitions.Transition>[0])
        .SelectMany(rm => rm)
        .ToArray();
    }

    static async Task<RestMonad<IssueTransitions.Transition>[]> CountUpGoNext(this JiraTicket<string> jiraTicket, string comment, string field, string transitionProperty) {
      return await jiraTicket.CounterGoNext((current, transPropValue) => current >= transPropValue, comment, field, transitionProperty);
    }
    static async Task<RestMonad<IssueTransitions.Transition>[]> CountDownGoNext(this JiraTicket<string> jiraTicket, string comment, string field, string transitionProperty) {
      return await jiraTicket.CounterGoNext((current, transPropValue) => current <= transPropValue, comment, field, transitionProperty);
    }

    public static async Task<RestMonad<IssueTransitions.Transition>[]> ExecCountStep(this JiraTicket<string> jiraTicket, string counterName, string comment) {
      var transPropName = "counter" + (!string.IsNullOrWhiteSpace(counterName) ? "-" + counterName : "");
      var issue = (await jiraTicket.GetIssueAsync()).Value;
      var transPropValue = (await issue.FindTransitionProperty(transPropName))
        .Counter(1, new Exception(new { ticket = jiraTicket, transPropName, message = "Not found" } + ""), null)
        .Single();
      var transPropValueInt = 0;
      if(!int.TryParse(transPropValue.value, out transPropValueInt))
        throw new Exception(new { ticket = jiraTicket, transPropName, transPropValue, message = "Is not a integer" } + "");
      if(transPropValueInt < 0)
        throw new Exception(new { ticket = jiraTicket, transPropName, transPropValue, message = "Is less then Zero" } + "");
      var step = transPropValueInt > 0 ? 1 : -1;
      var execCount = issue.ExtractCustomField<int>(EXEC_COUNT_FIELD)[0] + step;
      if(execCount < 0)
        throw new Exception(new { ticket = jiraTicket.Value, field = new { name = EXEC_COUNT_FIELD, value = execCount }, transPropName, transPropValue, message = "Counter < 0" } + "");
      await jiraTicket.PutIssueAsync(MakeUpdateComment(IssueClasses.Issue.DoCode(EXEC_COUNT_FIELD.ToExpando(execCount).Merge(new { transPropValueInt }).Stringify()) + comment), new object[] { EXEC_COUNT_FIELD, execCount });
      var rm = step == 1
        ? await jiraTicket.CountUpGoNext(comment, EXEC_COUNT_FIELD, transPropName)
        : await jiraTicket.CountDownGoNext(comment, EXEC_COUNT_FIELD, transPropName);
      return rm;
    }
    public static async Task<RestMonad> PutIssueFieldsAsync(this JiraTicket<string> restMonad, params object[] customFieldNameValuePairs) {
      return await restMonad.PutIssueAsync(null, customFieldNameValuePairs);
    }
    public static Task<RestMonad> PutIssueAsync(this JiraTicket<string> restMonad, object[] customFieldNameValuePairs) {
      return restMonad.PutIssueAsync(null, customFieldNameValuePairs);
    }
    public static Task<RestMonad> PutIssueAsync(this JiraTicket<string> restMonad, object update, object[] customFieldNameValuePairs) {
      Passager.ThrowIf(() => customFieldNameValuePairs.Length % 2 != 0);
      var fieldDict = customFieldNameValuePairs
        .Buffer(2)
        //.Zip(customFieldNameValuePairs.Skip(1), (n, body) => new { n, v = new { body }.ToJson() })
        .Select(b => new { n = b[0], v = b[1] })
        .ToDictionary(x => x.n + "", x => x.v);
      return restMonad.PutIssueAsync(update, fieldDict);
    }
    public static Task<RestMonad> PutIssueAsync(this JiraTicket<string> restMonad, IDictionary<string, object> customFields) {
      return restMonad.PutIssueAsync(null, customFields);
    }
    public static async Task<RestMonad> PutIssueAsync(this JiraTicket<string> restMonad, object update, IDictionary<string, object> customFields) {
      var issue = (await restMonad.GetIssueAsync()).Value;
      var newIssue = JiraNewIssue.Create(issue.fields.project.id, issue.fields.issuetype.name, "").ToRestMonad();
      newIssue.Value.key = restMonad.Value;
      var resolvedFields = await newIssue.ResolveCustomFieldsValues(customFields);
      return await restMonad.PutIssueAsync(update, resolvedFields);
    }
    static async Task<RestMonad> PutIssueAsync(this JiraTicket<string> restMonad, object update, IEnumerable<Field<object>> customFields) {
      return await (
       from rm1 in restMonad.Switch(new { update = update ?? new { }, fields = new { } }).PostAsync(customFields.ToArray(), () => IssueTicketPath(restMonad.Value), null, true)
       from rm2 in rm1.HandleExecutedAsync(
         (response, json) => rm1,
         (exc, t) => {
           var fields = customFields.Select(cf => new { cf.field.name, cf.field.id, value = cf.GetRawValue() }).ToJson();
           throw new Exception(new { ticket = restMonad.Value, fields } + "", exc.Value);
         }, null)
       select rm2
       );
    }
    public static async Task<RestMonad> PutIssueAsync(this JiraTicket<string> restMonad, object update, object fieldsToUpdate, IEnumerable<Field<object>> customFields) {
      return await (
       from rm1 in restMonad.Switch(new { update = update ?? new { }, fields = fieldsToUpdate ?? new { } }).PostAsync(customFields.ToArray(), () => IssueTicketPath(restMonad.Value), null, true)
       from rm2 in rm1.HandleExecutedAsync(
         (response, json) => rm1,
         (exc, t) => {
           var fields = (customFields ?? new Field<object>[0]).Select(cf => new { cf.field.name, cf.field.id, value = cf.GetRawValue() }).ToJson();
           throw new Exception(new { ticket = restMonad.Value, fields } + "", exc.Value);
         }, null)
       select rm2
       );
    }
    public static Task<RestMonad> PutIssueByFieldsAsync(this JiraTicket<string> jiraTicket, object fieldsToUpdate) {
      return jiraTicket.PutIssueAsync(null, fieldsToUpdate, new Field<object>[0]);
    }
    public static Task<RestMonad> PutIssueSecurityAsync(this JiraTicket<string> jiraTicket, string name) {
      return jiraTicket.PutIssueAsync(null, MakeUpdateSecurity(name), new Field<object>[0]);
    }
    public static Task<RestMonad> PutIssueAssigneeAsync(this JiraTicket<string> jiraTicket, string assignee) {
      return jiraTicket.PutIssueAsync(null, MakeUpdateAssignee(assignee), new Field<object>[0]);
    }
    public static async Task<RestMonad<HistoryItem[]>> RollbackAssignee(this JiraTicket<string> jiraTicket) {
      var issue = (await jiraTicket.GetIssueAsync(new[] { IssueExpander.changelog })).Value;
      var lastAssignee = (from h in issue.changelog.histories
                          from i in h.items
                          where i.field == "assignee"
                          select i
                          )
                          .TakeLast(1)
                          .Counter(1, new Exception($"No assignee re-assignment happened in ticket {jiraTicket.Value}"), new Exception("Too many assignees"))
                          .ToArray();
      await lastAssignee.Select(assignee => jiraTicket.PutIssueAssigneeAsync(assignee.@from)).WhenAll();
      return lastAssignee.ToRestMonad(jiraTicket);
    }

    #region Resolvers
    private static KeyValuePair<string, object> ResolveIssueCustomFieldValue(this RestMonad<JiraNewIssue> rest, string fieldId, string fieldValue) {
      int fieldValueId;
      if(!int.TryParse(fieldValue, out fieldValueId)) {
        Func<string> pathFactory = () => IssuePath(rest.ApiAddress);
        var resp = rest.PostAsync(new Dictionary<string, object> { { fieldId, "XXX" } }, pathFactory).Result;
        fieldValueId = resp.HandleExecutedAsync((rsp, json) => { throw new InvalidOperationException("Jira error was expected."); }, (rex, t) => {
          var strRegex = @"(?<id>\d+)\[(?<name>[^]]+)]";
          var message = rex.Value.Message;
          if(!message.StartsWith(fieldId)) throw new InvalidDataException("Unexpected format returned from operation:" + System.Environment.NewLine + new string('*', 40) + System.Environment.NewLine + message);
          var options = Regex.Matches(message, strRegex).Cast<Match>().Select(m => new {
            id = m.Groups["id"].Value,
            value = m.Groups["name"].Value
          }).ToArray();
          var option = options.SingleOrDefault(o => Core.FilterCompare(o.value, fieldValue));
          if(option == null) throw new KeyNotFoundException(new { fieldValue, error = "Not found in collection:" + System.Environment.NewLine + string.Join(System.Environment.NewLine, options.Select(o => o + "")) } + "");
          return int.Parse(option.id);
        }, null).Result;
      }
      return new KeyValuePair<string, object>(fieldId, fieldValueId + "");
    }


    private static async Task<RestMonad<JiraNewIssue>> ResolveIssueTypeName(this RestMonad<JiraNewIssue> newIssuePost) {
      var projectIssueTypes = await RestConfiger.ProjectIssueTypes;

      var project = newIssuePost.Value.fields.project.key;
      Passager.ThrowIf(() => project.IsNullOrWhiteSpace());
      Passager.ThrowIf(() => !projectIssueTypes.ContainsKey(project), "{0}", new { project });

      var issueTypeName = newIssuePost.Value.fields.issuetype.name;
      Passager.ThrowIf(() => issueTypeName.IsNullOrWhiteSpace());

      issueTypeName = projectIssueTypes[project].Where(it => it.ToLower() == issueTypeName.ToLower())
        .Counter(1
        , new Exception(new { project, issueTypeName, not = "found" } + "")
        , new Exception(new { project, issueTypeName, too = "many" } + "")
        ).Single();
      newIssuePost.Value.fields.issuetype.name = issueTypeName;
      return newIssuePost;

      return await newIssuePost.ResolveIssueTypeName((issue, issueTypeId) => {
        issue.fields.issuetype.name = issue.fields.issuetype.id;
        issue.fields.issuetype.id = issueTypeId + "";
      });
    }
    private static async Task<RestMonad<JiraNewIssue>> ResolveIssueTypeName(this RestMonad<JiraNewIssue> newIssuePost, Action<JiraNewIssue, int> resolver) {
      JiraNewIssue newIssue = newIssuePost.Value;
      var newIssueTypeId = newIssue.fields.issuetype.id;
      if(!string.IsNullOrWhiteSpace(newIssueTypeId)) {
        int issueTypeId;
        if(!int.TryParse(newIssueTypeId, out issueTypeId)) {
          issueTypeId = int.Parse((await newIssuePost.GetIssueTypesAsync(newIssueTypeId)).Value.Select(p => p.id).DefaultIfEmpty("0").SingleOrDefault());
          if(issueTypeId == 0)
            throw new Exception(new { newIssueTypeId, address = newIssuePost.FullAddress(), message = "Issue Type does not exists in JIRA." } + "");
          resolver(newIssuePost.Value, issueTypeId);
          newIssue.fields.issuetype.id = issueTypeId + "";
        }
      }
      return newIssuePost;
    }
    public static async Task<RestMonad<JiraNewIssue>> ResolveComponents(this RestMonad<JiraNewIssue> restMonad) {
      var components = restMonad.Value.fields.components;
      if(components == null || components.Count == 0) return restMonad;
      var f = restMonad.Value.fields;
      Func<IssueClasses.Component, bool> mustResolve = cmp => string.IsNullOrEmpty(cmp.id);
      var resolved = f.components.Where(c => !mustResolve(c)).ToArray();
      var resolving = f.components.Where(mustResolve).ToArray();
      if(resolving.Any()) {
        if(resolving.Any(c => string.IsNullOrEmpty(c.name)))
          throw new Exception("Component name must be provided in order to resolve to it's id.");
        var componentIds = resolving.Select(c => c.name).ToArray();
        var projectIdOrKey = new[] { f.project.id, f.project.key }.First(s => !string.IsNullOrWhiteSpace(s));
        restMonad.Value.fields.components =
          (await restMonad.ResolveComponents(projectIdOrKey, componentIds))
          .Select(cid => new IssueClasses.Component() { id = cid })
          .Concat(resolved)
          .ToList();
      }
      return restMonad;
    }
    public static async Task<string[]> ResolveComponents(this RestMonad restMonad, string project, IList<string> components) {
      if(components == null || components.Count == 0) return new string[0];
      var resolvedComponents = (await restMonad.GetArrayAsync<Component>(ProjectComponentsPath(project), true, field => new[] { field.name }, components.ToArray())).Value;
      var excs = (from c in components
                  join rc in resolvedComponents on c.ToLower() equals rc.name.ToLower() into gc
                  from g in gc.DefaultIfEmpty()
                  where g == null
                  select new Exception(new { component = c, message = "Not found" } + "")
                  ).ToArray();
      if(excs.Any()) throw new AggregateException(excs);
      return resolvedComponents.Select(c => c.id).ToArray();
    }

    private static async Task<RestMonad<JiraNewIssue>> ResolveProjectAndIssueType(this RestMonad<JiraNewIssue> rest) {
      return await (
        from rm1 in rest.ResolveIssueTypeName()
        from rm2 in rm1.ResolveComponents()
        select rm2
        );
    }
    #endregion
    public static string LockIt(this string comment) { return IssueClasses.Issue.LockComment(comment, true); }
    public static string LockIt(this string comment, bool doLock) { return IssueClasses.Issue.LockComment(comment, doLock); }
    public static async Task<RestMonad<IssueClasses.Issue>> PostIssueAsync(this RestMonad rest
      , string project
      , string issueType
      , string summary
      , string comment) {
      return await rest.PostIssueAsync(project, issueType, summary, null, null, null, null, null, null, null, null);
    }
    public static async Task<RestMonad<IssueClasses.Issue>> PostIssueAsync(this RestMonad rest
        , string project
        , string issueType
        , string summary
        , string description
        , string assignee
        , string reporter
        , string[] components
        , Dictionary<string, object> customProperties
        , string comments
        , string[] fileNames
        , IDictionary<Uri, string> webLinks
      , Action<JiraNewIssue> issueAction = null
      ) {
      var newIssue = JiraNewIssue.Create(project, issueType, summary, description, assignee, reporter, components);
      if(issueAction != null)
        issueAction(newIssue);
      return await new RestMonad<JiraNewIssue>(newIssue, rest)
        .PostIssueAsync(customProperties, comments, fileNames, webLinks);
    }
    public static async Task<RestMonad<IssueClasses.Issue>> PostIssueAsync(this RestMonad<JiraNewIssue> newIssuePost, string comments, string[] fileNames) {
      return await newIssuePost.PostIssueAsync(null, comments, fileNames, new Dictionary<Uri, string>());
    }
    public static async Task<RestMonad<IssueClasses.Issue>> PostIssueAsync(this RestMonad<JiraNewIssue> newIssuePost, Dictionary<string, object> customProperties, string comments, string[] fileNames, IDictionary<Uri, string> webLinks) {
      (fileNames ?? new string[0]).Where(fn => !File.Exists(fn)).ForEach(fn => { throw new FileNotFoundException(fn); });
      var newIssue = await newIssuePost.PostIssueAsync(customProperties);
      var attachments = newIssue.PostTicketAttachment(fileNames);
      var ticketPost = newIssue.Clone<JiraTicket<string>, string>(ni => ni.key);
      if(webLinks != null) {
        var wlTasks = webLinks.Select(async wl => await ticketPost.PostWebLinkAsync(wl.Key.AbsoluteUri, wl.Value)).ToArray();
        await Task.WhenAll(wlTasks);
      }
      if(!string.IsNullOrWhiteSpace(comments)) {
        await ticketPost.PostCommentsAsync(comments);
      }
      return await newIssue.GetIssueAsync();
    }
    public static async Task<RestMonad<IssueTransitions.Transition>> PostIssueTransitionAsync(this JiraTicket<string> jiraTicket, string comment, bool doLock) {
      return await jiraTicket.PostIssueTransitionAsync(comment, doLock, null);
    }
    public static async Task<RestMonad<IssueTransitions.Transition>> PostIssueTransitionAsync(this JiraTicket<string> jiraTicket, string comment, bool doLock, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<IssueTransitions.Transition>> onError) {
      var issue = (await jiraTicket.GetIssueAsync()).Value;
      var noLoops = issue.transitions.Where(t => t.to.id != issue.fields.status.id).ToList();
      var trans = noLoops.Count == 1
        ? noLoops
        : issue.transitions.Count == 1
        ? issue.transitions
        : issue.NextTransition(false);
      if(trans.IsEmpty())
        throw new Exception(new { issue, error = "No 'Next' transition found" } + "");
      if(trans.Count > 2)
        throw new Exception(new { To_Many_Next_Transitions = trans.ToJson(), jiraTicket } + "");
      return await PostIssueTransitionAsync(jiraTicket, trans.Single().id, comment, doLock, onError);
    }
    public static async Task<RestMonad<IssueTransitions.Transition>> PostIssueTransitionAsync(this JiraTicket<string> jiraTicket, int transitionId, string comment, bool doLock, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<IssueTransitions.Transition>> onError) {
      return await PostIssueTransitionAsync(jiraTicket, IssueTransitions.Transition.Create(transitionId), comment, doLock, onError);
    }
    public static async Task<RestMonad<IssueTransitions.Transition>> PostIssueTransitionAsync(this JiraTicket<string> jiraTicket, string transitionName, string comment, bool doLock, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<IssueTransitions.Transition>> onError) {
      var transition = (await jiraTicket.GetIssueAsync()).Value.GetTransition(transitionName);
      return await PostIssueTransitionAsync(jiraTicket, transition, comment, doLock, onError);
    }

    public static async Task<RestMonad<IssueTransitions.Transition>> PostIssueTransitionAsync(
      JiraTicket<string> jiraTicket
      , IssueTransitions.Transition transition
      , string comment
      , bool doLock
      , Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<IssueTransitions.Transition>> onError) {
      ExceptionDispatchInfo edi = null;
      try {
        return await jiraTicket.PostIssueTransitionAsync(transition, Jira.Json.IssueClasses.Issue.LockComment(comment, doLock), onError);
      } catch(Exception exc) {
        edi = ExceptionDispatchInfo.Capture(exc);
      }

      await jiraTicket.PostCommentsAsync(edi.SourceException.Message, true);
      edi.Throw();
      return null;
    }

    public static async Task<RestMonad<IssueTransitions.Transition>> PostIssueTransitionAsync(this JiraTicket<string> jiraTicket
      , IssueTransitions.Transition transition
      , string comment
      , Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<IssueTransitions.Transition>> onError = null) {

      if(!string.IsNullOrWhiteSpace(comment)) {
        await jiraTicket.PostCommentsAsync(comment);
        comment = null;
      }


      var t = new {
        update = string.IsNullOrWhiteSpace(comment)
          ? new object()
          : MakeUpdateComment(comment),
        fields = new { },
        transition = new { transition.id }
      };

      return await (
        from resp in jiraTicket.PostIssueAsync(IssueTransitionsPath, t)
        from s in resp.HandleExecutedAsync((response, _) => response.Switch(transition), onError, null)
        select s);
    }

    private static object MakeUpdateComment(string comment) {
      return new { comment = new[] { new { add = new IssueClasses.Comment() { body = comment } } } };
    }
    public static object MakeUpdateSecurity(string name) {
      return new { security = new { name } };
    }
    public static object MakeUpdateAssignee(string name) {
      return new { assignee = new { name } };
    }

    public static async Task<RestMonad<string>> PostWatcherAsync(this JiraTicket<string> ticket, string watcherUserName) {
      if((await ticket.Clone<RestMonad<User>, User>(User.FromUserName(watcherUserName)).GetAsync()).Value == null)
        throw new Exception("User [" + watcherUserName + "] is not found in JIRA");
      return await (await ticket.PostIssueAsync(tckt => IssueWatchersPath()(tckt, ticket.ApiAddress), watcherUserName))
       .HandleExecutedAsync((response, json) => response.Clone<RestMonad<string>, string>(json), null, null);
    }
    public static async Task<SelfId> PostWebLinkAsync(this JiraTicket<string> ticket, string url, string title) {
      var t = new WebLinkPost { @object = new Json.WebLinkPost.Inner { url = url, title = title } };
      return await (await ticket.PostIssueAsync(tckt => IssueWebLinkPath(tckt, ticket.ApiAddress), t))
       .HandleExecutedAsync((response, json) => JsonConvert.DeserializeObject<SelfId>(json), null, null);
    }
    public static async Task<IList<RemoteLink>> GetWebLinkAsync(this JiraTicket<string> ticket, params string[] title) {
      return (await ticket.GetArrayAsync<RemoteLink>(tckt => IssueWebLinkPath(ticket.Value, ticket.ApiAddress), true, rl => new[] { rl.@object.title }, title)).Value;
    }
    public static async Task<RestMonad> PostIssueLinkAsync(this JiraTicket<string> ticket, string outwardIssue, string comment, string issueLinkType = "Relationship") {
      var issueLink = NewIssueLink.Create(ticket.Value, outwardIssue, comment, issueLinkType);
      var rm = ticket.Clone<RestMonad<NewIssueLink>, NewIssueLink>(issueLink);
      return await (await rm.PostAsync(new Field<object>[0], () => IssueLinkPath()(rm.ApiAddress)))
        .HandleExecutedAsync((response, json) => rm, null, null);
    }
    public static async Task<RestMonad<WorklogOut[]>> PostWorklogForTransitionerAsync(this JiraTicket<string> jiraTicket, WebHook.Transition webhookTransition) {
      return await jiraTicket.PostWorklogAsync(webhookTransition.from_status, webhookTransition.to_status);
    }
    public static async Task<RestMonad<WorklogOut[]>> PostWorklogAsync(string jiraTicket, string statusFrom, string statusTo)
      => await jiraTicket.ToJiraTicket().PostWorklogAsync(statusFrom, statusTo);
    public static async Task<RestMonad<WorklogOut[]>> PostWorklogAsync(this JiraTicket<string> jiraTicket, string statusFrom, string statusTo) {
      var x = (await (await (from wls in jiraTicket.GetWorklogSubject(statusFrom, statusTo)
                             from ts in wls.Value
                             select jiraTicket.PostWorklogAsync(ts.Item1, ts.Item2, new { ts.Item4.name, ts.Item4.displayName, stateFrom = ts.Item3.FromState, stateTo = ts.Item3.ToState, @by = ts.Item3.Author?.displayName }.ToJson(false))))
                            .WhenAll())
                            .Select(rm => rm.Value)
                            .ToArray();
      return jiraTicket.Clone<RestMonad<WorklogOut[]>, WorklogOut[]>(x);
    }
    public static async Task<RestMonad<WLS>> GetWorklogSubject(this JiraTicket<string> ticket, string statusFrom, string statusTo) {
      var rm = (await ticket.GetIssueAsync(new[] { IssueExpander.changelog }));
      var issue = rm.Value;
      var history = issue.TransitionHistory().Reverse()
        .SkipWhile(h => h.ToState.ToLower() != statusTo.ToLower() || h.FromState.ToLower() != statusFrom.ToLower())
        .Take(2)
        .Reverse()
        .ToArray();
      var doLog = history.Skip(1);//.Where(h => stateFrom.IsNullOrWhiteSpace() || h.FromState.ToLower() == stateFrom.ToLower());
      return rm.Clone<RestMonad<WLS>, WLS>((
        from endHist in doLog//.Select(h => new { h.Date, h.Author.name })
        from startHist in history.Take(1).Select(h => new { Date = h.Date, h.Author?.name })
        select (startHist.Date.LocalDateTime, endHist.Date - startHist.Date, endHist, issue.fields.assignee)
        )
        .Where(t => t.Item2.TotalMinutes > 0)
        .ToList());
    }

    static async Task<RestMonad<WorklogOut>> PostWorklogAsync(this JiraTicket<string> ticket, DateTime dateStarted, TimeSpan timeSpent, string comment) {
      var t = WorklogIn.Create(dateStarted, timeSpent, comment);
      return await (await ticket.PostIssueAsync(tckt => IssueWorklogPath()(tckt, ticket.ApiAddress), t))
       .HandleExecutedAsync((response, json) => response.Clone<RestMonad<WorklogOut>, WorklogOut>(JsonConvert.DeserializeObject<WorklogOut>(json)), null, null);
    }

    public static async Task<IList<RemoteLink>> GetIssueLinkAsync(this JiraTicket<string> ticket, params string[] title) {
      return (await ticket.GetArrayAsync<RemoteLink>(tckt => IssueWebLinkPath(ticket.Value, ticket.ApiAddress), true, rl => new[] { rl.@object.title }, title)).Value;
    }

    public static async Task<RestMonad<Uri>> PostCommentsAsync(this JiraTicket<string> ticket, string comment) {
      return await ticket.PostCommentsAsync(comment, null);
    }
    public static async Task<RestMonad<Uri>> PostCommentsAsync(this JiraTicket<string> ticket, string comment, bool isError, string visibilityRole = null) {
      return await ticket.PostCommentsAsync(
        IssueClasses.Issue.ErrorComment(comment, isError),
        !string.IsNullOrWhiteSpace(visibilityRole)
          ? new IssueClasses.Visibility() { type = "role", value = visibilityRole }
          : null);
    }
    public static async Task<RestMonad<Uri>> PostCommentsAsync(this JiraTicket<string> ticket, string comment, IssueClasses.Visibility visibility) {
      var t = new IssueClasses.Comment() { body = comment, visibility = visibility };
      return await (await ticket.PostIssueAsync(tckt => IssueCommentPath(tckt), t))
       .HandleExecutedAsync((response, json) => response.Clone<RestMonad<Uri>, Uri>(response.Value.Headers.Location), null, null);
    }
    #region PostTicketAttachment
    public static Attachment[] PostTicketAttachment(this RestMonad<IssueClasses.Issue> jiraTicket, params string[] fileNames) {
      return jiraTicket.PostTicketAttachment(jiraTicket.Value.key, fileNames);
    }
    public static Attachment[] PostTicketAttachment(this JiraTicket<string> jiraTicket, params string[] fileNames) {
      return jiraTicket.PostTicketAttachment(jiraTicket.Value, fileNames);
    }
    public static Attachment[] PostTicketAttachment(this RestMonad rm, string jiraTicket, params string[] fileNames) {
      return PostTicketAttachment(rm.BaseAddress.AbsoluteUri,
        IssueAttachmentPath(jiraTicket),
        rm.UserName ?? JiraMonad.JiraPowerUser(),
        rm.Password == null ? JiraMonad.JiraPowerPassword() : rm.Password.GetValue(), fileNames);
    }
    static Attachment[] PostTicketAttachment(string jiraHost, string jiraRestUri, string userName, string password
      , params string[] fileNames) {
      if(fileNames == null || !fileNames.Any()) return new Attachment[0];
      Guard.NotNull(() => jiraHost, jiraHost); Guard.NotNull(() => jiraRestUri, jiraRestUri); Guard.NotNull(() => userName, userName); Guard.NotNull(() => password, password);
      using(var client = new HttpClient()) {
        client.BaseAddress = new Uri(RestMonad.EnsurePath(jiraHost));
        client.InitBasicAuthenticationHeader(userName, password);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var boundary = string.Format("--{0:N}", Guid.NewGuid().ToString().Replace("-", ""));
        var requestContent = new MultipartFormDataContent(boundary);
        var contentType = "multipart/form-data;  boundary=" + boundary;
        requestContent.Headers.ContentType.CharSet = "UTF-8";
        requestContent.Headers.Add("X-Atlassian-Token", "no-check");
        fileNames.ToList().ForEach(fileName => {
          var imageContent = new ByteArrayContent(System.IO.File.ReadAllBytes(fileName));
          imageContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
          requestContent.Add(imageContent, "file", Path.GetFileName(fileName));
        });
        try {
          return Task.Factory.StartNew(() =>
            client.PostAsync(jiraRestUri, requestContent)
            .Result.ToJiraTicket()
            .HandleExecutedAsync((r, json) => JsonConvert.DeserializeObject<Attachment[]>(json), null, null).Result
            ).Result;
        } catch(Exception exc) {
          throw new Exception(new { fileNames = string.Join(",", fileNames) } + "", exc);
        }
      }
    }

    public static async Task<RestMonad<IList<Outwardissue>>> GetIssueLinks(this JiraTicket<string> jiraTicket, string projectTo, string issueTo) {
      var issue = (await jiraTicket.GetIssueAsync(new[] { Rest.IssueFielder.issuelinks })).Value;
      List<Outwardissue> links = issue.HasLinks(projectTo, issueTo);
      return jiraTicket.Clone<RestMonad<IList<Outwardissue>>, IList<Outwardissue>>(links);
    }


    public static async Task<RestMonad<IssueClasses.Issue>> CloneIssueAndLink(this JiraTicket<string> jiraTiket, string project2, string issueType2, params string[] fields) {
      return await jiraTiket.CloneIssueAndLink(project2, issueType2, new Dictionary<string, object>(), fields);
    }
    public static Task<RestMonad<IssueClasses.Issue>> CloneIssueAndLink(this JiraTicket<string> jiraTiket, string project2, string issueType2, Dictionary<string, object> customFields, params string[] fields) {
      return (from i in jiraTiket.GetIssueAsync()
              from t1 in i.CloneIssueAndLink(project2, issueType2, customFields, fields)
              select t1);
    }
    public static async Task<RestMonad<IssueClasses.Issue>> CloneIssueAndLink(this RestMonad<IssueClasses.Issue> issue, string project2, string issueType2, params string[] fields) {
      return await issue.CloneIssueAndLink(project2, issueType2, new Dictionary<string, object>(), fields);
    }
    public static async Task<RestMonad<IssueClasses.Issue>> CloneIssueAndLink(this RestMonad<IssueClasses.Issue> issue, string project2, string issueType2, Dictionary<string, object> customFields, params string[] fields) {
      customFields = new Dictionary<string, object>(customFields ?? new Dictionary<string, object>(), StringComparer.OrdinalIgnoreCase);
      var links = issue.Value.HasLinks(project2, issueType2).Take(1);
      if(links.Any())
        return await links.First().key.ToJiraTicket(issue).GetIssueAsync();
      var properties = (from f in fields ?? new string[0]
                        join p in issue.Value.fields.GetType().GetAllProperties().ToArray() on f.ToLower() equals p.Name.ToLower() into gj
                        from p in gj.DefaultIfEmpty()
                        select new { f, p }).ToArray();
      properties.Where(x => x.p == null).Select(x => x.f)
        .Select(field => new { field, value = issue.Value.ExtractCustomFieldRaw(field, true, false) })
        .Where(x => x.value != null)
        .ForEach(field => field.value.ForEach(value => {
          if(!customFields.ContainsKey(field.field))
            customFields.Add(field.field, value);
        }));
      var jiraTicket = issue.Clone<JiraTicket<string>, string>(issue.Value.key);
      Action<JiraNewIssue> mapProps = newIssue => {
        (from p1 in newIssue.fields.GetType().GetAllProperties()
         join p2 in properties.Where(x => x.p != null).Select(x => x.p) on p1.Name equals p2.Name
         select new { p1, Value = p2.GetValue(issue.Value.fields) }
         ).ForEach(x => x.p1.SetValue(newIssue.fields, x.Value));
        var tt = newIssue.fields.timetracking;
        if(tt != null) tt.originalEstimate = null;
      };
      var issue2 = (await jiraTicket.PostIssueAsync(project2, issueType2, issueType2 + "::" + issue.Value.fields.summary, "", "", "", null, customFields, "", null, null, mapProps)).Value;
      await jiraTicket.PostIssueLinkAsync(issue2.key, issue2.key + " was created");
      return issue.Switch(issue2);
    }

    #endregion
    #endregion

    #region Stand-Alongs
    public static async Task<RestMonad<Json.Group>> GetAsync(this (RestMonad rm, string groupName) group) =>
      await Json.Group.FromUserName(group.groupName).ToRestMonad(group.rm).GetAsync();
    public static async Task<RestMonad<Json.Group>> GetAsync(this RestMonad<Json.Group> rm) {
      return (await rm.GetAsync<Json.Group>(() => GroupPath(rm.Value.name), null, null, (hrm, json) => hrm.Switch((Json.Group)null)));
    }
    public static async Task<RestMonad<Json.Group>> PostAsync(this (RestMonad rm, string groupName) group) {
      return await Jira.Json.Group.FromUserName(group.groupName).ToRestMonad(group.rm).PostAsync();
    }
    public static async Task<RestMonad<Json.Group>> PostAsync(this RestMonad<Json.Group> rm) {
      return await rm.PostAsync<Json.Group>(() => GroupPath(rm.Value.name), rm.Value);
    }
    public static async Task<RestMonad> DeleteAsync(this RestMonad<Json.Group> rm) {
      return await rm.DeleteAsync(GroupPath(rm.Value.name));
    }
    public static Task<RestMonad<User[]>> GetAsync(this RestMonad<User[]> RestMonad) => RestMonad.GetAsync("");
    public static async Task<RestMonad<User[]>> GetAsync(this RestMonad<User[]> RestMonad, string userName) {
      var x = from users in RestMonad.Value
                .Where(u => userName.IsNullOrWhiteSpace() || u.name.ToLower() == userName.ToLower())
                .Select(u => User.FromKey(u.key).ToRestMonad(RestMonad).GetAsync())
              from u in users
              select u.Value;
      return (await x).ToArray().ToRestMonad(RestMonad);
    }
    public static async Task<RestMonad<Json.User>> GetByUserNameAsync(this (RestMonad rm, string userName) user) =>
      await Json.User.FromUserName(user.userName).ToRestMonad(user.rm).GetAsync();
    public static Task<RestMonad<Json.User>> PostOrGetAsync(this Json.User user) => user.ToRestMonad().PostOrGetAsync();
    public static async Task<RestMonad<Json.User>> PostOrGetAsync(this RestMonad<Json.User> rm) {
      var x2 = await rm.PostAsync<Task<Json.User>>(() => UserPath(), rm.Value,
        (x, y) => x.Switch(Task.FromResult(JsonConvert.DeserializeObject<User>(y))),
          (hrm, t) => {
            var error = JiraMonad.ParseJiraError(hrm.Value.Json);
            if(error.Contains("already exists"))
              return hrm.Switch(getUser());
            throw hrm.Value;
          }, false);
      return x2.Switch(await x2.Value);
      async Task<User> getUser() => (await rm.GetAsync()).Value;
    }
    public static Task<RestMonad<User>> DeleteAsync(this Json.User rm) => rm.ToRestMonad().DeleteAsync();
    public static Task<RestMonad<User>> DeleteAsync(this RestMonad<Json.User> rm) =>
      rm.DeleteAsync(UserPath(Tuple.Create("username", rm.Value.name))(ApiPath));
    public static Task<RestMonad<User>> GetAsync(this User user) => user.ToRestMonad().GetAsync();
    public static async Task<RestMonad<User>> GetAsync(this RestMonad<User> RestMonad) {
      var pathFactory = UserPath(Tuple.Create("username", RestMonad.Value.name), Tuple.Create("key", RestMonad.Value.key), Tuple.Create("expand", RestMonad.Value.expand));
      return (await RestMonad.GetAsync<User>(pathFactory, null, null, (hrm, json) => hrm.Switch<User>(null)));
    }
    public static async Task<RestMonad> AddToGroupAsync(this RestMonad<User> RestMonad, string groupName) {
      var pathFactory = GroupAddUserPath(groupName);
      return (await RestMonad.PostAsync(() => pathFactory, RestMonad.Value, (hrm, json) => hrm.Switch<User>(null), null, false));
    }
    public static Task<RestMonad<User[]>> GetUsersAsync(this RestMonad RestMonad, string user = "") => RestMonad.GetUsersAsync(10000, user);
    public static async Task<RestMonad<User[]>> GetUsersAsync(this RestMonad RestMonad, int maxResults, string user = "") {
      return await RestMonad.GetAsync<User[]>(UsersPath(user, maxResults), null);
    }
    public static Task<RestMonad<User[]>> GetUsersWithGroups(this RestMonad rme) => rme.GetUsersWithGroups(10000);
    public static Task<RestMonad<User[]>> GetUsersWithGroups(this RestMonad rme, int maxResults) => rme.GetUsersWithGroups(maxResults, "");
    public static Task<RestMonad<User[]>> GetUsersWithGroups(this RestMonad rme, string userName) => rme.GetUsersWithGroups(100000, userName);
    public static Task<RestMonad<User[]>> GetUsersWithGroups(this RestMonad rme, int maxResults, string userName) =>
      (from rmUser in rme.GetUsersAsync(maxResults)
       from user in rmUser.GetAsync(userName)
       select user);

    public static async Task<RestMonad<User[]>> GetUsersWithPermission(this JiraTicket<string> jiraTicket, string user, string permission = "BROWSE") {
      return await jiraTicket.GetAsync<User[]>(UserWithPermissionPath(jiraTicket.Value, user, permission)
        , null, null
        , (hrm, json) => new User[0].ToRestMonad());
    }

    public static Task<RestMonad<Project[]>> GetProjectsAsync(this RestMonad RestMonad) => RestMonad.GetAsync<Project[]>(ProjectsPath(), null);
    public static Task<RestMonad<Project>> GetProjectAsync(this RestMonad RestMonad, string projectKey) =>
      RestMonad.GetAsync<Project>(ProjectPath(projectKey), null);
    public static Task<RestMonad<Project>> CreateDefaultProjectAsync(this RestMonad rm, string name, string key, string description, string lead) =>
      new ProjectNew {
        key = key,
        name = name,
        projectTypeKey = "business",
        projectTemplateKey = "com.atlassian.jira-core-project-templates:jira-core-task-management",
        description = description,
        lead = lead,
        assigneeType = "PROJECT_LEAD",
        avatarId = 10000,
        issueSecurityScheme = 10000,
        //permissionScheme = 10000,
        notificationScheme = 10000,
        categoryId = 10020
      }.ToRestMonad().PostAsync();
    public static Task<RestMonad<Project>> CreateProjectAsync(this RestMonad rm
      , string name
      , string key
      , string description
      , string lead
      , int issueSecurityScheme = 10000
      , int permissionScheme = 10000
      , int notificationScheme = 10000
      , int categoryId = 10020
      ) =>
      new ProjectNew {
        key = key,
        name = name,
        projectTypeKey = "business",
        projectTemplateKey = "com.atlassian.jira-core-project-templates:jira-core-task-management",
        description = description,
        lead = lead,
        assigneeType = "PROJECT_LEAD",
        avatarId = 10000,
        issueSecurityScheme = issueSecurityScheme,
        permissionScheme = permissionScheme,
        notificationScheme = notificationScheme,
        categoryId = categoryId
      }.ToRestMonad().PostAsync();

    public static Task<RestMonad<Project>> PostAsync(this RestMonad<ProjectNew> rm) =>
      from p in rm.PostAsync<Project>(ProjectPath, rm.Value)
      from p1 in p.GetProjectAsync(rm.Value.key)
      select p1;

    public static Task<IList<RestMonad<string>>> ProjectCleanWorkflowsAsync(this RestMonad rm, string projectKey, string keyPhrase = "DELETE_WF")
      => rm.ProjectCleanWorkflowsAsync(projectKey, new string[0], keyPhrase);
    public static Task<IList<RestMonad<string>>> ProjectCleanWorkflowsAsync(this RestMonad rm, string projectKey, string issueTypeToClean, string keyPhrase = "DELETE_WF")
      => rm.ProjectCleanWorkflowsAsync(projectKey, new[] { issueTypeToClean }, keyPhrase);
    public static async Task<IList<RestMonad<string>>> ProjectCleanWorkflowsAsync(this RestMonad rm, string projectKey, IList<string> issueTypesToClean, string keyPhrase = "DELETE_WF") {
      var project = (await rm.GetProjectAsync(projectKey)).Value;
      Passager.ThrowIf(() => !keyPhrase.IsNullOrWhiteSpace() && !project.description.Contains(keyPhrase));
      // Delete tickets
      await (await (from res in rm.Search(projectKey, "", "", 0, false, new string[0])
                    from issue in res
                    select issue.id.ToJiraTicket().DeleteIssueAsync()
                      )).WhenAllSequiential(); ;
      // Map Workflow to IssueType
      var defaultWorkflow = projectKey + ": Task Management Workflow";
      var dwfExists = (await RestMonad.Empty().GetWorkflows(defaultWorkflow)).Value.Any();
      if(!dwfExists) {
        var twf = "ACO: Task Management Workflow";
        var xml = await GetWorkflowXmlAsync(twf);
        await Rest.PostWorkflowAsync(defaultWorkflow, "", xml);
      }
      var issueTypes = project.issueTypes.AsEnumerable();
      if(issueTypesToClean?.Count > 0)
        issueTypes = (from it in issueTypes
                      join itd in issueTypesToClean on it.name equals itd
                      select it);
      await ProjectIssueTypeAddOrDelete(projectKey, "Sub-task");
      return await (from it in issueTypes.Select(it => it.name).Concat(new[] { "" }).Distinct()
                    select rm.WorkflowIssueTypeAttach(projectKey, it, defaultWorkflow)
                     ).WhenAllSequiential();
    }
    public static Task<RestMonad<HttpResponseMessage>> DeleteProjectAsync(this RestMonad RestMonad, string projectKey) =>
      RestMonad.DeleteAsync(ProjectPath(projectKey)(ApiPath));
    public static async Task<(JObject jObject, IList<Exception> errors)> DestroyProjectAsync(this RestMonad rm, string projectKey, bool deleteProject = false) {
      var project = (await rm.GetProjectAsync(projectKey)).Value;
      Passager.ThrowIf(() => !project.description.Contains("DELETE_ME"));
      var issueTypeSchemeId = (await rm.GetProjectIssueTypeSchemeId(projectKey)).Value.id;
      var workflowSchemeId = (await rm.ProjectWorkflowShemeGetAsync(projectKey)).Value.parentId;
      var workflows = (await rm.GetWorkflowShemeWorkflowAsync(workflowSchemeId)).Value.Select(w => w.workflow).ToArray();
      var projectIssueTypeScreenSchemeRest = (await rm.GetProjectIssueTypeSceenScheme(projectKey).WithError());
      var projectIssueTypeScreenScheme = projectIssueTypeScreenSchemeRest.error == null
        ? projectIssueTypeScreenSchemeRest.value.Value
        : (id: 0, screenSchemeIds: new int[0], screenIds: new int[0]);
      var screenSchemeIds = projectIssueTypeScreenScheme.screenSchemeIds;
      var screenIds = projectIssueTypeScreenScheme.screenIds;
      //Assert.Inconclusive("Remove to delete project");
      var errors = new List<Exception>();
      var dp = deleteProject == false
        ? new { issueTypeSchemeId, error = (Exception)null }
        : await (from p in WithErrorMonad.Create(() => rm.DeleteProjectAsync(projectKey))()
                 select new { issueTypeSchemeId, error = readError(p.error) }
                      );
      var dit = await WithErrorMonad.Create(() => DeleteIssueTypeSchemeAsync(issueTypeSchemeId))();

      var dws = await WithErrorMonad.Create(() => DeleteWorkflowSchemeAsync(workflowSchemeId))();
      var dwfs = await from workflow in workflows
                       from t in WithErrorMonad.Create(() => DeleteWorkflowAsync(workflow))()
                       select new { workflow, t.value, error = readError(t.error) };

      var ditss = await from xd in WithErrorMonad.Create(() => DeleteIssueTypeScreenSchemeAsync(projectIssueTypeScreenScheme.id))()
                        select new { projectIssueTypeScreenScheme, xd.value, error = readError(xd.error) };

      var dss = await from screenSchemeId in screenSchemeIds
                      from t in WithErrorMonad.Create(() => DeleteScreenScheme(screenSchemeId))()
                      select new { screenSchemeId, t.value, error = readError(t.error) };

      var ds = await from screenId in screenIds
                     from t in WithErrorMonad.Create(() => DeleteScreen(screenId))()
                     select new { screenId, t.value, error = readError((t.error)) };

      var dsbp = await from xd in WithErrorMonad.Create(async () => { await ScreenDeleteByProject(projectKey, false); return ""; })()
                       select new { projectKey, xd.value, error = readError(xd.error) };
      var jo = JObject.FromObject(new {
        create = new {
          issueTypeSchemeId = issueTypeSchemeId,
          workflowSchemeId,
          workflows = workflows.Flatten(),
          projectIssueTypeScreenSchemeId = projectIssueTypeScreenScheme.id,
          screenSchemeIds = screenSchemeIds.Flatten(),
          screenIds = screenIds.Flatten()
        },
        delete = new {
          dp,
          dit,
          dws,
          dwfs = dwfs.Where(t => t.error != null).ToArray(),
          ditss,
          dss = dss.Where(t => t.error != null).ToArray(),
          ds = ds.Where(t => t.error != null).ToArray(),
          dsbp
        }
      });
      return (jo, errors);
      Exception readError(Exception exc) {
        if(exc != null) errors.Add(exc);
        return exc;
      }
    }

    public static async Task<RestMonad<Tuple<string, string>[]>> GetProjectsIssueTypesAsync(this RestMonad rme) {
      var x = await (
        from rm in rme.GetProjectsAsync()
        from rm2 in rm.Value.Select(p => rme.GetProjectAsync(p.key)).WhenAll()
        from p in rm2
        select p.Value.issueTypes.Select(it => Tuple.Create(p.Value.key, it.name))
       );
      return x.Concat().ToArray().ToRestMonad(rme);
    }
    public static async Task<RestMonad<Dictionary<string, string[]>>> GetProjectIssueTypesAsync(this RestMonad rme) {
      return (await (
        from rm in rme.GetProjectsAsync()
        from rm2 in rm.Value.Select(p => rme.GetProjectAsync(p.key)).WhenAll()
        from p in rm2
        select p.Value
       ))
       .ToDictionary(p => p.key, p => p.issueTypes.Select(it => it.name).ToArray(), StringComparer.OrdinalIgnoreCase)
       .ToRestMonad(rme);
    }

    public static async Task<RestMonad<Jira.Json.ProjectIssueStatuses[]>> GetProjectIssueStatusesAsync(this RestMonad rest, string project) {
      return await rest.GetArrayAsync<ProjectIssueStatuses>(ProjectIssueStatusesPath(project), true, it => it.name);
    }
    public static async Task<RestMonad<Dictionary<string, string>>> ProjectRolesGetAsync(this RestMonad rest, string project) {
      return await rest.GetAsync<Dictionary<string, string>>(ProjectRolesPath(project), null);
    }
    public static async Task<RestMonad> ProjectRolesClearAsync(this RestMonad rm, string projectKey) {
      var projectRoles = (await (await (
        from rolesRM in rm.ProjectRolesGetAsync(projectKey)
        from roleUrl in rolesRM.Value.Values
        select rolesRM.GetAsync(() => roleUrl, (hrm, json) => Core.Return<Role>(hrm, json), null, null)
        )).WhenAllSequiential()
        )
        .Where(role => role.Value.actors.Any())
        .OrderBy(role => role.Value.name)
        .ToList();

      // Clean group from project role
      var x = await (from role in projectRoles
                     from actor in role.Value.actors
                     select role.ProjectRoleRemoveMemberAsync(projectKey, role.Value.id, actor)
       ).WhenAllSequiential();
      return x.FirstOrDefault() ?? rm;
    }

    public static async Task<RestMonad> ProjectRoleRemoveMemberAsync(this RestMonad rest, string project, int roleId, Actor actor) {
      string ActorName(Actor a) => a.IsUser ? a.name.ToLower() : a.name;
      return await rest.DeleteAsync($"{ProjectRoleByIdPath(project, roleId)}?{actor.Type}={ActorName(actor)}");
    }
    public static async Task<RestMonad<Role[]>> RolesGetAsync(this RestMonad rest, params string[] roles) {
      return await rest.GetArrayAsync<Role>(RolesPath, true, r => r.name, roles);
    }
    public static async Task<RestMonad<Role>> GetProjectRoleAsync(this RestMonad rest, string project, int roleId) {
      return await rest.GetAsync<Role>(ProjectRolePath(project, roleId), null);
    }
    public static async Task<RestMonad<Role[]>> ProjectRolePostAsync(this RestMonad rest, string project, string roleName, params string[] groupName) {
      var x = await (await (from roles in rest.RolesGetAsync(roleName)
                            from role in roles.Value.Select(role => roles.ProjectRolePostAsync(project, role.id, groupName))
                            select role
       )).WhenAllSequiential();
      return x.SingleOrDefault()?.Switch(x.Select(rm => rm.Value).ToArray()) ?? new Role[0].ToRestMonad();
    }
    public static async Task<RestMonad<Role>> ProjectRolePostAsync(this RestMonad rest, string project, int roleId, params string[] groupName) {
      return await rest.PostAsync(() => ProjectRoleByIdPath(project, roleId), new { group = groupName }, Core.Return<Role>);
    }
    public static async Task<RestMonad<Role[]>> ProjectRolesRemoveMemberAsync(this RestMonad rest, string project, string roleName, string member) {
      var x = await (await (
        from roleRM in rest.RolesGetAsync(roleName)
        select roleRM.Value.Select(role => roleRM.ProjectRolesRemoveMemberAsync(project, role.id, member))
       )).WhenAllSequiential();
      return x.DefaultIfEmpty(new Role[0].ToRestMonad()).Single();
    }
    public static async Task<RestMonad<Role[]>> ProjectRolesRemoveMemberAsync(this RestMonad rest, string project, int roleId, string member) {
      var x = await (await (
        from role in rest.GetProjectRoleAsync(project, roleId)
        from actor in role.Value.actors.Where(a => a.name.ToLower() == member.ToLower())
        select role.DeleteAsync($"{role.Value.self}?{actor.Type}={actor.name}")
        )).WhenAllSequiential();
      return x.FirstOrDefault()?.Switch(x.Select(role => role.Value).ToArray()) ?? new Role[0].ToRestMonad();
    }
    public static T ToObject<T>(this JToken jt, Func<T> returnTypeTemplate) {
      return jt.ToObject<T>();
    }

    #region Custom fields meta
    /// <summary>
    /// Get custom fields meta data(name, values) from project/issuetype pair
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <param name="restMonad"></param>
    /// <param name="valueProjector">valueId,ValueName</param>
    /// <param name="nameProjector">fieldId,fieldName</param>
    /// <returns>Collection of metadata for all custom fields</returns>
    ///                                      GetIssueCustomFields
    public static async Task<RestMonad<U[]>> GetIssueCustomFields<T, U>(this RestMonad<JiraNewIssue> restMonad, Func<string, string, T> valueProjector, Func<string, string, T[], U> nameProjector) {
      return await (
        from rm in restMonad.ResolveProjectAndIssueType()
        from rm1 in rm.GetAsync(IssueCreateMetaWithFieldsPath(rm.Value.fields.project.key, rm.Value.fields.issuetype.name))
        from rm2 in
          rm1.HandleExecutedAsync((resp, json) => {
            var jo = ((JContainer)(JsonConvert.DeserializeObject(json)));
            var a = jo.With("projects").With(0).With("issuetypes").With(0).With("fields");
            if(a == null) throw new HttpRestException(resp, new { DoesNotExist = RestMonad.SerializeObject(restMonad.Value) } + "");
            U[] customField = a.ExtractFieldsMeta(valueProjector, nameProjector);
            return restMonad.Switch(customField);
          })
        select rm2);
    }

    /// <summary>
    /// Get custom fields meta data(name, values) from project/issuetype pair
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <param name="restMonad"></param>
    /// <param name="valueProjector">valueId,ValueName</param>
    /// <param name="nameProjector">fieldId,fieldName</param>
    /// <returns>Collection of metadata for all custom fields</returns>
    public static async Task<RestMonad<U[]>> GetIssueCustomFields<T, U>(this JiraTicket<string> restMonad, Func<string, string, T> valueProjector, Func<string, string, T[], U> nameProjector) {
      return await (
        from rm1 in restMonad.GetIssueRelatedAsync(IssueEditMetaPath())
        from rm2 in
          rm1.HandleExecutedAsync((resp, json) => {
            var jo = ((JContainer)(JsonConvert.DeserializeObject(json)));
            var a = jo.With("fields");
            if(a == null) throw new HttpRestException(restMonad.BaseAddress + "", new { property = "fields", DoesNotExist = RestMonad.SerializeObject(restMonad.Value), @in = resp } + "");
            U[] customField = a.ExtractFieldsMeta(valueProjector, nameProjector);
            return restMonad.Switch(customField);
          })
        select rm2);
    }

    private static U[] ExtractFieldsMeta<T, U>(this JContainer a, Func<string, string, T> valueProjector, Func<string, string, T[], U> nameProjector) {
      return (
        from jp in a.Cast<JProperty>()
        where jp.Name.StartsWith("customfield", StringComparison.OrdinalIgnoreCase)
        let alowedValues = jp.Value["allowedValues"]
        where alowedValues != null
        let avs = alowedValues.ToObject(() => new[] { new { self = "", value = "", id = "" } }).Select(av => valueProjector(av.id, av.value)).ToArray()
        select nameProjector(jp.Name, jp.Value["name"] + "", avs)
        ).ToArray();
    }

    public static async Task<RestMonad<string>> GetIssueCreateMetaJson(this RestMonad<JiraNewIssue> restMonad) {
      return await (
        from rm in restMonad.ResolveProjectAndIssueType()
        from rm1 in rm.GetAsync(IssueCreateMetaWithFieldsPath(rm.Value.fields.project.key, rm.Value.fields.issuetype.name), (resp, json) => resp.Switch(RestMonad.FormatJson(json)), null, null)
        select rm1
        );
    }
    /// <summary>
    /// To be used in case of emergency, when nothing else works
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="rest"></param>
    /// <param name="fieldId"></param>
    /// <param name="projector"></param>
    /// <returns></returns>
    async public static Task<RestMonad<T[]>> GetIssueCustomFieldValues<T>(this RestMonad<JiraNewIssue> rest, string fieldId, Func<string, string, T> projector) {
      rest.Value.fields.summary = "summary";
      rest.Value.fields.description = "description";
      return await (
        from rm in rest.ResolveProjectAndIssueType()
        from rm2 in rm.PostAsync(new Dictionary<string, object> { { fieldId, "XXX" } }, () => IssuePath(rest.ApiAddress))
        from rm3 in rm2.HandleExecutedAsync((rsp, json) => {
          throw HttpRestException.Create(rsp.Value.RequestMessage.RequestUri.AbsoluteUri, new InvalidOperationException("Jira error was expected, but was not thrown."));
        }, (rex, t) => {
          var regex = @"(?<id>\d+)\[(?<name>[^]]+)]";
          var message = rex.Value.Message;
          if(!message.StartsWith(fieldId))
            throw HttpRestException.Create(rex.Value.Address, new InvalidDataException("Unexpected format returned from operation:" + System.Environment.NewLine + new string('*', 40) + System.Environment.NewLine + message));
          return rest.Switch(Regex.Matches(message, regex).Cast<Match>().Select(m => projector(m.Groups["id"].Value, m.Groups["name"].Value)).ToArray());
        }, null)
        select rm3);
    }
    #endregion

    public static RestMonad<User> GetMySelf(this RestMonad rest, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<User>> onError) {
      return AsyncHelpers.RunSync(() => RestMonad.Empty().GetMySelfAsync(onError));
    }
    public static async Task<RestMonad<User>> GetMySelfAsync(this RestMonad rest) {
      return await rest.GetMySelfAsync(null);
    }
    public static async Task<RestMonad<User>> GetMySelfAsync(this RestMonad rest, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<User>> onError) {
      return await rest.GetAsync<User>(MySelfPath(Tuple.Create("expand", "groups")), onError);
    }
    public static async Task<RestMonad<Field>> GetField(this RestMonad restMonad, params string[] filter) {
      return (await GetFieldsByKeyOrName(restMonad, true, filter)).Clone<RestMonad<Field>, Field>(rm => rm.SingleOrDefault());
    }
    public static async Task<RestMonad<Field[]>> GetFields(this RestMonad restMonad, params string[] filter) {
      return await GetFieldsByKeyOrName(restMonad, false, filter);
    }

    private static async Task<RestMonad<Field[]>> GetFieldsByKeyOrName(this RestMonad restMonad, bool isEquelFilter, params string[] filter) {
      return await restMonad.GetFieldsByKeyOrName(isEquelFilter, true, filter);
    }
    private static async Task<RestMonad<Field[]>> GetFieldsByKeyOrName(this RestMonad restMonad, bool isEquelFilter, bool throwIfMissing, params string[] filter) {
      var fields = await restMonad.GetArrayAsync<Field>(
        FieldPath(),
        isEquelFilter,
        field => field.FilterValues(),
        filter);
      var fields2 = (from field in fields.Value
                     join fi in JiraConfig.FieldsToIgnore on field.name.ToLower() equals fi.ToLower() into g
                     where g.IsEmpty()
                     select field
              ).ToArray();
      return fields.Clone(fields2);
    }

    public static async Task<RestMonad<SecurityLevel>> GetSecurityLevel(this RestMonad restMonad, int securityLevelId, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<SecurityLevel>> onError = null) {
      return await restMonad.GetAsync<SecurityLevel>(SecurityLevelPath(securityLevelId), onError);
    }
    public static async Task<RestMonad<IssueSecurityLevelsScheme>> GetIssueSecurityLevelScheme(this RestMonad restMonad, string projectKey, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<IssueSecurityLevelsScheme>> onError) {
      return await restMonad.GetAsync<IssueSecurityLevelsScheme>(IssueSecurityLevelSchemePath(projectKey), onError);
    }
    public static async Task<RestMonad<Jira.Json.SearchResult>> GetLastTicket(this RestMonad restMonad, bool isEquelFilter, params string[] filter) {
      return await restMonad.GetLastTicket(isEquelFilter, null, filter);
    }
    public static async Task<RestMonad<Jira.Json.SearchResult>> GetLastTicket(this RestMonad restMonad, bool isEquelFilter, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<SearchResult>> onError, params string[] filter) {
      var queryParams = new[] {
        Tuple.Create("jql", "order by created"),
        Tuple.Create("maxResults", "1") ,
        Tuple.Create("fields", "key,created") ,
        Tuple.Create("validateQuery", "true")
      };
      return await restMonad.GetAsync<SearchResult>(SearchPath(queryParams), onError);
    }
    public class JqlFilter {
      public static string ExactDateFilter(string fieldName, DateTime date) {
        var dateStart = date.ToJiraDate();
        var dateEnd = date.Date.AddDays(1).ToJiraDate();
        return "{0} >= {1} AND {0} <= {2}".Formatter(fieldName, dateStart, dateEnd);
      }
      public string[] Fields { get; set; }
      public int MaxResults { get; set; }
      private bool _expandTransitions = true;
      public bool ExpandTransitions {
        get { return _expandTransitions; }
        set { _expandTransitions = value; }
      }
      public bool ExpandChangelog { get; set; }
      public class JQL {
        public JQL(params object[] customFields) {
          customFields
            .Buffer(2)
            .Select(b => b.Counter(2).ToArray())
            .ForEach(b => CustomFields.Add(b[0] + "", b[1]));
        }
        public string Key { get; set; }
        public string Project { get; set; }
        public string IssueType { get; set; }
        public string Status { get; set; }
        public string Resolution { get; set; }
        public string Assignee { get; set; }
        public string Reporter { get; set; }

        class ExcludeAttribute :Attribute { }
        [Exclude]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();
        [Exclude]
        public IList<string> FieldsQuery { get; set; } = new List<string>();
        [Exclude]
        public IList<string> OrderBy { get; set; } = new List<string>();
        string OrderByString() {
          return (OrderBy == null || OrderBy.IsEmpty()) ? "" : " ORDER BY " + string.Join(" and ", OrderBy);
        }
        public override string ToString() {
          var cps = CustomFields.Select(cf => new { name = "\"" + cf.Key + "\"", value = cf.Value, delim = "~" });
          return string.Join(" and ",
            (from p in GetType().GetProperties()
             let v = p.GetValue(this)
             where !p.GetCustomAttributes(false).Any(a => a is ExcludeAttribute) && v != null && v.ToString() != ""
             select new { name = p.Name, value = v, delim = "=" })
             .Concat(cps)
             .Select(p => p.name + " " + p.delim + " \"" + p.value + "\"")
             .Concat(FieldsQuery ?? new List<string>()))
             + OrderByString();
        }
      }
      public JQL Jql { get; set; }

    }
    public static async Task<RestMonad<List<Issue>>> SearchIssueAsync(string key) {
      var jrqFilter = new Rest.JqlFilter { Jql = new Rest.JqlFilter.JQL() { Key = key }, MaxResults = 100 };
      var res = await JiraRest.Create(jrqFilter).GetTicketsAsync();
      return res.Value.issues.ToRestMonad(res);
    }
    public static async Task<RestMonad<SearchResult<Issue>>> GetTicketsAsync(this RestMonad<JqlFilter> restMonad, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<SearchResult<IssueClasses.Issue>>> onError = null) {
      return await restMonad.GetTicketsAsync<Issue>(onError);
    }
    public static async Task<RestMonad<SearchResult<T>>> GetTicketsAsync<T>(this RestMonad<JqlFilter> restMonad, T sample, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<SearchResult<T>>> onError = null) {
      return await restMonad.GetTicketsAsync<T>(onError);
    }
    public static async Task<RestMonad<SearchResult<TIssue>>> GetTicketsAsync<TIssue>(this RestMonad<JqlFilter> restMonad, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<SearchResult<TIssue>>> onError) {
      var filter = restMonad.Value;
      var queryParams = new[] {
        Tuple.Create("jql", filter.Jql.ToString()),
        Tuple.Create("validateQuery", "true") }.ToList();
      if(filter.MaxResults != 0)
        queryParams.Add(Tuple.Create("maxResults", filter.MaxResults + ""));
      filter.Fields.YieldNoNull()
        .ForEach(fields => queryParams.Add(Tuple.Create("fields", string.Join(",", fields))));
      var expand = new List<string>();
      if(filter.ExpandTransitions) expand.Add(IssueExpander.transitions + "");
      if(filter.ExpandChangelog) expand.Add(IssueExpander.changelog + "");
      if(expand.Any())
        queryParams.Add(Tuple.Create("expand", string.Join(",", expand)));
      return await restMonad.GetAsync<SearchResult<TIssue>>(SearchPath(queryParams.ToArray()), onError);
    }

    public static async Task<RestMonad<(string key, object value)[]>> GetUnresolvedTicketsWithCustomField(this RestMonad restMonad, string project, string issueType, string customFieldName) {
      var jrqFilter = new JqlFilter {
        MaxResults = 10000,
        ExpandTransitions = false,
        Jql = new JqlFilter.JQL {
          Project = project,
          IssueType = issueType,
          FieldsQuery = new[] {
            "Resolution = Unresolved"
          },
          OrderBy = new List<string> { "CREATED" }
        }
      };
      return await restMonad.GetTicketsWithCustomField(jrqFilter, customFieldName);
    }

    private static async Task<RestMonad<(string key, object value)[]>> GetTicketsWithCustomField(this RestMonad restMonad, JqlFilter jrqFilter, string customFieldName) {
      var customField = (await restMonad.GetField(customFieldName)).Value;
      Passager.ThrowIf(() => customField == null, new { customFieldName } + "");
      jrqFilter.Fields = new[] { customField.id };

      var res = await RestMonad.Create(jrqFilter, restMonad).GetTicketsAsync(new { key = "", id = "", fields = new Dictionary<string, object>(), transitions = new IssueTransitions.Transition[0] });
      return res.Switch(res.Value.issues.Select(i => (i.key, value: i.fields[customField.id])).ToArray());
    }

    public static async Task<IssueClasses.Issue[]> Search(this RestMonad rester, string project, string issueTypeName, string stateName, int maxResults, bool expandChangelog, string[] customFields) {
      return await rester.Search(project, issueTypeName, stateName, maxResults, expandChangelog, null, customFields);
    }
    public static async Task<IssueClasses.Issue[]> Search(this RestMonad rester, string project, string issueTypeName, string stateName, int maxResults, bool expandChangelog, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<SearchResult<IssueClasses.Issue[]>>> onError, string[] customFields) {
      var rm = rester.Copy(
      new JiraRest<Rest.JqlFilter>(new JqlFilter {
        MaxResults = maxResults,
        ExpandChangelog = expandChangelog,
        Jql = new Rest.JqlFilter.JQL {
          Project = project,
          IssueType = issueTypeName,
          Status = stateName,
        }
      }));
      Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<SearchResult<object>>> onObjectError = (exc, t) => {
        if(onError != null) {
          onError(exc, t);
          throw new InvalidOperationException("Proveded \"onError\" handler should have thrown an exception.");
        }
        throw exc.Value;
      };
      var rest = await rm.GetTicketsAsync<object>(onObjectError);
      var fields = await (new RestMonad()).GetFields(customFields);
      var search = rest.Value;
      var issues = (await Task.WhenAll(search.issues.Cast<JObject>()
        .Select(async jTicket => {
          var issue = jTicket.ToObject<IssueClasses.Issue>();
          await rest.Switch(issue).SetIssueCustomFields(jTicket, customFields);
          return issue;
        })));
      return issues
        .OrderBy(ticket => ticket.fields.updated)
        .ToArray();
    }

    public static async Task<IList<U>> SearchValue<T, U>(this RestMonad rm, string project, string issueType, string state, string field, Func<T, bool> valuePredicate, Func<IssueClasses.Issue, T, U> map, int maxResults) {
      return (await rm.SearchValue<T>(project, issueType, state, field, valuePredicate, maxResults))
        .Select(t => map(t.Item1, t.Item2))
        .ToArray();
    }
    public static async Task<IList<Tuple<IssueClasses.Issue, T>>> SearchValue<T>(this RestMonad rm, string project, string issueType, string state, string field, Func<T, bool> valuePredicate, int maxResults) {
      var search = await rm.Search(project, issueType, state, maxResults, false, new[] { field });
      return search.Select(issue => new { issue, value = issue.ExtractCustomField<T>(field).Single() })
        .Where(x => valuePredicate(x.value))
        .Select(x => Tuple.Create(x.issue, x.value))
        .ToArray();
    }
    public static async Task<RestMonad<SearchResult<Issue>>> FindInProgressSubTasks(string project, string assignee) {
      var jql = new JqlFilter {
        Jql = new JqlFilter.JQL {
          Project = project,
          Status = "In Progress",
          FieldsQuery = new[] {
            "issuetype in subTaskIssueTypes()",
            $"assignee in ({assignee})"
          }
        }
      };
      return await new JiraRest<JqlFilter>(jql).GetTicketsAsync();
    }

    public static async Task<IssueClasses.Issue[]> PauseOtherProgress(string project, string assignee, string currentTicket, string comment = "Paused by Worklogger") {
      var search = await Rest.FindInProgressSubTasks(project, assignee);
      var issues = search.Value.issues;
      var ticketsToPause = issues.Select(i => i.key).Where(key => key.ToUpper() != currentTicket.ToUpper()).ToArray();
      var pausedIssues =
        await (from issue in issues
               let key = issue.key
               where key != currentTicket.ToUpper()
               from rm in key.ToJiraTicket().PostIssueTransitionAsync("Stop Progress", IssueClasses.Issue.DoCode(comment), false, null)
               select issue
               );
      return pausedIssues.ToArray();
    }

    public static async Task IsJiraDev() {
      var jiraHost = (await RestMonad.Empty().GetAuthSession()).BaseAddress.Host.ToLower();
      Passager.ThrowIf(() => "usmpokwjird01" != jiraHost, new { jiraHost } + "");
    }

    public static async Task<RestMonad<AuthSession>> GetAuthSession(this RestMonad restMonad) {
      return await restMonad.GetAsync(s => s + authSessionPath, null, (rm, re) => {
        if(rm.Value.Response.StatusCode == HttpStatusCode.Unauthorized)
          return rm.Switch(new AuthSession());
        throw rm.Value;
      }, null);
    }
    public static async Task<RestMonad<AuthSession>> PostAuthSession(this RestMonad restMonad) {
      return await restMonad.PostAuthSession(JiraMonad.JiraPowerUser(), JiraMonad.JiraPowerPassword());
    }
    public static async Task<RestMonad<AuthSession>> PostAuthSession(this RestMonad restMonad, string userName, string password) {
      var user = new {
        username = userName,
        password = password
      };
      var session = await restMonad.PostAsync<Jira.Json.AuthSession>(() => authSessionPath, user);
      return session;
    }

    #endregion
  }
}
