using System;
using System.Linq;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Jira;
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
using System.Collections.Concurrent;
using HtmlAgilityPack;

namespace Jira {
  static partial class Rest {
    static bool ContainsKey(this ConcurrentDictionary<StringIntKey, Workflow.Transition.Property[]> d, string workflowName, int propertyId) {
      return d.ContainsKey(new StringIntKey(workflowName, propertyId));
    }
    static Workflow.Transition.Property[] Get(this ConcurrentDictionary<StringIntKey, Workflow.Transition.Property[]> d, string workflowName, int propertyId) {
      return d[new StringIntKey(workflowName, propertyId)];
    }
    class StringIntKey {
      public string WorkFlow { get; set; }
      public int PropertyId { get; set; }
      public StringIntKey(string workFlow, int propertyId) {
        WorkFlow = workFlow;
        PropertyId = propertyId;
      }
      public static bool operator ==(StringIntKey c1, StringIntKey c2) {
        return string.Compare(c1.WorkFlow, c2.WorkFlow, StringComparison.OrdinalIgnoreCase) == 0 && c1.PropertyId == c2.PropertyId;
      }
      public static bool operator !=(StringIntKey c1, StringIntKey c2) {
        return string.Compare(c1.WorkFlow, c2.WorkFlow, StringComparison.OrdinalIgnoreCase) != 0 || c1.PropertyId != c2.PropertyId;
      }
      public override bool Equals(object obj) {
        return this == (StringIntKey)obj;
      }
      public override int GetHashCode() {
        return (WorkFlow + PropertyId).GetHashCode();
      }
    }
    #region Transition Properties
    static ConcurrentDictionary<StringIntKey, Workflow.Transition.Property[]> _transitionsPropertiesCache = new ConcurrentDictionary<StringIntKey, Workflow.Transition.Property[]>();
    public static void ClearTransitionsPropertiesCache() {
      _transitionsPropertiesCache.Clear();

    }
    public static async Task<Tuple<IssueTransitions.Transition, Workflow.Transition.Property[]>[]> GetTransitionsProperties(this RestMonad restMonad, string workflowName, IEnumerable<IssueTransitions.Transition> transitions) {
      return await transitions.Select(async t => Tuple.Create(t, (await restMonad.GetWorkflowTransitionProperties(workflowName, t.id)).Value)).WhenAll();
    }
    public static async Task<RestMonad<Workflow.Transition.Property[]>> GetWorkflowTransitionProperties(this RestMonad restMonad, string workflowName, int transitionId) {
      return await restMonad.GetAsync(WorkflowTransitionPropertiesPath(workflowName, transitionId), null, null, (rm, json) => {
        return rm.Switch(new Workflow.Transition.Property[0]);
      });
    }
    static async Task<IEnumerable<Tuple<IssueTransitions.Transition, bool>>> FillIssueTransitionPropertiesFromCache(this IEnumerable<IssueTransitions.Transition> transitions, Lazy<Task<string>> workflowName) {
      return await (transitions.Select(trans => trans.FillIssueTransitionPropertiesFromCache(workflowName)).WhenAll());
    }
    static async Task<Tuple<IssueTransitions.Transition, bool>> FillIssueTransitionPropertiesFromCache(this IssueTransitions.Transition transition, Lazy<Task<string>> workflowName) {
      if(false && _transitionsPropertiesCache.ContainsKey(await workflowName.Value, transition.SafeId())) {
        //transition.Properties = _transitionsPropertiesCache.Get(await workflowName.Value, transition.SafeId());
        return Tuple.Create(transition, true);
      }
      return Tuple.Create(transition, false);
    }
    public static async Task<IList<IssueTransitions.Transition>> FillIssueTransitionProperties(this RestMonad<IssueClasses.Issue> restMonad) {
      await restMonad.FillIssueTransitionProperties(restMonad.Value.transitions);
      return restMonad.Value.transitions;
    }

