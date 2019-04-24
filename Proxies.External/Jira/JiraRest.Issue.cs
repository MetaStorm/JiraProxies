using Jira.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommonExtensions;
using Wcf.ProxyMonads;
using Jira;

namespace Jira {
  static partial class Rest {

    #region Delete Issue
    public static async Task<RestMonad> DeleteIssueAsync(this JiraTicket<string> ticket) {
      return await (
        from rm in ticket.DeleteAsync(() => IssueTicketPath(ticket.Value))
        from rm2 in rm.HandleExecutedAsync((response, json) => response, null, null)
        select rm2);
    }
    public static async Task<RestMonad> DeleteIssueTypeAsync(this RestMonad<IssueType> issueType, bool throwNotFound = true) {
      var issueTypeId = int.Parse(issueType.Value.id);
      return await (
        from rm in issueType.Switch(issueTypeId).DeleteAsync(() => IssueTypePath(issueTypeId))
        from rm2 in rm.HandleExecutedAsync((response, json) => response, null, (hrm, json) => {
          if (throwNotFound)
            throw new HttpResponseMessageException(hrm.Value, json, hrm.Value.RequestMessage.RequestUri + "",
              new { issueType = new { name = issueType.Value.name, id = issueType.Value.id } } + "", null);
          return hrm;
        })
        select rm2);
    }
    #endregion
    #region Get Issue
    public static async Task<RestMonad<IssueClasses.Issue>> GetIssueAsync(this RestMonad<IssueClasses.Issue> ticket) {
      return await ticket.Clone<JiraTicket<string>, string>(jt => jt.key).GetIssueAsync();
    }
    public static async Task<RestMonad<IssueClasses.Issue>> GetIssueAsync(this JiraTicket<string> ticket, string[] expand, string[] fields) {
      return await ticket.GetIssueAsync<IssueClasses.Issue, string, string>(expand, fields);
    }
    public static async Task<RestMonad<IssueClasses.Issue>> GetIssueAsync(this JiraTicket<string> ticket, IssueExpander[] expand, IssueFielder[] fields) {
      return await ticket.GetIssueAsync<IssueClasses.Issue>(expand, fields);
    }
    public static async Task<RestMonad<IssueClasses.Issue>> GetIssueAsync(this JiraTicket<string> ticket) {
      return await ticket.GetIssueAsync(new IssueExpander[] { IssueExpander.transitions }, new IssueFielder[0]);
    }
    public static async Task<RestMonad<IssueClasses.Issue>> GetIssueAsync(this JiraTicket<string> ticket, IssueFielder[] fields) {
      return await ticket.GetIssueAsync(new IssueExpander[] { IssueExpander.transitions }, fields);
    }
    public static async Task<RestMonad<IssueClasses.Issue>> GetIssueAsync(this JiraTicket<string> ticket, IssueExpander expand) {
      return await ticket.GetIssueAsync(new[] { expand });
    }
    public static async Task<RestMonad<IssueClasses.Issue>> GetIssueAsync(this JiraTicket<string> ticket, IssueExpander[] expand) {
      return await ticket.GetIssueAsync<IssueClasses.Issue>(expand.Concat(new[] { IssueExpander.transitions }).ToArray());
    }
    //public static async Task<RestMonad<IssueClasses.Issue>> GetIssueAsync(this JiraTicket<string> ticket,string workflowName) {
    //  return await ticket.GetIssueAsync<IssueClasses.Issue>(workflowName);
    //}
    static async Task<RestMonad<TIssue>> GetIssueAsync<TIssue>(this JiraTicket<string> ticket, IssueExpander[] expand) {
      return await ticket.GetIssueAsync<TIssue>(expand, new IssueFielder[0]);
    }
    public static async Task<RestMonad<TIssue>> GetIssueAsync<TIssue>(this JiraTicket<string> ticket, string[] expands, string[] fields) {
      return await ticket.GetIssueAsync<TIssue, string, string>(expands, fields);
    }
    static async Task<RestMonad<TIssue>> GetIssueAsync<TIssue>(this JiraTicket<string> ticket, IssueExpander[] expands, IssueFielder[] fields) {
      return await ticket.GetIssueAsync<TIssue, IssueExpander, IssueFielder>(expands, fields);
    }
    static string ParseQueryArray<T>(T[] enums, string name, params string[] concat) {
      return enums != null && enums.Any()
        ? name + "=" + string.Join(",", enums.Select(e => e.ToString()).Concat(concat.Where(s => !string.IsNullOrEmpty(s))).Distinct())
        : "";
    }
    static async Task<RestMonad<TIssue>> GetIssueAsync<TIssue, TExtender, TFielder>(
      this JiraTicket<string> ticket,
      TExtender[] expands,
      TFielder[] fields) {

      var fieldsQuery = ParseQueryArray(fields, "fields", Rest.IssueFielder.issuetype + "");
      var expandQuery = ParseQueryArray(expands, "expand", IssueExpander.transitions + "");
      var properties = "properties=*all";
      var query = string.Join("&", new[] { fieldsQuery, expandQuery, properties }.Where(s => !string.IsNullOrWhiteSpace(s)));
      var hasTransitions = expands.OfType<Jira.Rest.IssueExpander>().Where(enm => enm == IssueExpander.transitions);
      return await (await
        (await ticket.GetIssueAsync(t => IssueTicketPath(t) + (string.IsNullOrEmpty(query) ? "" : "?") + query))
        .HandleExecutedAsync(async (response, json) => {
          var j = response.Clone<RestMonad<TIssue>, TIssue>(JsonConvert.DeserializeObject<TIssue>(json));
          var issue = j.Value as IssueClasses.Issue;
          if (issue != null) {
            var jTicket = ((JObject)JsonConvert.DeserializeObject(json));
            await ticket.Switch(issue).SetIssueCustomFields(jTicket);
          }
          await hasTransitions.Select(_ =>
            issue.ToJiraTicket().FillIssueTransitionProperties()
          ).WhenAll();
          return j;
        }, null, null));
    }
    #endregion

