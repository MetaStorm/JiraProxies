using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jira;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wcf.ProxyMonads;
using CommonExtensions;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Diagnostics;
using Jira.Json;
using System.IO;
using System.Text.RegularExpressions;
using static Jira.Rest;
using Newtonsoft.Json;

namespace Jira.Tests {
  [TestClass()]
  public class RestTests {
    private const string projectKey = "HLP";
    private const string issueType = "password reset";
    private string assignee = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\').Last();
    private string reporter => JiraMonad.JiraPowerUser();
    private const int SECURITY_LEVEL_ID = 10661;
    private const string SECURITY_LEVEL_NAME = "CHILE";
    readonly static string _ticketKey = "SUI-1199";
    [TestMethod]
    public async Task ResetSessionID() {
      await SetDefaultSessionID();
    }

    [TestMethod]
    public async Task GetTicketWithSessionID_M() {
      Assert.Inconclusive();
      RestMonad.UseSessionID = true;
      await JiraMonad.RunTestFastAsync();
      var issue = (await _ticketKey.ToJiraTicket().GetIssueAsync()).Value;
      Assert.AreEqual(_ticketKey, issue.key);
      JiraMonad.SessionIDs[JiraMonad.JiraPowerUser()] = Task.FromResult("AAAA");
      await _ticketKey.ToJiraTicket().GetIssueAsync();
    }
    [TestMethod()]
    public void ResolveIssueFields() {
      var customFieldName = "customfield";
      try {
        (from fields in new RestMonad().GetFields(customFieldName)
         let field = fields.Value.SkipWhile(cp => cp.name == "Duplicate").First()
         let cps = fields.Value.SkipWhile(cp => cp.name == "Duplicate").Take(1).ToDictionary(f => f.name, f => (object)"irrelevant value")
         from cfs in new RestMonad().ResolveCustomFields(cps)
         select cfs.Value.Select(v => new { field.id, Key = v.field.id, Value = v.GetRawValue() })
        ).Result
        .ToList()
        .ThrowIfEmpty(new Exception(new { customFieldName, message = "Not Found" } + ""))
        .ForEach(a => {
          Assert.AreEqual(a.Key, a.id);
          Assert.AreEqual(a.Value, "irrelevant value");
        });
      } catch (Exception exc) {
        Console.WriteLine(exc + "");
        throw;
      }
    }
    [TestMethod]
    public async Task GetSecurityLevel() {
      try {
        var secLevId = (await new JiraNewIssue {
          fields = new JiraNewIssue.Fields {
            project = new Project { key = "SUI" },
            security = new SecurityLevel { name = SECURITY_LEVEL_NAME }
          }
        }.ToRestMonad().ResolveSecurityLevel()).fields.security.id;
        var secLev = (await new RestMonad().GetSecurityLevel(int.Parse(secLevId))).Value;
        Assert.AreEqual(SECURITY_LEVEL_NAME.ToLower(), secLev.name.ToLower(), new { secLev }.ToJson(false));
      } catch (Exception exc) {
        Console.WriteLine(exc);
        throw;
      }
    }
    [TestMethod]
    public async Task GetSecurityLevels() {
      var x = (await RestMonad.Empty().GetSecurityLevels()).Value;
      Console.WriteLine(x.Flatten("\n"));
      Assert.IsTrue(x.Any());
    }