    public static async Task<string> IssueWorkflow(this IssueClasses.Issue issue) {
      var projectsFromKey = issue.key?.Split('-').Take(1) ?? new string[0];
      var projectKey = issue.fields?.project?.key ?? projectsFromKey.SingleOrDefault();
      projectsFromKey.ForEach(projectFromKey => Passager.ThrowIf(() => projectFromKey != projectKey, "{0}", new { projectFromKey, projectKey }));
      Passager.ThrowIf(() => projectKey.IsNullOrWhiteSpace());

      var issueType = issue.fields?.issuetype?.name;
      Passager.ThrowIf(() => issueType.IsNullOrWhiteSpace());

      return await RestConfiger.ProjectIssueTypeWorkflow(projectKey, issueType);
    }
    static Lazy<T> LazyMe<T>(Func<T> func) { return new Lazy<T>(func); }
    public static async Task FillIssueTransitionProperties(this RestMonad<IssueClasses.Issue> rest, IEnumerable<IssueTransitions.Transition> transitions) {
      var workflowName = LazyMe(() => rest.Value.IssueWorkflow());
      var transEmpty = await transitions.FillIssueTransitionPropertiesFromCache(workflowName);
      if(transEmpty.Any(x => !x.Item2)) {
        //var workflows = (await rest.GetWorkflows(null)).Value;
        await transEmpty
          //.AsParallel()
          .Select(trans => rest.FillIssueTransitionProperties(workflowName, trans.Item1))
          .ToArray()
          .WhenAll();
      }
    }