    #region Post Issue
    public static async Task<RestMonad<IssueClasses.Issue>> PostIssueAsync(this RestMonad<JiraNewIssue> newIssuePost) {
      return await newIssuePost.PostIssueAsync(new Dictionary<string, object>());
    }
    #endregion

    #region Custom Fields
    public static async Task SetIssueCustomFields(this RestMonad<IssueClasses.Issue> issue, JObject jTicket, params string[] customFields) {
      Field<object>[] customFieldsResolved = await ResolveIssueCustomFields(issue, jTicket, customFields);
      SetIssueCustomFields(issue.Value, customFieldsResolved);
    }

    private static void SetIssueCustomFields(IssueClasses.Issue issue, Field<object>[] customFieldsResolved) {
      Func<string, bool> isNotSystemClassName = cn => !Regex.IsMatch(cn, @"cf\[\d+\]");
      var toDict = customFieldsResolved
        .SelectMany(cf => cf.field.clauseNames.Where(isNotSystemClassName).Select(key => new { key, cf }))
        .OrderBy(x => x.key)
        .ToArray();
      toDict.GroupBy(kv => kv.key).Where(g => g.Count() > 1)
        .ForEach(kv => {
          throw new Exception(new { field = kv.Key, value = kv.Select(x => x.cf.GetRawValue()).ToArray().ToJson() } + "");
        });
      issue.fields.custom = toDict.ToDictionary(x => x.key, x => new[] { x.cf.GetValueFromJira() });
    }
    #endregion

    #region Get Issue RelatedAsync
    public static async Task<RestMonad<byte[][]>> GetAttachment(this RestMonad<IssueClasses.Issue> issue, string fileName) {
      var x = await (from c in issue.Value.fields.attachment.Where(a => a.id == fileName || a.filename.ToLower() == fileName.ToLower()).Select(a => a.content)
                     from b in issue.GetBytesAsync(c)
                     select b.Value);
      return issue.Switch(x.ToArray());
    }
    public static async Task<RestMonad<HttpResponseMessage>> GetIssueRelatedAsync(this JiraTicket<string> ticket, Func<string, string, string> pathFactory) {
      return (await ticket.GetIssueAsync(t => pathFactory(t, ticket.ApiAddress)));
    }
    public static async Task<RestMonad<T>> GetIssueRelatedAsync<T>(this JiraTicket<string> ticket, Func<string, string, string> pathFactory) {
      return await
        (await ticket.GetIssueAsync(t => pathFactory(t, ticket.ApiAddress)))
        .HandleExecutedAsync((response, json) => response.Clone<RestMonad<T>, T>(JsonConvert.DeserializeObject<T>(json)), null, null);
    }
    #endregion

  }
}
