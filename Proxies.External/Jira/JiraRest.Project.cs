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
using System.Diagnostics;
using HtmlAgilityPack;

namespace Jira {
  static partial class Rest {

    #region Screen scrapers

    #region Load Page
    private static string ProjectPluginPath(string projectKey, string operation) => $"plugins/servlet/project-config/{projectKey}/{operation}";
    static async Task<RestMonad<(HtmlAgilityPack.HtmlDocument doc, string pageAddress)>> LoadPlugin(this RestMonad rm, string projectKey, string operation) {
      var pageAddress = ProjectPluginPath(projectKey, operation);
      return await rm.LoadAdminPage(pageAddress);
    }
    private static async Task<RestMonad<(HtmlAgilityPack.HtmlDocument doc, string pageAddress)>> LoadAdminPage(this RestMonad rm, string pageAddress) {
      var html = await rm.GetStringAsync(pageAddress);
      var doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml(html.Value);
      return html.Switch((doc, pageAddress));
    }
    #endregion

    public static async Task<RestMonad<(int id, string name)>> GetProjectIssueTypeSchemeId(this RestMonad rm, string projectKey) {
      var docRM = await rm.LoadPlugin(projectKey, "issuetypes");
      var doc = docRM.Value.doc;
      var idValue = "project-config-issuetype-scheme-edit";
      var dataAttrName = "data-id";
      var xPath = $"//a[@id='{idValue}']";
      var id = (from a in doc.SelectNodes(xPath)
                select a.Attributes[dataAttrName].Value
               ).FirstOrDefault();
      if(id.IsNullOrWhiteSpace())
        throw new Exception(new { elementWith = new { id = idValue, dataAttrName }, docRM.Value.pageAddress, error = "Not Found", } + "");

      var classValue = "project-config-scheme-name";
      var name = (from e in doc.SelectNodes($"//*[@class='{classValue}']")
                  select e.InnerText
               ).FirstOrDefault();
      if(name.IsNullOrWhiteSpace())
        throw new Exception(new { elementWith = new { @class = classValue }, docRM.Value.pageAddress, error = "Not Found", } + "");

      return docRM.Switch((int.Parse(id), name));
    }
    public static async Task<RestMonad<(int id, int[] screenSchemeIds, int[] screenIds)>> GetProjectIssueTypeSceenScheme(this RestMonad rm, string projectKey) {
      var docRM = await rm.LoadPlugin(projectKey, "screens");
      var doc = docRM.Value.doc;
      var idValue = "project-config-screens-scheme-edit";
      var dataAttrName = "data-id";
      var isGeneric = doc.SelectNodes($"//*[contains(.,'Generic')]").TakeLast(1).Any(n => n.InnerText.Contains(": Generic"));
      if(isGeneric)
        return docRM.Switch((0, new int[0], new int[0]));
      var id = (from a in doc.SelectNodes($"//a[@id='{idValue}']")
                select a.Attributes[dataAttrName].Value
               ).FirstOrDefault();
      if(id.IsNullOrWhiteSpace())
        throw new Exception(new { elementWith = new { id = idValue, dataAttrName }, docRM.Value.pageAddress, error = "Not Found", } + "");

      var classValue = "project-config-screens-field-screen-scheme-id";
      var screenSchemeIds = (from a in doc.SelectNodes($"//input[@class='{classValue}']")
                             select a.Attributes["value"].Value
               ).ToArray();
      if(id.IsNullOrWhiteSpace())
        throw new Exception(new { elementWith = new { @class = classValue }, docRM.Value.pageAddress, error = "Not Found", } + "");

      var screenIds = GetScreenIds(doc);
      return docRM.Switch((int.Parse(id), screenSchemeIds.Select(sid => int.Parse(sid)).ToArray(), screenIds));
    }
    static int[] GetScreenIds(HtmlAgilityPack.HtmlDocument doc) {
      var hrefValue = "ConfigureFieldScreen.jspa?id=";
      var ids = (from a in doc.SelectNodes($"//a[contains(@href,'{hrefValue}')]")
                 select a.Attributes["href"].Value.Split('=').Last()
               ).ToArray();
      if(ids.IsEmpty())
        throw new Exception(new { elementWith = new { hrefValue }, error = "Not Found", } + "");

      return ids.Select(id => int.Parse(id)).Distinct().ToArray();
    }