    static async Task<IssueTransitions.Transition> FillIssueTransitionProperties(this RestMonad restMonad, Lazy<Task<string>> workflowName, IssueTransitions.Transition transition) {
      var xTrans = await transition.FillIssueTransitionPropertiesFromCache(workflowName);
      if(xTrans.Item2) {
        return transition;
      }
      transition.PropertiesGetter = LazyMe(async ()
        => (await restMonad.GetAsync<Workflow.Transition.Property[]>(WorkflowTransitionPropertiesPath(await workflowName.Value, transition.SafeId()), null)).Value);
      return transition;
      /*
      var props = await restMonad.GetAsync<Workflow.Transition.Property[]>(WorkflowTransitionPropertiesPath(await workflowName.Value, transition.SafeId()), null);
      if (props.Value.Any()) {
        transition.Properties = props.Value;
        _transitionsPropertiesCache.TryAdd(new StringIntKey(await workflowName.Value, transition.SafeId()), props.Value);
      }
      return transition;
      */
    }
    public static async Task<RestMonad<string[]>> GetWorlflowSchemeIdsAsync(string jiraHost, string jiraUser, string jiraPassword) {
      return await new RestMonad(jiraHost, "", jiraUser, jiraPassword).GetWorlflowSchemeIdsAsync();
    }
    public static async Task<RestMonad<string[]>> GetWorlflowSchemeIdsAsync(this RestMonad restMonad) {
      return await restMonad.GetWorlflowSchemeIdsAsync("secure/admin/ViewWorkflowSchemes.jspa");
    }
    public static async Task<RestMonad<string[]>> GetWorlflowSchemeIdsAsync(this RestMonad restMonad, string xPath) {
      var s = (await restMonad.GetStringAsync(xPath)).Value;
      var doc = new HtmlDocument();
      doc.LoadHtml(s);
      var searchPath = "//tr/td/ul/li/a[contains(.,'Edit') and contains(@href,'EditWorkflow')]";
      var edits = doc.DocumentNode.SelectNodes(searchPath);
      if(edits == null) {
        var error = doc.DocumentNode.SelectSingleNode("html/head/title")?.InnerText ?? "not found";
        throw new Exception(new { searchPath, restMonad, error } + "");
      }
      return edits.Select(link => link.Attributes["href"].Value.Split('?')[1].Split('=')[1]).ToArray().ToRestMonad();
    }
    public static async Task<RestMonad<Workflow[]>> GetWorkflows(this RestMonad restMonad, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<Workflow[]>> onError = null) {
      return await restMonad.GetAsync<Workflow[]>(
        WorkflowPath(), onError);
    }
    public static async Task<RestMonad<WorkflowSchemaWorkflow[][]>> GetWorkflowShemeWorkflowsAsync(this RestMonad restMonad, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<WorkflowSchemaWorkflow[]>> onError = null) {
      //var wsList = new List<WorkflowSchemaWorkflow[]>();
      var wfs = (await RestConfiger.WorkflowSchemaIds.Select(wsId => restMonad.GetWorkflowShemeWorkflowAsync(wsId, onError))
       .WhenAll())
       .Select(rm => rm.Value)
       .ToArray();
      return wfs.ToRestMonad(restMonad);

      //foreach (var wsId in RestConfiger.WorkflowSchemaIds)
      //  wsList.Add((await restMonad.GetWorkflowShemeWorkflowAsync(wsId, onError)).Value);
      //return  wsList.ToArray().ToRestMonad(restMonad);
    }
    public static async Task<RestMonad<WorkflowSchemaWorkflow[]>> GetWorkflowShemeWorkflowAsync(this RestMonad restMonad, int workflowSchemaId, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<WorkflowSchemaWorkflow[]>> onError = null) {
      return await restMonad.GetAsync(WorkflowSchemaWorkflowsPath(workflowSchemaId), (rme, ret) => {
        if(onError != null) return onError(rme, ret);
        throw new Exception(new { workflowSchemaId, rme } + "", rme.Value);
      });
    }
    public static async Task<RestMonad<IssueClasses.Status[]>> GetStatuses(this RestMonad restMonad, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<IssueClasses.Status[]>> onError = null) {
      return await restMonad.GetAsync(StatusesPath(), onError);
    }
    //public static async Task<RestMonad<IssueType[]>> GetIssueTypesAsync(this RestMonad restMonad, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<IssueType[]>> onError = null) {
    //  return await restMonad.GetAsync(IssueTypePath(), onError);
    //}
    public static async Task<IssueType[]> GetIssueTypesAsync(params string[] issueTypeFilter) {
      return (await RestMonad.Empty().GetIssueTypesAsync(issueTypeFilter)).Value;
    }
    public static async Task<RestMonad<IssueType[]>> GetIssueTypesAsync(this RestMonad rest, params string[] issueTypeFilter) {
      return await rest.GetArrayAsync<IssueType>(
        IssueTypePath, true, it => it.name, issueTypeFilter);
    }
    public static async Task<RestMonad<IssueType>> GetIssueTypeAsync(this RestMonad rest, string issueTypeFilter) {
      var x = await from rm in rest.GetIssueTypesAsync(issueTypeFilter)
                    from it in rm.Value
                    select rm.Switch(it);
      return x.SingleOrDefault();
    }
    public static async Task<RestMonad<IssueType>> GetOrPostIssueType(this RestMonad rm, string issueType, string description) {
      var it = await rm.GetIssueTypeAsync(issueType);
      if(it != null) return it;
      return await rm.PostIssueTypesAsync(issueType, description, async (exc, eit) => {
        if(exc.Value.Response.StatusCode == HttpStatusCode.Conflict) {
          return await (exc.GetIssueTypeAsync(issueType));
        }
        ExceptionDispatchInfo.Capture(exc.Value).Throw();
        throw exc.Value;
      });
    }
    public static async Task<RestMonad<IssueType>> PostIssueTypesAsync(this RestMonad restMonad
      , string name, string description
      , Func<RestMonad<HttpResponseMessageException>, RestErrorType, Task<RestMonad<IssueType>>> onError = null) {
      return await await await from h in restMonad.PostAsync(IssueTypePath, new { name, description, type = "standard" }, null, false)
                               select h.HandleExecutedAsync((r, j) => Task.FromResult(Jira.Core.Return<IssueType>(r, j)), onError, null);
    }
    public static async Task<RestMonad<CreateIssueMeta.Issuetype>> GetIssueCreateFields(this RestMonad restMonad, string project, string issueType, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<CreateIssueMeta.Issuetype>> onError = null) {
      return await restMonad.GetAsync(
        IssueCreateMetaWithFieldsPath(project, issueType), (response, json) => {
          var j = JsonConvert.DeserializeObject<CreateIssueMeta>(json);
          var issue = j.projects
            .Counter(1, _ => { throw new Exception($"No metadata found for [{new { project }}]"); }, null)
            .Single()
            .issuetypes
            .Counter(1, _ => { throw new Exception($"No metadata found for [{new { project, issueType }}]"); }, null)
            .Single();
          var jMeta = ((JObject)JsonConvert.DeserializeObject(json));
          var customFields0 = from jt in jMeta["projects"][0]["issuetypes"][0]["fields"].OfType<JProperty>()
                              where jt.Name.StartsWith("customfield_")
                              select (JObject)jt.Value;
          var customFields = customFields0.Select(jv => jv.ToObject<CreateIssueMeta.FieldDescription>());
          issue.fields.customFields.AddRange(customFields);
          return response.Clone<RestMonad<CreateIssueMeta.Issuetype>, CreateIssueMeta.Issuetype>(issue);
        }, onError, null);
    }
    public static async Task<RestMonad<CreateIssueMeta>> GetIssueCreateMeta(this RestMonad restMonad, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<CreateIssueMeta>> onError = null) {
      return await restMonad.GetAsync(IssueCreateMetaPath(), onError);
    }
    public static async Task<RestMonad<(string p, string sl)[]>> GetSecurityLevels(this RestMonad rm) {
      var icm = await rm.GetIssueCreateMeta();
      return icm.Switch((from p in icm.Value.projects
                         where p.issuetypes != null
                         from it in p.issuetypes
                         where it.fields.security != null
                         from sl in it.fields.security.allowedValues
                         select (p: p.key, sl: sl.name)
               ).ToArray());
    }

    #endregion

