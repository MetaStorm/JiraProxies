using CommonExtensions;
using Jira.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wcf.ProxyMonads;

namespace Jira {
  static partial class Rest {
    static async Task<IEnumerable<Field<object>>> ResolveCustomFieldsValues(this RestMonad<JiraNewIssue> newIssuePost, IDictionary<string, object> customProperties) {
      var valueProjector = CommonExtensions.Helpers.ToFunc("", "", (id, name) => new { id, name });
      var nameProjector = CommonExtensions.Helpers.ToFunc("", "", new[] { valueProjector("", "") }, (id, name, values) => new { id, name, values });
      var issueCustomFields = (await (
        string.IsNullOrWhiteSpace(newIssuePost.Value.key)
          ? newIssuePost.GetIssueCustomFields(valueProjector, nameProjector)
          : newIssuePost.Value.key.ToJiraTicket(newIssuePost).GetIssueCustomFields(valueProjector, nameProjector)
        )).Value;
      var valueFields = new[] { "radiobuttons", "cascadingselect", "select" };
      return (
        from cp in (await ResolveCustomFields(newIssuePost, customProperties)).Value
        from issueJson in new[] { (Func<string>)(() => newIssuePost.Value.fields.project.key + " : " + newIssuePost.Value.fields.issuetype.name) }
        from customFields in new[] { issueCustomFields }
        let customField = cp.field.schema.jiraType == "multicheckboxes" ? null : customFields.SingleOrDefault(cf => !valueFields.Contains(cp.field.schema.jiraType) && Core.FilterCompareAny(new[] { cf.name, cf.id }, cp.field.id))
        let value = customField == null ? null : customField.values
          .Where(cfv => Core.FilterCompareAll(new[] { cfv.name, cfv.id }, cp.GetRawValue()))
          .ThrowIfEmpty(new Exception(new { CustomField = new { cp.field.name, cp.field.id }, WithValue = cp.GetRawValue().ToJson(), IsNotAllowedInIssue = issueJson() } + ""))
        select value == null ? cp : Field.Create(cp, (object)value.Select(v => v.id).ToArray())
      );
    }
    public static async Task<JiraNewIssue> ResolveSecurityLevel(this RestMonad<JiraNewIssue> restMonad) {
      var fields = restMonad.Value.fields.YieldNoNull().FirstOrDefault();
      Func<string, bool> hasSecLevId = id => {
        var secLevId = 0;
        return int.TryParse(id, out secLevId) && secLevId > 0;
      };
      if (fields.security != null && !hasSecLevId(fields.security.id) && !string.IsNullOrWhiteSpace(fields.security.name)) {
        var projectKey = fields.project.YieldNoNull(p => p.key).FirstOrDefault();
        if (projectKey == null)
          throw new MissingFieldException("Project key is missing in " + fields.ToJson());
        var secCheme = (await restMonad.GetIssueSecurityLevelScheme(projectKey, null)).Value;
        var secLev = secCheme.levels.First(sl => sl.name.ToLower() == fields.security.name.ToLower());
        fields.security.id = secLev.id;
      }
      return restMonad.Value;
    }
    /// <summary>
    /// Converts fieldName/fieldValue pairs to fieldId/vieldValue pairs
    /// To be used in writing custom fields to JIRA ticket
    /// </summary>
    /// <param name="restMonad"></param>
    /// <param name="customProperties">fieldName/fieldValue pairs</param>
    /// <param name="throwMissingException"></param>
    /// <returns></returns>
    public static async Task<RestMonad<Field<object>[]>> ResolveCustomFields(this RestMonad restMonad, IDictionary<string, object> customProperties, bool throwMissingException = true) {
      customProperties = customProperties ?? new Dictionary<string, object>();
      Action<int, string> throwDuplicatedFieldName = (i, field) => { throw new Exception(new { field, message = "Exists " + i + " times" } + ""); };
      var fields = (await restMonad.GetFieldsByKeyOrName(true, customProperties.Keys.ToArray())).Value;
      var x = customProperties.Select(cp
        => new { cp, field = fields.Where(f => Core.FilterCompareAny(f.FilterValues(), cp.Key)).Counter(1, _ => { }, i => throwDuplicatedFieldName(i, cp.Key)).DefaultIfEmpty().ToArray().SingleOrDefault() });
      var missingExceptions = x.Where(_ => throwMissingException).Where(y => y.field == null).Select(y => new MissingFieldException(new { customField = y.cp.Key, error = "Missing" } + "")).ToArray();
      if (missingExceptions.Any())
        throw new AggregatedException(missingExceptions);
      return x.Select(y => Field.Create(y.field, y.cp.Value)).ToArray().ToRestMonad();
      //var results = (from cp in customProperties
      //               from field in fields
      //               let ok = Core.FilterCompareAny(field.FilterValues(), cp.Key)
      //               group new { cp, field, ok } by cp.Key into gcp
      //               select new { gcp.Key, field = gcp.Where(g => g.ok).Select(g => new { g.field, g.cp }).FirstOrDefault() } into x2
      //               //from x in (throwMissingException ? gcp.ThrowIfEmpty(new MissingFieldException(new { CustomField = gcp.Key } + "")) : gcp).Take(1)
      //               //join field2 in fields on cp.Key.ToLower() equals field2.name.ToLower() into gcp
      //               //from jcp in throwMissingException ? gcp.ThrowIfEmpty(new MissingFieldException(new { CustomField = cp.Key } + "")) : gcp
      //               select new { x2.Key, field = x2.field == null ? null : Field.Create(x2.field.field, x2.field.cp.Value) }
      //        ).ToArray();
      ////.ToRestMonad(restMonad);
      //return results.Select(x => x.field).ToArray().ToRestMonad();
    }
    public static async Task<Field<object>[]> ResolveIssueCustomFields(RestMonad rest, JObject jTicket, params string[] fieldNames) {
      var customFields0 = from jt in jTicket.With("fields").OfType<JProperty>()
                          where jt.Name.StartsWith("customfield_")
                          select jt;
      var customFields = customFields0.ToDictionary(jp => jp.Name, jp => jp.Value.Value<object>());
      var customFieldsResolved = (await rest.UnResolveCustomFields(customFields, false)).Value.Where(cf => cf.field.name != "Duplicate").ToArray();
      if (fieldNames.Any())
        customFieldsResolved = (from cf in customFieldsResolved
                                join fn in fieldNames on cf.field.name.ToLower() equals fn.ToLower()
                                select cf).ToArray();
      var duplicatedFields = customFieldsResolved.GroupBy(cfr => cfr.field.name)
      .Where(g => g.Count() > 1)
      .Select(g => g.Key)
      .OrderBy(s => s)
      .ToArray();
      Passager.ThrowIf(() => duplicatedFields.Any(), ". Field Name:{0}", duplicatedFields.Flatten());
      //issue.fields.custom = customFieldsResolved.ToDictionary(cf => cf.field.clauseNames.Last(), cf => new[] { cf.GetJiraValue() });
      return customFieldsResolved;
    }

    /// <summary>
    /// Converts fieldId/fieldValue pairs to fieldName/vieldValue pairs
    /// To be used in writing custom fields to JIRA ticket
    /// </summary>
    /// <param name="restMonad"></param>
    /// <param name="customProperties">fieldName/fieldValue pairs</param>
    /// <param name="throwMissingException">fieldName/fieldValue pairs</param>
    /// <returns></returns>
    public static async Task<RestMonad<Field<object>[]>> UnResolveCustomFields(this RestMonad restMonad, IDictionary<string, object> customProperties, bool throwMissingException = true) {
      Func<object, object> valuer = v => v as JValue != null ? ((JValue)v).Value : (object)v;
      customProperties = customProperties ?? new Dictionary<string, object>();
      var fields = (await restMonad.GetFieldsByKeyOrName(true, customProperties.Keys.ToArray())).Value;
      return (from cp in customProperties
              join field in fields on cp.Key.ToLower() equals field.id.ToLower() into gcp
              from jcp in throwMissingException ? gcp.ThrowIfEmpty(new MissingFieldException(new { CustomField = cp.Key } + "")) : gcp
              select Field.Create(jcp, valuer(cp.Value))
              ).ToArray().ToRestMonad(restMonad);
    }

  }
}