    public static async Task<RestMonad<int[]>> GetScreensByProjectPrefix(this RestMonad rm, string projectKey, string classPrefix, bool throwOnEmpty = true) {
      var docRM = await rm.LoadAdminPage("/secure/admin/ViewFieldScreens.jspa");
      var doc = docRM.Value.doc;
      var classValue = classPrefix + "-fieldscreen";
      var screenIds = (from a in doc.SelectNodes($"//*[@class='{classValue}']")
                       where Regex.IsMatch(a.Attributes["id"].Value, $"{classPrefix}_fieldscreen_{projectKey}: ")
                       select int.Parse(a.Attributes["rel"].Value)
                       ).ToArray();
      if(throwOnEmpty && screenIds.IsEmpty())
        throw new Exception(new { elementWith = new { classValue, @in = docRM.Value.pageAddress }, error = "Not Found", } + "");

      return docRM.Switch(screenIds.ToArray());

    }

    //project-config-screens-scheme-edit
    #endregion


    public static async Task<IssueType> ProjectIssueTypeAddOrDelete(string projectKey, string issueTypeName, bool isDelete = false) {
      var rm = RestMonad.Empty();
      var x = (await (from p in RestMonad.Empty().GetProjectAsync(projectKey)
                      from its in p.GetIssueTypesAsync(issueTypeName)
                      from it in its.Value
                      select new { p = decimal.Parse(p.Value.id), it })
               ).Single();
      var schemeId = (await rm.GetProjectIssueTypeSchemeId(projectKey)).Value;
      var query = new Dictionary<string, string> {
        ["schemeId"] = schemeId.id + "",
        ["fieldId"] = "issuetype",
        ["projectId"] = x.p + "",
        ["name"] = schemeId.name,
        ["description"] = "",
        ["defaultOption"] = "",
        ["save"] = "Save"
      };
      var issueTypes = await (from its in GetIssueTypes()
                              from it in its
                              select it.id);
      var selectedOptions = new[] { x.it.id }.Concat(issueTypes)
        .Distinct()
        .Where(id => !isDelete || id != x.it.id)
        .Select(id => "selectedOptions=" + id)
        .Flatten("&");
      var html = await Core.PostFormAsync("/secure/admin/ConfigureOptionSchemes.jspa?" + selectedOptions, query);
      var projectIssueType = (await GetIssueTypes())
        .Where(it => it.name.ToLower() == issueTypeName.ToLower())
        .SingleOrDefault();
      if(!isDelete)
        Passager.ThrowIf(() => projectIssueType == null);
      else
        Passager.ThrowIf(() => projectIssueType != null);
      Trace.WriteLine(new { projectKey, issueTypeName });
      return x.it;
      // Locals
      Task<Jira.Json.IssueType[]> GetIssueTypes() => from p in rm.GetProjectAsync(projectKey) select p.Value.issueTypes;
    }