    #region Get Next Transition
    public static async Task<IssueTransitions.Transition> GetIssueTransitionNextAsync(string ticket) {
      var transitions = (await GetIssueTransitionsAsync(ticket)).transitions;
      return ProcessNextTransition(ticket, transitions);
    }
    public static async Task<IssueTransitions.Transition> GetIssueTransitionNextAsync(this JiraTicket<string> ticket) {
      var transitions = (await GetIssueTransitionsAsync(ticket)).transitions;
      return ProcessNextTransition(ticket, transitions);
    }

    private static IssueTransitions.Transition ProcessNextTransition<TTicket>(TTicket ticket, List<IssueTransitions.Transition> transitions) {
      if(transitions.Count == 1) return transitions.Single();
      var nextNames = new[] { "next", "done", "yes" };
      var nextTransitions = transitions.Where(t => nextNames.Contains(t.name.ToLower())).ToList();
      if(nextTransitions.Count > 1)
        throw new Exception(new { ticket, error = "{Next} operation requires a single availible transition [" + nextNames.ToJson(false) + "]", nextTransitions }.ToJson());
      if(transitions.Count == 0 || !nextTransitions.Any())
        throw new Exception(new { ticket, error = "{Next} operation requires at least one availible transition [" + nextNames.ToJson(false) + "]" }.ToJson());
      return nextTransitions.Single();
    }

    /// <summary>
    /// Get current status' transition or empty array
    /// </summary>
    /// <param name="issue"></param>
    /// <param name="transitionOrPropertyName"></param>
    /// <param name="throwNotFound">Run error action if provided</param>
    /// <returns></returns>
    public static IList<IssueTransitions.Transition> FindTransitionByNameOrProperty(this IssueClasses.Issue issue, string transitionOrPropertyName, bool throwNotFound) {
      var message = new { ticket = issue.key, status = issue.fields.status.name, transitionOrPropertyName, message = "Not found" } + "";
      var axc = new List<Exception>();
      try {
        return issue.transitions.Where(t => t.name.ToLower() == transitionOrPropertyName.ToLower()).Counter(1).ToArray();
      } catch(Exception exc) {
        try {
          return issue.FindTransitionByProperty(transitionOrPropertyName).Counter(1, _ => { throw new TransitionsException("No 'next' transition found by property"); }, c => { throw new TooManyTransitionsException(issue); }).Select(t => t.Item1).ToArray();
        } catch(TooManyTransitionsException) {
          throw;
        } catch(TransitionsException exc2) {
          if(throwNotFound)
            throw new AggregatedException(exc2, exc);
          return new IssueTransitions.Transition[0];
        }
      }
    }
    class TransitionsException :Exception { public TransitionsException(string message) : base(message) { } }
    class TooManyTransitionsException :TransitionsException {
      public TooManyTransitionsException(IssueClasses.Issue issue) : base(new { error = "More then one 'next' transition found by property", issue } + "") { }
    }
    public static IList<IssueTransitions.Transition> ErrorTransition(this IssueClasses.Issue issue, bool throwNotFound = true) {
      return issue.FindTransitionByNameOrProperty("error", throwNotFound);
    }
    public static IList<IssueTransitions.Transition> NextTransition(this IssueClasses.Issue issue, bool throwNotFound = true) {
      return issue.FindTransitionByNameOrProperty("next", throwNotFound);
    }
    public static IList<IssueTransitions.Transition> BackTransition(this IssueClasses.Issue issue, bool throwNotFound = true) {
      return issue.FindTransitionByNameOrProperty("back", throwNotFound);
    }
    public static IList<IssueTransitions.Transition> YesTransition(this IssueClasses.Issue issue, bool throwNotFound = true) {
      return issue.FindTransitionByNameOrProperty("yes", throwNotFound);
    }
    public static IList<IssueTransitions.Transition> NoTransition(this IssueClasses.Issue issue, bool throwNotFound = true) {
      return issue.FindTransitionByNameOrProperty("no", throwNotFound);
    }
    public static IList<IssueTransitions.Transition> FastForwardTransitions(this IssueClasses.Issue issue, bool throwNotFound = true) {
      return issue.FindTransitionByNameOrProperty("fastforward", throwNotFound);
    }

    private static IssueTransitions.Transition GetTransitionByPropertyName(this IssueClasses.Issue issue, string propertyName, Action<TransitionPropertyNotFound> error = null) {
      return issue.TransitionByProperty(propertyName, "true", true, error);
    }

