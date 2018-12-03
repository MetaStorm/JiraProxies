using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wcf.ProxyMonads;
using Jira;
using CommonExtensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using HtmlAgilityPack;

namespace ProxiesTest {
  [TestClass]
  public class UnitTest1 {
    [TestMethod]
    public void GetCustomAfields_Assist() {
      var applicationsServices = "External Reference Number";
      var branche = "Account Number";
      var user = "banker";
      var customFields = GetAssistPasswordResetCustomFields(applicationsServices, branche, user, (a, b, u) => new { a, b, u });
      Console.WriteLine(customFields.ToJson());
    }

    [TestMethod]
    public async Task ResolveIssueFieldsWithAlias() {
      var cip_mail = "eMail Main Signer";
      var customFields = (await RestMonad.Empty().ResolveCustomFields(new Dictionary<string, object> { { cip_mail, "dimok" } })).Value;
      AreEqual(1, customFields.Length, "customFields.Length");
      var jiraValue = customFields[0].GetJiraValue();
      var rawValue = customFields[0].GetRawValue();
      AreEqual("dimok", jiraValue, "jiraValue");
      AreEqual("dimok", rawValue, "rawValue");
      Console.WriteLine(customFields.ToJson());
    }
    [TestMethod]
    [ExpectedException(typeof(MissingFieldException))]
    public async Task ResolveIssueFieldsMissimg() {
      var cps = new Dictionary<string, object> { { "dimok", "dimon" } };
      try {
        var res = (await new RestMonad().ResolveCustomFields(cps));
        var count = res.Value.Count();
        IsTrue(count > 0);
      } catch (AggregatedException exc) {
        var hasMissingField = exc.InnerExceptions.OfType<MissingFieldException>().Any(e => e.Message.ToLower().Contains("dimok"));
        IsTrue(hasMissingField, "hasMissingField");
        throw exc.InnerExceptions[0];
      }
    }

    [TestMethod]
    [TestCategory("Manual")]
    public async Task GetStringAsync_M() {
      Assert.Inconclusive();
      var jiraHost = "https://aliceice.atlassian.net/";
      var jiraUser = "admin";
      var jiraPassword = "1Aaaaaaa";
      string[] wsIds = (await Jira.Rest.GetWorlflowSchemeIdsAsync(jiraHost, jiraUser, jiraPassword)).Value;
      Console.WriteLine(wsIds.ToJson());
    }
    [TestMethod]
    public async Task GetWorlflowSchemeIds() {
      //Assert.Inconclusive();
      string[] wsIds = (await new  RestMonad().GetWorlflowSchemeIdsAsync()).Value;
      Console.WriteLine(wsIds.ToJson());
    }

    public static async Task<string[]> GetWorlflowSchemeIds(string jiraHost, string jiraUser, string jiraPassword) {
      var s = (await new RestMonad(jiraHost, "", jiraUser, jiraPassword).GetStringAsync("secure/admin/ViewWorkflowSchemes.jspa")).Value;
      var doc = new HtmlDocument();
      doc.LoadHtml(s);
      var searchPath = "//tr/td/ul/li/a[contains(.,'Edit') and contains(@href,'EditWorkflow')]";
      var edits = doc.DocumentNode.SelectNodes(searchPath);
      var wsIds = edits.Select(link => link.Attributes["href"].Value.Split('?')[1].Split('=')[1]).ToArray();
      return wsIds;
    }

    public delegate T AssistPasswordFields<T>(Jira.Json.Field applicationsServices, Jira.Json.Field branch, Jira.Json.Field user);

    private static T GetAssistPasswordResetCustomFields<T>(string applicationsServices, string ioc_apps_assist_branches, string ioc_apps_user_identification, AssistPasswordFields<T> map) {
      var restOut = RestMonad.Empty();
      var rest = restOut.GetFields(applicationsServices, ioc_apps_assist_branches, ioc_apps_user_identification).Result;
      var customFields = rest.Value.ToIgnoreCaseDictionary(cf => cf.name, cf => cf);
      Func<string, Jira.Json.Field> getField = field => {
        Jira.Json.Field f;
        if (!customFields.TryGetValue(field, out f))
          throw new Exception(new { rest = RestMonad.ToString(rest), field, error = "Missing" } + "");
        return f;
      };
      return map(getField(applicationsServices), getField(ioc_apps_assist_branches), getField(ioc_apps_user_identification));
    }
    [TestMethod]
    public async Task ExtractId() {
      var issue = (await "EC-59".ToJiraTicket().GetIssueAsync()).Value;
      Assert.AreEqual("407807", issue.ExtractCustomFieldRaw("Id",true,true).Single());
    }
  }
}