    #region Workflow IssueType
    public static async Task<string> ProjectWorkflowSchemeSetAsync(int projectId, int workflowSchemeId) {
      var values = new Dictionary<string, string> {
        { "projectId", projectId+ "" },
        { "draftMigration", "false" },
        { "projectIdsParameter", projectId + "" },
        { "schemeId", workflowSchemeId+"" },
        { "Associate", "Associate" }
      };
      var requestUri = "/secure/project/SelectProjectWorkflowSchemeStep2!default.jspa";
      var html  = await Core.PostFormAsync(requestUri, values);
      return html;
    }
    public static Task<RestMonad<Json.ProjectConfigResponse>> ProjectWorkflowShemeGetAsync(this RestMonad rm, string projectKey) =>
      rm.GetAsync(() => WorkflowScheme(projectKey) + "?_" + DateTime.Now.Ticks, Core.ReturnDebug<ProjectConfigResponse>, null, null);
    static async Task<RestMonad<Json.ProjectConfigResponse>> WorkflowIssueTypeDraftStart(this RestMonad rm, string projectKey) {
      return await rm.PostAsync<Json.ProjectConfigResponse>(() => WorkflowScheme(projectKey) + "?_" + DateTime.Now.Ticks, new { });
    }
    static async Task<RestMonad<ProjectConfigResponse>> WorkflowIssueTypeDraftMap(this RestMonad rm, string projectKey, string issueTypeName, string workflowName) {
      var issueTypeId = (await rm.GetIssueTypeAsync(issueTypeName)).Value.id;
      var request = new {
        name = workflowName,
        issueTypes = new[] { issueTypeId }
      };
      var r = await rm.PostAsync<Json.ProjectConfigResponse>(() => Rest.DraftWorkflowScheme(projectKey), request, true);
      return r;
    }

    static async Task<RestMonad<string>> WorkflowIssueTypeDraftSubmit(this RestMonad rm, string projectKey, int draftWorkflowSchemaId) {
      var project = await rm.GetProjectAsync(projectKey);
      var request = new Dictionary<string, string> {
        ["projectId"] = project.Value.id,
        ["schemeId"] = draftWorkflowSchemaId + "",
        ["draftMigration"] = "true",
        ["projectIdsParameter"] = project.Value.id + "",
        ["Associate"] = "Associate"
      };
      try {
        return project.Switch(await Core.PostFormAsync("secure/project/SelectProjectWorkflowSchemeStep2.jspa", request));
      } catch(Exception exc) {
        request.Add("ProjectKey", projectKey);
        throw new Exception(request.ToJson(false), exc);
      }
    }
    public static Task<RestMonad<string>> WorkflowIssueTypeAttach(this RestMonad rm, string projectKey, string issueType, string workflow) {
      return from it in rm.GetProjectAsync(projectKey)
             from d in it.WorkflowIssueTypeDraftStart(projectKey)
             from m in d.WorkflowIssueTypeDraftMap(projectKey, issueType, workflow)
             from s in m.WorkflowIssueTypeDraftSubmit(projectKey, d.Value.id)
             select s;
    }
    #endregion