    public static IssueTransitions.Transition SmsTransition(this IssueClasses.Issue issue, SmsValue smsValue, bool throwNotFound) {
      return issue.SmsTransition(smsValue + "", throwNotFound);
    }
    public static IssueTransitions.Transition SmsTransition(this IssueClasses.Issue issue, string smsValue, bool throwNotFound) {
      return issue.TransitionByProperty("sms", smsValue, throwNotFound);
    }
    public static IEnumerable<IssueTransitions.Transition> SmsValueTransitions(this IssueClasses.Issue issue, string key, bool throwNotFound, Action<TransitionPropertyNotFound> error) {
      return issue.TransitionsByProperty(key, "sms", throwNotFound, error);
    }

    public static IssueTransitions.Transition TransitionByProperty(this IssueClasses.Issue issue, string propertyName, string propertyValue, bool throwNotFound) {
      return issue.TransitionByProperty(propertyName, propertyValue, throwNotFound, null);
    }
    public static IEnumerable<IssueTransitions.Transition> TransitionsByProperty(this IssueClasses.Issue issue, string propertyName, string propertyValue, bool throwNotFound, Action<TransitionPropertyNotFound> error) {
      Action<int> noNextTransition = _ => {
        var exc = new TransitionPropertyNotFound(issue.key, propertyName, propertyValue);
        if(error != null)
          error(exc);
        else
          throw exc;
      };
      var finder = issue.FindTransitionByProperty(propertyName, propertyValue).Select(x => x.Item1);
      return !throwNotFound
        ? finder
        : finder
        .Counter(1, noNextTransition, null);
    }

    public static IssueTransitions.Transition TransitionByProperty(this IssueClasses.Issue issue, string propertyName, string propertyValue, bool throwNotFound, Action<TransitionPropertyNotFound> error) {
      Action<int> noNextTransition = _ => {
        var exc = new TransitionPropertyNotFound(issue.key, propertyName, propertyValue);
        if(error != null)
          error(exc);
        else
          throw exc;
      };
      var finder = issue.FindTransitionByProperty(propertyName, propertyValue).Select(x => x.Item1);
      return !throwNotFound
        ? finder.SingleOrDefault()
        : finder
        .Counter(1, noNextTransition, null)
        .SingleOrDefault();
    }

    private static IEnumerable<Tuple<IssueTransitions.Transition, Workflow.Transition.Property>> FindTransitionByProperty(this IssueClasses.Issue issue, string name) {
      return issue.FindTransitionByProperty(name, null);
    }
    private static IEnumerable<Tuple<IssueTransitions.Transition, Workflow.Transition.Property>> FindTransitionByProperty(this IssueClasses.Issue issue, string name, string value) {
      return issue.transitions == null
        ? new Tuple<IssueTransitions.Transition, Workflow.Transition.Property>[0]
        : issue.transitions
        .SelectMany(t => t.Properties.Select(p => new { t, p, ok = p.Find(name, value) }))
        .Where(x => x.ok)
        .Select(x => Tuple.Create(x.t, x.p));
    }
    private static IEnumerable<Workflow.Transition.Property> FindTransitionProperty(this IssueClasses.Issue issue, string propertyName) {
      return issue.transitions == null
        ? new Workflow.Transition.Property[0]
        : issue.FindTransitionByProperty(propertyName).Select(x => x.Item2);
    }
    public static IEnumerable<Workflow.Transition.Property> FindProperty(this IssueTransitions.Transition t, string name) {
      return t.FindProperty(name, null);
    }
    public static IEnumerable<Workflow.Transition.Property> FindProperty(this IssueTransitions.Transition t, string name, string value) {
      return t.Properties.Where(p => p.Find(name, value));
    }

    private static bool Find(this Workflow.Transition.Property p, string name) {
      return p.Find(name, null);
    }
    private static bool Find(this Workflow.Transition.Property p, string name, string value) {
      return p.key.ToLower() == name.ToLower()
                        && (value == null || p.value.ToLower() == value.ToLower());
    }

    #endregion

    #region Get Issue Transitions
    public static async Task<IssueTransitions> GetIssueTransitionsAsync(string ticket) {
      return await ticket.ToJiraTicket().GetIssueTransitionsAsync();
    }
    public static async Task<IssueTransitions> GetIssueTransitionsAsync(this JiraTicket<string> ticket) {
      return await
        (await ticket.GetIssueAsync(IssueTransitionsPath))
        .HandleExecutedAsync((response, json) => JsonConvert.DeserializeObject<IssueTransitions>(json), null, null);
    }
    #endregion
    public class TransitionPropertyNotFound :Exception {
      public TransitionPropertyNotFound(string ticket, string key, string value)
        : base(new { ticket, transition = new { property = new { key, value } }, message = "Not Found" } + "") {
      }
    }
  }
}
