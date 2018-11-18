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
      } catch(HttpResponseMessageException exc) {
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
        from s in it.WorkflowIssueTypeAttach(projectKey, issueType, "jira")
        from dit in ait.ToRestMonad().DeleteIssueTypeAsync()
        select s.Value
        );
      var doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml(r);
      Console.WriteLine(Regex.Replace(doc.DocumentNode.InnerText, "[\r\n]{3,}", "\n"));
    }


    [TestMethod]
    public async Task ProjectIssueTypeSchemaId() {
      await Core.IsJiraDev();
      var projectKey = "AMEX";
      var rm = await RestMonad.Empty().GetProjectIssueTypeSchemeId(projectKey);
      Console.WriteLine(new { projectKey, issueTypeScheme = rm.Value });
      Assert.IsTrue(rm.Value.id > 0);
      var projectId = int.Parse((await rm.GetProjectAsync(projectKey)).Value.id);
      Assert.IsTrue(projectId > 0);
      await Rest.ProjectIssueTypeSchemeSetAsync(projectId, 14070);


      // Set schemes back
      await Rest.ProjectIssueTypeSchemeSetAsync(projectId, rm.Value.id);
    }
    [TestMethod]
    [TestCategory("Dev")]
    [TestCategory("Manual")]
    [TestCategory("Project")]
    public async Task ProjectCleanWorkflows_M() {
      //Assert.Inconclusive("Manually run");
      await Core.IsJiraDev();
      var res = await (
        from dp in RestMonad.Empty().ProjectCleanWorkflowsAsync("ACW").WithError()
        select dp
        .SideEffect(se => Console.WriteLine(se.ToJson())));
      Console.WriteLine(res);
      Assert.IsNull(res.error);
    }
    [TestMethod]
    public async Task ProjectWorkflowSchema() {
      await Core.IsJiraDev();
      var projectKey = "AMEX";
      var rm = RestMonad.Empty();
      var projectId = int.Parse((await rm.GetProjectAsync(projectKey)).Value.id);
      var workflowSchemeIdNew = 12974;
      var workflowSchemeId = (await rm.ProjectWorkflowShemeGetAsync(projectKey)).Value.parentId;
      Console.WriteLine(new { workflowSchemeId });
      await Rest.ProjectWorkflowSchemeSetAsync(projectId, workflowSchemeIdNew);
      await Rest.ProjectWorkflowSchemeSetAsync(projectId, workflowSchemeId);
    }
    [TestMethod]
    public async Task ProjectIssueTypeScreenScheme() {
      await Rest.IsJiraDev();
      var projectKey = "ACW";
      var schemeId = 12191;
      await Rest.ProjectIssueTypeScreenSchemeAdd(projectKey, schemeId);
      //Console.WriteLine(new { issueTypeSchemeId });
    }
    [TestMethod]
    [TestCategory("Manual")]
    public async Task FieldConfiguration() {
      //Assert.Inconclusive();
      await Rest.IsJiraDev();
      var configName = "SR: Generic";
      var suffix = " " + Helpers.RandomStringUpper(4);

      var fieldSchemeId = (await Rest.FieldConfigurationSchemeAddAsync(configName + suffix)).First();
      Console.WriteLine(new { fieldSchemeId });


      var fc = await Rest.FieldConfigurationIdAsync(configName);
      Console.WriteLine(new { fc = fc.Flatten() });
      Assert.IsTrue(fc.Contains(10800));

      var newName = configName + " " + suffix;
      var id = await Rest.FieldConfigurationCopyAsync(fc.First(), newName);
      var nfc = await Rest.FieldConfigurationIdAsync(newName);
      Console.WriteLine(new { nfc = nfc.Flatten() });
      Assert.IsTrue(nfc.Contains(id));

      id = await Rest.FieldConfigurationCopyAsync(fc.First(), newName);
      Assert.IsTrue(nfc.Contains(id));

      await Rest.FieldConfigurationSchemeAddConfigAsync(fieldSchemeId, id);

      await Rest.ProjectFieldConfigurationSchemeAddAsync("ACW", fieldSchemeId);

      var fieldSchemaId2 = await Rest.FieldConfigurationSchemeIdAsync("ACW: Account Closing");
      Assert.AreEqual(1, fieldSchemaId2.Length);

      await Rest.ProjectFieldConfigurationSchemeAddAsync("ACW", fieldSchemaId2.First());

      await Rest.FieldConfigurationSchemeDeleteAsync(fieldSchemeId);
      await Rest.FieldConfigurationDeleteAsync(nfc.First());
      nfc = await Rest.FieldConfigurationIdAsync(newName);
      Assert.IsTrue(nfc.IsEmpty());
    }

    [TestMethod]
    [TestCategory("Project")]
    public async Task ProjectRolesClear() {
      var projectKey = "ACW";
      await RestMonad.Empty().ProjectRolesClearAsync(projectKey);
    }

    [TestMethod]
    [TestCategory("Project")]
    public async Task ProjectRoles() {
      var roleToName = "Users";
      var groupToAdd = "jira";
      var projectKey = "ACW";
      var rm = RestMonad.Empty();

      await ExceptionAssert.Propagates<HttpResponseMessageException>(rm.ProjectRolesGetAsync(projectKey + 2), exc => Assert.IsTrue(exc.Message.Contains(projectKey + 2)));

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
      await (from role in projectRoles
             where role.Value.name.ToLower() == roleToName.ToLower()
             from actor in role.Value.actors
             where actor.name.ToLower() == groupToAdd.ToLower()
             select role.DeleteAsync($"{role.Value.self}?{actor.Type}={actor.name}")
       ).WhenAllSequiential();

      // Add group to missing Project Role
      var rolesEmpty = await rm.ProjectRolePostAsync(projectKey, roleToName + "2", groupToAdd);
      Assert.IsTrue(rolesEmpty.Value.IsEmpty());
      // Add missing group to Project Role
      await ExceptionAssert.Propagates<HttpResponseMessageException>(rm.ProjectRolePostAsync(projectKey, roleToName, groupToAdd + 2), exc => Assert.IsTrue(exc.Message.Contains(groupToAdd + 2)));

      // Add group to Project Role
      var rolesAdded = await rm.ProjectRolePostAsync(projectKey, roleToName, groupToAdd);
      Assert.IsTrue(rolesAdded.Value.SelectMany(role => role.actors).Any(a => a.name == groupToAdd));

      // Remove group from project role
      (await (from roleRemoved in rm.ProjectRolesRemoveMemberAsync(projectKey, roleToName, groupToAdd)
              from role in roleRemoved.Value
              select role
       ))
       .Counter(1)
       .ForEach(role => Assert.AreEqual(roleToName, role.name));
      (await rm.RolesGetAsync(roleToName)).Value.SelectMany(role => role.actors).Where(a => a.name == groupToAdd).Counter(0);

      //await RestMonad.Empty().ProjectRolesDeleteAsync("ACW");
      //roles = (await RestMonad.Empty().ProjectRolesGetAsync("ACW")).Value;
      //Console.WriteLine(roles.ToJson());
      //Assert.IsFalse(roles.Any());
    }
    [TestMethod]
    [TestCategory("Project")]
    [TestCategory("Manual")]
    public async Task Roles() {
      Assert.Inconclusive("Manual");
      var rm = RestMonad.Empty();
      var roles = (await (
        from rolesRM in rm.RolesGetAsync()
        from role in rolesRM.Value
        select role
        ))
        .Where(role => role.actors?.Any() == true)
        .OrderBy(role => role.name)
        .ToList();
      Assert.IsTrue(true, "User 'true' do delete default roles");
      //Console.WriteLine(roles.ToJson());
      Assert.IsTrue(roles.Any());
      var x = await (from role in roles
                     from actor in role.actors
                     select rm.DeleteAsync($"{role.self}/actors?{actor.Type}={actor.name.ToLower()}").SideEffect(_ => Console.WriteLine(new { role = role.name, actor = actor.name }))
       ).WhenAllSequiential();
      //await RestMonad.Empty().ProjectRolesDeleteAsync("ACW");
      //roles = (await RestMonad.Empty().ProjectRolesGetAsync("ACW")).Value;
      //Console.WriteLine(roles.ToJson());
      //Assert.IsFalse(roles.Any());
    }

  }
}