    public static async Task ProjectIssueTypeScreenSchemeAdd(string projectKey, int issueTypeScreenSchemeId) {
      var rm = RestMonad.Empty();
      var projectId = (await RestMonad.Empty().GetProjectAsync(projectKey)).Value.id;
      var query = new Dictionary<string, string> {
        ["schemeId"] = issueTypeScreenSchemeId + "",
        ["projectId"] = projectId + "",
        ["Associate"] = "Associate"
      };
      var html = await Core.PostFormAsync("/secure/project/SelectIssueTypeScreenScheme.jspa", query);
      Trace.WriteLine(new { projectKey, issueTypeScreenSchemeId });
    }
    #region Field Configuration
    public static async Task ProjectFieldConfigurationSchemeAddAsync(string projectKey, int fieldSchemaId) {
      var rm = RestMonad.Empty();
      var projectId = (await RestMonad.Empty().GetProjectAsync(projectKey)).Value.id;
      var query = new Dictionary<string, string> {
        ["schemeId"] = fieldSchemaId + "",
        ["projectId"] = projectId + "",
        ["Associate"] = "Associate"
      };
      var html = await Core.PostFormAsync("/secure/admin/SelectFieldLayoutScheme.jspa", query);
      Trace.WriteLine(new { projectKey, fieldSchemaId });
    }
    public static async Task<int> FieldConfigurationCopyAsync(int schemeId, string newName) {
      var existingId = await FieldConfigurationIdAsync(newName);
      if(existingId.IsEmpty()) {
        var rm = RestMonad.Empty();
        var query = new Dictionary<string, string> {
          ["id"] = schemeId + "",
          ["fieldLayoutName"] = newName,
          ["Copy"] = "Copy"
        };
        await Core.PostFormAsync("/secure/admin/CopyFieldLayout.jspa", query);
        existingId = await FieldConfigurationIdAsync(newName);
        Trace.WriteLine(new { copied = new { schemeId, newName } });
      }
      return existingId.First();
    }
    public static async Task FieldConfigurationDeleteAsync(int configId) {
      var rm = RestMonad.Empty();
      var query = new Dictionary<string, string> {
        ["id"] = configId + "",
        ["confirm"] = "true",
        ["Delete"] = "Delete"
      };
      await Core.PostFormAsync("/secure/admin/DeleteFieldLayout.jspa", query);
      Trace.WriteLine(new { deleted = new { configId } });
    }
    public static async Task<int[]> FieldConfigurationIdAsync(string name) {
      var rm = RestMonad.Empty();
      var html = await Core.PostFormAsync("/secure//admin/ViewFieldLayouts.jspa");
      int[] fieldConfigId = ParseLinkId(html, name);
      return fieldConfigId ?? new int[0];
    }
    public static async Task<int[]> FieldConfigurationSchemeIdAsync(string name) {
      var rm = RestMonad.Empty();
      var html = await Core.PostFormAsync("/secure//admin/ViewFieldLayoutSchemes.jspa");
      int[] fieldConfigId = ParseLinkId(html, name);
      return fieldConfigId ?? new int[0];
    }
    public static async Task<int[]> FieldConfigurationSchemeAddAsync(string name) {
      var fieldSchemeId = await FieldConfigurationSchemeIdAsync(name);
      if(fieldSchemeId.IsEmpty()) {
        var rm = RestMonad.Empty();
        var query = new Dictionary<string, string> {
          ["inline"] = "true",
          ["decorator"] = "dialog",
          ["fieldLayoutSchemeName"] = name,
          ["fieldLayoutSchemeDescription"] = ""
        };
        await Core.PostFormAsync("/secure//admin/AddFieldConfigurationScheme.jspa", query);
        fieldSchemeId = await FieldConfigurationSchemeIdAsync(name);
      }
      return fieldSchemeId;
    }
    public static async Task FieldConfigurationSchemeDeleteAsync(int fieldSchemaId) {
      var rm = RestMonad.Empty();
      var query = new Dictionary<string, string> {
        ["id"] = fieldSchemaId + "",
        ["confirm"] = "true",
        ["Delete"] = "Delete"
      };
      await Core.PostFormAsync("/secure/admin/DeleteFieldLayoutScheme.jspa", query);
    }

    public static async Task FieldConfigurationSchemeAddConfigAsync(int schemaId, int configId) {
      var rm = RestMonad.Empty();
      var query = new Dictionary<string, string> {
        ["fieldConfigurationId"] = configId + "",
        ["id"] = schemaId + "",
        ["edited"] = "true",
        ["Update"] = "Update"
      };
      var html = await Core.PostFormAsync("/secure/project/EditFieldLayoutSchemeEntity.jspa", query);
      Trace.WriteLine(new { schemaId, configId });
    }

    private static int[] ParseLinkId(string html, string linkText) {
      var doc = new HtmlDocument();
      doc.LoadHtml(html);
      var searchPath = $"//a[contains(.,'{linkText}')]";
      var fieldConfigId = doc.DocumentNode.SelectNodes(searchPath)?
        .Where(a => a.InnerText.Trim() == linkText)
        .Take(1)
        .Select(a => a.Attributes["href"].Value.Split(';').TakeLast(1)
        .Select(q => q.Split('=').TakeLast(1))
        )
        .Concat()
        .Concat()
        .Select(int.Parse)
        .ToArray();
      return fieldConfigId;
    }
    #endregion

    static IEnumerable<HtmlNode> SelectNodes(this HtmlDocument doc, string xPath) =>
      doc.DocumentNode.SelectNodes(xPath) ?? new HtmlNode[0].AsEnumerable();
  }
}