    [TestMethod]
    public async Task PostSecureIssue() {
      //Assert.Inconclusive("Can be used only in JIRA 6.4 and up");
      var customFields = new Dictionary<string, object>() {
        { "Account Number", "5555555" },
        { "eMail main signer", "dimok" },
        { "Language", "en" } };
      Action<JiraNewIssue> addSecurity = issue => issue.fields.security = new SecurityLevel { name = SECURITY_LEVEL_NAME };
      var ticket = (await RestMonad.Empty().PostIssueAsync("SUI", "Questionnaire", "Secured Issue", "", "", "", null, customFields, null, null, null, addSecurity)).Value.key;
      Trace.WriteLine(new { ticket, message = "Done" });
      {
        var issue = await ticket.ToJiraTicket().GetIssueAsync();
        Assert.AreEqual(SECURITY_LEVEL_NAME.ToLower(), issue.Value.fields.security.name.ToLower());
      }
      {
        var SEC_LEVEL = "IPB - MIAMI - Operations";
        await ticket.ToJiraTicket().PutIssueSecurityAsync(SEC_LEVEL);
        var issue = (await ticket.ToJiraTicket().GetIssueAsync()).Value;
        Assert.IsNotNull(SEC_LEVEL, issue.fields.security.name);
      }
      await ticket.ToJiraTicket().DeleteIssueAsync();
    }
    [TestMethod()]
    public async Task PostFullIssue() {
      var branches = new[] { "MIAMI", "BAHAMAS" };
      var smsPhone = new Random().Next();
      var cusomProps = new Dictionary<string, object> {
      { "Applications And Services", "Assist CK" } ,
      { "Sms Phone", smsPhone } ,
      //{"UserID","dimok"}
      //{"ioc_apps_assist_branches",branches } // this is another way
      //{"ioc_apps_assist_branches",new[]{"MIAMI"}}
      };

      var customFieldName = "Sms Phone";
      var execCountField = "ExecCount";
      var customFields = new Dictionary<string, object>() {
        {"Applications And Services",new[]{"Assist CK"}},
      { "Applications List", "Miami" } ,
        //{customFieldName,"5555555"},
        { "Sms Phone", smsPhone+"" } ,
        {execCountField,1},
        //{"ioc_apps_assist_branches",branches } // this is another way
      };
      var path = CommonExtensions.Helpers.GetExecPath(@"..\..\barcode-image.gif");
      var comment = @"{color:red}*Bold Red Comment!*{color}";
      var webLinks = new Dictionary<Uri, string> { { new Uri("http://google.com"), "GooGle" } };
      var issue = await RestMonad.Empty().PostIssueAsync(projectKey, issueType, IssueClasses.Issue.DoSkip("kill it", true), null, assignee, reporter, new[] { "miami" }, customFields, comment, new[] { path }, webLinks);
      issue = await issue.Value.key.ToJiraTicket().GetIssueAsync();
      var ticket = issue.Value.key;
      var jiraTicket = ticket.ToJiraTicket();
      Console.WriteLine(issue.Value);
      Trace.TraceInformation(issue.ToJson());
      Assert.AreEqual(projectKey.ToLower(), issue.Value.fields.project.key.ToLower());
      Assert.AreEqual(issueType.ToLower(), issue.Value.fields.issuetype.name.ToLower());
      Assert.IsTrue(issue.Value.IsSkip, new { issue.Value.IsSkip } + "");
      Assert.AreEqual("kill it" + IssueClasses.Issue.DoSkip(true), issue.Value.fields.summary);
      Assert.AreEqual("kill it" + IssueClasses.Issue.DoSkip(true), issue.Value.fields.description);
      Assert.IsTrue(issue.Value.fields.assignee.displayName.ToLower().Contains("dmitri"));
      Assert.IsTrue(issue.Value.fields.assignee.displayName.ToLower().Contains("lapchine"));
      Assert.AreEqual("ICE Admin".ToUpper(), issue.Value.fields.reporter.displayName.ToUpper());
      Assert.AreEqual("miami", issue.Value.fields.components.Single().name.ToLower());
      Assert.IsTrue(issue.Value.fields.comment.comments.Single().body.Contains("Bold Red Comment!"));
      Assert.AreEqual("barcode-image.gif", issue.Value.fields.attachment[0].filename);
      var barCode = (await jiraTicket.GetBytesAsync(issue.Value.fields.attachment[0].content)).Value;
      var barCode2 = File.ReadAllBytes(path);
      Assert.IsTrue(barCode.SequenceEqual(barCode2));
      var webLinks2 = await ticket.ToJiraTicket().GetWebLinkAsync("GooGle");
      Assert.IsTrue(webLinks2.Do(rm => Assert.AreEqual("GooGle", rm.@object.title)).Any());

      // Fast forward
      const string resolvedComment = "Issue been resolved";
      Trace.TraceInformation("{0}", new { ticket, transition = (await ticket.ToJiraTicket().PostIssueTransitionAsync(issue.Value.FastForwardTransitions().Single(), resolvedComment, null)).Value });

      // Decrement counter
      await jiraTicket.ExecCountStep("", "Count Me Down");
      issue = await jiraTicket.GetIssueAsync();
      var execCount = issue.Value.ExtractCustomField<int>(execCountField)[0];
      Assert.AreEqual(execCount, 0, "execCount");
      Assert.AreEqual("ON HOLD", issue.Value.fields.status.name.ToUpper());

      // Increment Counter
      (await ticket.ToJiraTicket().ExecCountStep("up", "Go on Zero")).Single();
      issue = await jiraTicket.GetIssueAsync();
      execCount = issue.Value.ExtractCustomField<int>(execCountField)[0];
      Assert.AreEqual(1, execCount, "execCount");
      Assert.AreEqual("OPEN", issue.Value.fields.status.name.ToUpper());
      // Increment Counter one more time
      try {
        (await ticket.ToJiraTicket().ExecCountStep("up", "Go on Zero")).Single();
      } catch (Exception exc) {
        Assert.IsTrue(exc.Message.Contains("Not found") && exc.Message.Contains("counter-up"), exc.Message);
      }
      issue = await jiraTicket.GetIssueAsync();
      execCount = issue.Value.ExtractCustomField<int>(execCountField)[0];
      Assert.AreEqual(1, execCount, "execCount");
      Assert.AreEqual("OPEN", issue.Value.fields.status.name.ToUpper());

      //Trace.TraceInformation("{0}", new { ticket, transition = (await jiraTicket.PostIssueTransitionAsync("resolve issue", resolvedComment, true, null)).Value });

      var test = (await jiraTicket.GetIssueAsync(new[] { Rest.IssueFielder.status, Rest.IssueFielder.comment })).Value;
      Assert.IsNull(test.fields.description);
      Assert.AreEqual("OPEN", test.fields.status.name.ToUpper());

      var ticketOutward = await RestMonad.Empty().PostIssueAsync(projectKey, issueType, IssueClasses.Issue.DoSkip("kill it"), null, assignee, reporter, new[] { "miami" }, customFields, "Linked issue", new[] { path }, webLinks);
      await ticketOutward.Value.key.ToJiraTicket().PostIssueTransitionAsync(ticketOutward.Value.FastForwardTransitions().Single(), "", null);
      await ticketOutward.Value.key.ToJiraTicket().PostIssueTransitionAsync("Resolve Issue", resolvedComment, true, null);

      ticketOutward = await ticketOutward.Value.ToJiraTicket().GetIssueAsync();
      Assert.AreEqual(resolvedComment.LockIt(), ticketOutward.Value.fields.comment.comments.Last().body);
      Assert.IsTrue(ticketOutward.Value.IsLocked);

      await ticketOutward.Value.key.ToJiraTicket().PostIssueTransitionAsync(resolvedComment, false, null);
      ticketOutward = await ticketOutward.Value.ToJiraTicket().GetIssueAsync();
      Assert.AreEqual("REOPENED", ticketOutward.Value.fields.status.name.ToUpper());

      await jiraTicket.PostIssueLinkAsync(ticketOutward.Value.key, "Odd link couple");
      issue = await jiraTicket.GetIssueAsync(new[] { Rest.IssueFielder.issuelinks });
      //ticket.Value.fields.issuelinks[0]
      Console.WriteLine(issue.Value);
      Console.WriteLine(new { inward = ticket, outward = ticketOutward.Value.key });
      Assert.AreEqual(issue.Value.fields.issuelinks[0].outwardIssue.key, ticketOutward.Value.key);
      // Update custom field
      customFields[customFieldName] = "(" + customFields[customFieldName] + ")";
      await jiraTicket.PutIssueAsync(null, customFields);
      issue = await jiraTicket.GetIssueAsync();
      var customFieldValue = issue.Value.fields.custom[customFieldName].Single();
      Assert.AreEqual(customFields[customFieldName], customFieldValue);
      // Post secured comment
      var commentRole = "Administrators";
      var securedComment = commentRole + " eyes only";
      await jiraTicket.PostCommentsAsync(securedComment, true, commentRole);
      issue = await jiraTicket.GetIssueAsync();
      var lastComment = issue.Value.fields.comment.comments.Last();
      Assert.AreEqual("role", lastComment.visibility.type);
      Assert.AreEqual(commentRole, lastComment.visibility.value);
      Assert.IsTrue(lastComment.body.Contains(securedComment));

      var smsComment = "Some text message to be sent through";
      await jiraTicket.PostCommentsAsync(IssueClasses.Issue.SmsComment(smsComment, true), false, commentRole);
      issue = await jiraTicket.GetIssueAsync();
      Assert.IsTrue(issue.Value.IsSms);

      var jrqFilter = new Rest.JqlFilter { Jql = new Rest.JqlFilter.JQL("Sms Phone", smsPhone), MaxResults = 100 };
      var res = await JiraRest.Create(jrqFilter).GetTicketsAsync();
      await Task.FromResult(0);

    }
    [TestMethod()]
    public void PostIssueMissingFieldValue() {
      var customFields = new Dictionary<string, object>() {
        {"Applications List",null}
        //{"ioc_apps_user_identification","rester"}
      };
      try {
        var ticket = RestMonad.Empty().PostIssueAsync(projectKey, issueType, "kill it", null, null, null, new[] { "miami" }, customFields, null, null, null).Result;
      } catch (AggregateException axc) {
        var message = ((System.Exception)(axc)).InnerException.Message;
        Assert.IsTrue(message.Contains("Applications List is required."), message);
        Console.WriteLine(string.Join("\n", axc.InnerExceptions.Select(e => e + "")));
        return;
      }
      Assert.Fail("Exception should be thrown.");
    }
    [TestMethod()]
    public async Task PostIssueEmptyFieldValue() {
      var customFields = new Dictionary<string, object>() {
        {"Applications List",new string[0]},
        //{"ioc_apps_user_identification","cip_admin"}
      };
      Trace.TraceInformation((await RestMonad.Empty().GetFields("External Reference Number", "ioc_apps_user_identification")).Value.ToJson());
      try {
        var ticket = RestMonad.Empty().PostIssueAsync(projectKey, issueType, "kill it", null, null, null, new[] { "miami" }, customFields, null, null, null).Result;
      } catch (AggregateException axc) {
        var message = ((System.Exception)(axc)).InnerException.Message;
        Assert.IsTrue(message.Contains("Applications List is required."), message);
        Console.WriteLine(string.Join("\n", axc.InnerExceptions.Select(e => e + "")));
        return;
      }
      Assert.Fail("Exception should be thrown.");
    }
    [TestMethod()]
    public async Task PostIssueDuplicatedFieldName() {
      var customFields = new Dictionary<string, object>() {
        {"Duplicate","5555555"}
      };
      try {
        var cfs = await RestMonad.Empty().ResolveCustomFields(customFields);
      } catch (Exception exc) {
        Assert.IsTrue(exc.Message.Contains(customFields.First().Key), exc.Message);
        return;
      }
      Assert.Fail("Exception should be thrown.");
    }
    [TestMethod]
    public async Task PostPSTicket() {
      string project = "PS", issueType = "OPS-051-Wire Notification";
      var newIssue = JiraNewIssue.Create(project, issueType, "Wire Sms Confirmation Test", "", null);
      var customFields = new Dictionary<string, object> {
        { "Account Number","1111-000001" },
        { "SMS Phone","13057880763" },
        { "Amount","$1.00" },
        { "Beneficiary","Demo"}
      };
      var issue = await newIssue.ToJiraPost().PostIssueAsync(customFields);
      Assert.IsNotNull(issue);
      Console.WriteLine(issue.Value);
      var jiraTicket = issue.Value.key.ToJiraTicket();
      await jiraTicket.DeleteIssueAsync();
      try {
        await jiraTicket.GetIssueAsync();
      } catch (HttpResponseMessageException exc) {
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, exc.Response.StatusCode);
        return;
      }
      Assert.Fail("Issue still there.");
    }
    [TestMethod]
    public async Task PostAccessRequestTicket() {
      string project = "HLP", issueType = "Access Request";
      var newIssue = JiraNewIssue.Create(project, issueType, "Create ticket Test", null, "", "", new[] { "Miami" });
      var customFields = new Dictionary<string, object> {
        { "Applications List",new[] { "Miami","Dashboard Finance" } }
      };
      var issue = await (from rm in newIssue.ToJiraPost().PostIssueAsync(customFields)
                         from rm1 in rm.GetIssueAsync()
                         select rm1.Value);

      Assert.IsNotNull(issue);
      Console.WriteLine(issue);
      var appList = issue.ExtractCustomField<string>("Applications List");
      Assert.AreEqual("Miami", appList[0]);
      Assert.AreEqual("Dashboard Finance", appList[1]);
      var jiraTicket = issue.key.ToJiraTicket();
      await jiraTicket.DeleteIssueAsync();
      try {
        await jiraTicket.GetIssueAsync();
      } catch (HttpResponseMessageException exc) {
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, exc.Response.StatusCode);
        return;
      }
      Assert.Fail("Issue still there.");
    }
    [TestMethod]
    public async Task PostUserSupportTicket() {
      string project = "HLP", issueType = "User Support";
      var newIssue = JiraNewIssue.Create(project, issueType, "Wire Sms Confirmation Test", null, new[] { "Miami" });
      var customFields = new Dictionary<string, object> {
        {"Applications List",new[] {"Miami" } }
      };
      var issueRM = await newIssue.ToJiraPost().PostIssueAsync(customFields);
      Assert.IsNotNull(issueRM);
      Console.WriteLine(issueRM.Value);
      var jiraTicket = issueRM.Value.key.ToJiraTicket();
      var issue = await jiraTicket.GetIssueAsync();
      await jiraTicket.DeleteIssueAsync();
      try {
        await jiraTicket.GetIssueAsync();
      } catch (HttpResponseMessageException exc) {
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, exc.Response.StatusCode);
        return;
      }
      Assert.Fail("Issue still there.");
    }
    [TestMethod()]
    public async Task UserSelf() {
      var test = await Jira.Rest.RunTestAsync();
      Trace.TraceInformation(test.ToJson());
      var rest = (dynamic)test.First().Value;
      var u = rest.Value is Jira.Json.User;
      Assert.IsTrue(u);
      // Test should not run again
      test = await Jira.Rest.RunTestAsync();
      Assert.IsTrue(((dynamic)test.Single().Value).IsTested);
    }
    [TestMethod()]
    public async Task UserSome() {
      var user = (await RestMonad.Create(User.FromKey("ipbldit")).GetAsync()).Value;
      Console.WriteLine(user.ToJson());
      Assert.IsTrue(user.InGroup("it"));
      Assert.IsFalse(user.InGroup("it_"));
    }
    [TestMethod()]
    public async Task GroupCreateDelete() {
      var jiraGroup = await Json.Group.FromUserName("jira").ToRestMonad().GetAsync();
      Console.WriteLine(jiraGroup.Value.ToJson());
      Assert.AreEqual("jira", jiraGroup.Value.name);
      var groupName = "Test Group " + CommonExtensions.Helpers.RandomStringUpper(4);
      var oldGroup = (await Json.Group.FromUserName(groupName).ToRestMonad().GetAsync()).Value;
      Assert.IsNull(oldGroup);
      var group = await (rm: jiraGroup, groupName: groupName).PostAsync();
      Console.WriteLine(group.Value.ToJson());
      Assert.AreEqual(groupName, group.Value.name);
      await group.DeleteAsync();
      oldGroup = (await (rm: group, groupName: groupName).GetAsync()).Value;
      Assert.IsNull(oldGroup);
    }
    [TestMethod()]
    public async Task UserCreateDelete() {
      var selfUser = await RestMonad.Empty().GetMySelfAsync();
      Console.WriteLine(selfUser.Value.ToJson());
      Assert.IsNotNull(selfUser.Value);

      var userName = "Test User " + CommonExtensions.Helpers.RandomStringUpper(4);
      var user = User.New(userName, userName, "dmitri.lachine@itauinternational.com");
      var oldUser = (await user.GetAsync()).Value;
      Assert.IsNull(oldUser);
      var jiraUser = await user.PostOrGetAsync();
      Console.WriteLine(jiraUser.Value.ToJson());
      Assert.AreEqual(userName, jiraUser.Value.name);

      jiraUser = await user.PostOrGetAsync();
      Assert.AreEqual(userName, jiraUser.Value.name);

      await user.DeleteAsync();
      oldUser = (await user.GetAsync()).Value;
      Assert.IsNull(oldUser);
      //await user.DeleteAsync();
    }

    [TestMethod()]
    public void ResolveComponents() {
      var newIssue = Jira.Json.JiraNewIssue.Create(null, null, null);
      newIssue.fields.project.key = projectKey;
      newIssue.fields.components = new List<IssueClasses.Component>() {
        new IssueClasses.Component { name = "miami" } ,
        new IssueClasses.Component { name = "Caribbean" }
      };
      var test = RestMonad.Create(newIssue).ResolveComponents().Result.Value;
      Trace.TraceInformation(test.ToJson());
      var ids = test.fields.components.Select(c => c.id).ToArray();
      Trace.TraceInformation(ids.ToJson());
      Assert.AreEqual(2, ids.Length);
    }
    [TestMethod()]
    public async Task ResolveComponentsMissing() {
      var newIssue = Jira.Json.JiraNewIssue.Create(null, null, null);
      newIssue.fields.project.key = projectKey;
      newIssue.fields.components = new List<IssueClasses.Component>() {
        new IssueClasses.Component { name = "dimok" } ,
        new IssueClasses.Component { name = "dimon" }
      };
      try {
        await RestMonad.Create(newIssue).ResolveComponents();
        Assert.Fail("Must throw missing component exception.");
      } catch (AggregateException exc) {
        Assert.IsTrue(exc.InnerExceptions[0].Message.Contains("dimok"));
        Assert.IsTrue(exc.InnerExceptions[1].Message.Contains("dimon"));
      }
    }
    [TestMethod]
    public void HtmlDocumentTest() {
      var doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml("<Dimok><A><B id='c'>Test</B></A></dimok>");
      var b = doc.DocumentNode.Descendants().Where(d => d.Id == "c").Select(n => n.InnerText).FirstOrDefault();
      doc.LoadHtml("<Dimok><A><B id='d'>Test</B></A></dimok>");
      b = doc.DocumentNode.Descendants().Where(d => d.Id == "c").Select(n => n.InnerText).FirstOrDefault();

    }
    [TestMethod]
    public async Task Timeouter() {
      var state = "Access Granted";
      var dateEndField = "lockbox_end";
      var issueType = "LockBox - Open";
      Func<DateTime?, bool> p = d => d != null;
      var search = await RestMonad.Empty().SearchValue(projectKey, issueType, state, dateEndField, p, (issue, value) => new { issue, value }, 100);
      search.Take(1).ForEach(x => Console.WriteLine(x));
      Assert.IsNotNull(search.Any());
    }
    [TestMethod]
    public async Task CreateSUI_M() {
      //Assert.Inconclusive("Manual test");
      var emailSource = DateTime.Now.Subtract(DateTime.MinValue).TotalMilliseconds.ToString().Split('.');
      var accountNubmer = new string((DateTime.Now.Ticks + "").TakeLast(7).ToArray());
      var userEmail = "dimokdimon___cip01@gmail.com";
      var customFields = CommonExtensions.Helpers.ToFunc(() => new Dictionary<string, object> {
        { "eMail Main Signer", userEmail },
        { "language", "en" },
        {"Account Number",accountNubmer }
      });
      // Create ticket
      var ticket = (await RestMonad.Empty().PostIssueAsync("SUI", "Questionnaire", IssueClasses.Issue.DoSkip("Test:" + userEmail, true), "Unit test", null, null, null, customFields(), "", null, null)).Value;
      Console.WriteLine(ticket);
    }
    [TestMethod]
    public async Task CustomFieldGet_M() {
      //Assert.Inconclusive("For manual testing only");
      var fieldName = "Banker";
      var ticket = "SUI-1199";// 668";
      var issue = (await ticket.ToJiraTicket().GetIssueAsync()).Value;
      var fieldValue = issue.ExtractCustomField<object>(fieldName).Single() + "";
      var fieldValue2 = issue.ExtractCustomField<string>(fieldName).Single() + "";
      Assert.IsFalse(string.IsNullOrWhiteSpace(fieldValue));
      Assert.AreEqual(fieldValue, fieldValue2);
    }
    [TestMethod]
    public async Task CustomFieldSet_M() {
      Assert.Inconclusive("For manual testing only");
      var fieldName = "CoSigner-1 eMail";
      var fieldValue = "dimok@hot.com";
      var ticket = "SUI-1199";// 668";
      await ticket.ToJiraTicket().PutIssueFieldsAsync(fieldName, fieldValue, "eMail Main Signer", "main_" + fieldValue);
      var issue = (await ticket.ToJiraTicket().GetIssueAsync()).Value;
      var fieldValue2 = issue.ExtractCustomField<string>(fieldName).Single();
      Assert.AreEqual(fieldValue, fieldValue2);
    }
    [TestMethod]
    public async Task CustomFieldSetCheckbox_M() {
      Assert.Inconclusive("For manual testing only");
      var fieldName = "Tax Validation 6";
      var fieldValue = "none";
      var ticket = "BPM-7";// 668";
      await ticket.ToJiraTicket().PutIssueFieldsAsync(fieldName, fieldValue);
      var issue = (await ticket.ToJiraTicket().GetIssueAsync()).Value;
      var fieldValue2 = issue.ExtractCustomField<string>(fieldName).Single();
      Assert.AreEqual(fieldValue, fieldValue2);
    }
    [TestMethod]
    public async Task CustomFieldEmpty_M() {
      //Assert.Inconclusive("For manual testing only");
      var fieldName = "Validation Status 6";
      var fieldValue = "";
      var ticket = "BPM-7";// 668";
      await ticket.ToJiraTicket().PutIssueFieldsAsync(fieldName, fieldValue);
      var issue = (await ticket.ToJiraTicket().GetIssueAsync()).Value;
      var fieldValue2 = issue.ExtractCustomField<string>(fieldName).Single();
      Assert.AreEqual(" ", fieldValue2);
    }
    [TestMethod]
    public async Task IsIssueLinked() {
      const string accountField = "Account Number";
      var custom = new Dictionary<string, object> {
        { accountField, "99999" },
        {"Group Assignee","user-doc-miami" }
      };
      var issueTo = "Fix Data";
      const string project = "DIT";
      var issue = (await RestMonad.Empty().PostIssueAsync(project, "Fake Problem", IssueClasses.Issue.DoSkip("Test Clone and Link"), "", "", "", null, custom, "", null, null)).Value;
      Console.WriteLine(issue);
      var jiraTicket = issue.key.ToJiraTicket();
      var issues = (await jiraTicket.GetIssueLinks(project, issueTo)).Value;
      Assert.AreEqual(0, issues.Count);

      var emptyCustom = new Dictionary<string, object>();
      await jiraTicket.CloneIssueAndLink(project, issueTo, accountField);
      issues = (await jiraTicket.GetIssueLinks(project, issueTo)).Value;
      Assert.AreEqual(1, issues.Count);

      var linked = (await jiraTicket.CloneIssueAndLink(project, issueTo, accountField)).Value;
      issues = (await jiraTicket.GetIssueLinks(project, issueTo)).Value;
      Assert.AreEqual(1, issues.Count);

      Assert.AreEqual(project, linked.fields.project.key);
      Assert.AreEqual(issueTo, linked.fields.issuetype.name);
      Assert.AreEqual(custom[accountField], linked.ExtractCustomField<string>(accountField).Single());
    }


    [TestMethod]
    public async Task CloneIssue_M() {
      //Assert.Inconclusive("For manual testing only");
      var ticket = "CNG-2898";// 668";
      var custom = new Dictionary<string, object> {
        { "Required Hours", 5 }
      };
      var issue = (await ticket.ToJiraTicket().GetIssueAsync(new[] { IssueExpander.renderedFields })).Value;
      Console.WriteLine(issue.ToJson());
      var issue2 = await ticket.ToJiraTicket().CloneIssueAndLink("LOK", "LockBox - Open", custom, "Windows Servers", "SQL Servers", "IIS Servers", "Olympic Servers", "timetracking", "reporter");
      Console.WriteLine(issue2.ToJson());
      //custom = new Dictionary<string, object> { { "lockbox_start", DateTime.Now } };
      await issue2.Value.key.ToJiraTicket().PutIssueAsync(custom);
    }
    [TestMethod]
    public async Task AssigneePrevious_M() {
      Assert.Inconclusive("For manual testing only");
      var ticket = "DIT-392";// 668";

      var lastAssignee = (await ticket.ToJiraTicket().RollbackAssignee()).Value;

      var issueAssignee = (await ticket.ToJiraTicket().GetIssueAsync()).Value.fields.assignee.name;
      //Console.WriteLine(issue.ToJson());
      Console.WriteLine(lastAssignee.ToJson());
      lastAssignee.Counter(1).ForEach(assignee => Assert.AreEqual(assignee.from.ToLower(), issueAssignee.ToLower()));

      var custom = new Dictionary<string, object> {
        { "Account Number", "123456" }
      };
      var issue = (await JiraNewIssue.Create("DIT", "Fix Data", IssueClasses.Issue.DoSkip("Test rollback")).ToJiraPost().PostIssueAsync(custom));
      try {
        await issue.Value.key.ToJiraTicket().RollbackAssignee();
      } catch (Exception exc) {
        Assert.IsTrue(exc.Message.StartsWith("No assignee re-assignment happened in ticket"));
        await issue.Value.key.ToJiraTicket().DeleteIssueAsync();
        return;
      }
      Assert.Fail("Should not be here");
    }

    [TestMethod]
    public async Task GetCreateIssueMeta() {
      var issueMeta = (await RestMonad.Empty().GetIssueCreateFields("DIT", "Fix Data")).Value;
      Console.WriteLine(issueMeta.ToJson());
      Assert.IsFalse(issueMeta.fields.customFields["unique key"].required);
    }
    [TestMethod]
    public async Task GetAttachement() {
      var issue = (await "DIT-257".ToJiraTicket().GetIssueAsync()).Value;
      Assert.IsTrue(issue.fields.attachment?.Count > 0);
    }
    //[TestMethod]
    //public async Task PostWorklogIfDone() {
    //  var ticket = "HLP-656".ToJiraTicket();
    //  var worklogs = (await ticket.PostWorklogIfDoneAsync()).Value;
    //  Assert.IsTrue(worklogs.Any(), "No progress history");
    //  //worklogs.ForEach(worklog=>  Assert.AreEqual(worklog.started,dateStart));
    //}
    //[TestMethod]
    //public async Task PostWorklog() {
    //  var ticket = "OP-2".ToJiraTicket();
    //  var worklogs = (await ticket.PostWorklogForTransitionerAsync()).Value;
    //  Assert.IsTrue(worklogs.Any(), "No progress history");
    //  worklogs.ForEach(worklog => Trace.WriteLine(worklog.ToJson()));
    //}
    [TestMethod]
    public async Task PostWorklogByTransition() {
      var ticket = "OP-2438".ToJiraTicket();
      var transition = new WebHook.Transition {
        workflowId = 483362,
        workflowName = "SWIFT Manual Funds",
        transitionId = 171,
        transitionName = "Settled",
        to_status = "Awaiting Settlement",
        from_status = "Under Review"
      };
      var issue = (await ticket.GetIssueAsync()).Value;
      var worklogs = (await ticket.PostWorklogForTransitionerAsync(transition, 1491825626822)).Value;
      Assert.IsTrue(worklogs.Any(), "No progress history");
      var anon = new { name = "", displayName = "", stateFrom = "", stateTo = "" };
      var comment = JsonConvert.DeserializeAnonymousType(worklogs.Single().comment, anon);
      Assert.IsFalse(comment.name.IsNullOrWhiteSpace());
      Assert.IsFalse(comment.displayName.IsNullOrWhiteSpace());
      Assert.IsFalse(comment.stateFrom.IsNullOrWhiteSpace());
      Assert.IsFalse(comment.stateTo.IsNullOrWhiteSpace());
      Console.WriteLine(comment);
    }

    [TestMethod]
    public async Task FindInProgressSubTasks_M() {
      Assert.Inconclusive("For manual testing only");
      var project = "RQ";
      var assignee = "IPBLDIT";
      var currentTicket = "RQ-3371";
      var pausedIssues = await PauseOtherProgress(project, assignee, currentTicket);
      Console.Write("Paused Issues:");
      pausedIssues.ForEach(i => Console.WriteLine(i));

      Assert.IsTrue(pausedIssues.Any(), new { pauseTransitions = new { any = pausedIssues.Any() } } + "");
      var x = await (from pi in pausedIssues
                     from wl in pi.key.ToJiraTicket().PostWorklogAsync("In Progress", "On Hold", 0)
                     select wl.Value);
      Console.WriteLine(x.ToJson());
    }
    [TestMethod]
    public async Task PostComments() {
      Assert.Fail("Hangs on transition properties error");
      var issue = (await "AMF-54".ToJiraTicket().GetIssueAsync()).Value;
      if (issue.fields.status.name.ToLower() == "closed") {
        await issue.key.ToJiraTicket().PostIssueTransitionAsync("reopen", false);
        issue = (await issue.key.ToJiraTicket().GetIssueAsync()).Value;
      }
      var transition = "done";
      (await (from closeTrans in issue.FindTransitionByNameOrProperty(transition, false).Counter(1, new Exception(new { transition, Transition = "not found", issue } + ""), null)
              select issue.key.ToJiraTicket().PostIssueTransitionAsync(closeTrans, IssueClasses.Issue.DoCode("Closed by ICE", "white", "navy")).WithError()
      )
      .WhenAllSequiential()
      ).ForEach(i => Console.WriteLine(i.value));
    }

    //[TestMethod]
    //public async Task PostWorklogByChangelogId() {
    //  var ticket = "OP-2".ToJiraTicket();
    //  var chagelogId = 653091;
    //  var worklogs = (await ticket.PostWorklogForTransitionerAsync(chagelogId)).Value;
    //  Assert.IsTrue(worklogs.Any(), "No progress history");
    //  worklogs.ForEach(worklog => Trace.WriteLine(worklog.ToJson()));
    //}
  }
  static class Mixins {

  }
}
