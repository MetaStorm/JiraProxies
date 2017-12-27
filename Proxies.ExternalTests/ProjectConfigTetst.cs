using CommonExtensions;
using Jira.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Wcf.ProxyMonads;

namespace Jira.Tests {
  [TestClass()]
  public class ProjectConfigTests {
    [TestMethod]
    public async Task PostIssueType() {
      var rm = RestMonad.Empty();
      var issueType = "ZZZ Issue Type " + CommonExtensions.Helpers.RandomStringUpper(4);
      var oldITs = (await rm.GetIssueTypesAsync(issueType)).Value;
      await oldITs.Select(oit => oit.ToRestMonad().DeleteIssueTypeAsync()).WhenAll();
      var it = (await RestMonad.Empty().PostIssueTypesAsync(issueType, "Delete Me")).Value;
      Console.WriteLine(it.ToJson());
      Assert.AreEqual(issueType, it.name);

      var projectKey = "IM";
      await Rest.ProjectIssueTypeAddOrDelete(projectKey, it.name);
      Assert.IsTrue((await rm.GetProjectAsync(projectKey)).Value.issueTypes.Any(nit => nit.name == issueType));
      await Rest.ProjectIssueTypeAddOrDelete(projectKey, it.name, true);

      var oldIT = (await rm.GetOrPostIssueType(issueType, "Delete Me")).Value;
      Assert.AreEqual(issueType, oldIT.name);

      await it.ToRestMonad().DeleteIssueTypeAsync();
      await it.ToRestMonad().DeleteIssueTypeAsync(false);
      try {
        await it.ToRestMonad().DeleteIssueTypeAsync();
      } catch (HttpResponseMessageException exc) {
        Assert.IsTrue(exc.ToMessages().Contains(issueType));
        return;
      }
      Assert.Fail("Should not be here.");
      //Console.WriteLine(pitw.Select(l=>new { project = l.Key, issue = l.Select(l1 => l1.Select(l2 => new { l2.Key, Value = l2.ToArray() })).ToArray() } ).ToJson());
    }

    public async Task ProjectIssueTypeAddDelete() {
      var issueTypeName = "Test Issue " + CommonExtensions.Helpers.RandomStringUpper(4);
      var it = await Rest.ProjectIssueTypeAddOrDelete("IM", issueTypeName);
      await Rest.ProjectIssueTypeAddOrDelete("IM", issueTypeName, true);
      await it.ToRestMonad().DeleteIssueTypeAsync();
    }
    [TestMethod()]
    public async Task WorkflowIssueTypeDraftStartStop() {
      var rm = RestMonad.Empty();
      var projectKey = "IM";
      var issueType = "ZZZ Issue Type " + CommonExtensions.Helpers.RandomStringUpper(4);
      var workflow = "ZZZ1";
      var r = await (
        from it in rm.PostIssueTypesAsync(issueType, "Delete Me")
        from ait in Rest.ProjectIssueTypeAddOrDelete(projectKey, it.Value.name)
        from s0 in it.WorkflowIssueTypeAttach(projectKey, issueType, workflow)
        from s in it.WorkflowIssueTypeAttach(projectKey, issueType,"jira")
        from dit in ait.ToRestMonad().DeleteIssueTypeAsync()
        select s.Value
        );
      var doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml(r);
      Console.WriteLine(Regex.Replace(doc.DocumentNode.InnerText,"[\r\n]{3,}","\n"));
    }


    [TestMethod]
    public async Task ProjectIssueTypeSchemaId() {
      var projectKey = "IM";
      var issueTypeSchemeId = await RestMonad.Empty().GetProjectIssueTypeSchemeId(projectKey);
      Console.WriteLine(new { issueTypeSchemeId });
    }
    [TestMethod]
    public async Task ProjectWorkflowSchema() {
      var projectKey = "IM";
      var id = await RestMonad.Empty().GetProjectWorkflowShemeAsync(projectKey);
      Console.WriteLine(id.Value.ToJson() );
    }
  }
}