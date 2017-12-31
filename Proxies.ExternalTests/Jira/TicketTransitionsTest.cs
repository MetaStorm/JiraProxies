using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wcf.ProxyMonads;
using CommonExtensions;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Diagnostics;
using Jira;
using Jira.Json;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Proxies.ExternalTests {
  [TestClass]
  public class TicketTransitionsTest {

    const string projectKey = "HLP";
    private const string issueType = "Password Reset";

    [TestMethod]
    public async Task ScreenDeleteByProject() {
      Assert.Inconclusive();
      await Rest.ScreenDeleteByProject("PM");
    }
    [TestMethod]
    public async Task GetScreensByProject() {
      var screens = (await RestMonad.Empty().GetScreensByProjectPrefix("DIT", "copy")).Value;
      Assert.IsTrue(screens.Length > 0);
    }
    [TestMethod]
    public async Task ProjectDelete() {
      Assert.Inconclusive("To be used manually destroy the project");
      var jo = (await RestMonad.Empty().DestroyProjectAsync("IM5")).Value;
      Console.WriteLine(jo);
    }
    [TestMethod]
    public async Task ProjectCreate_M() {
      Assert.Inconclusive();
      var x = (await from lead in RestMonad.Empty().GetMySelfAsync()
                     from p in lead.CreateDefaultProjectAsync("Import5", "IM5", "Impotred Project\nDELETE_ME", lead.Value.name)
                     from d in p.DestroyProjectAsync(p.Value.key)
                     select new { p = p.Value, d = d.Value }
                     .SideEffect(se => Console.WriteLine(se.ToJson())));
      Assert.AreEqual(0, x.d.Value<JArray>("errors").Values<string>().Count());
    }

    [TestMethod]
    public async Task ProjectsGet() {
      var rme = RestMonad.Empty();
      var projectsLookup = (await rme.GetProjectIssueTypesAsync()).Value;
      Console.WriteLine(projectsLookup.ToJson());
      Assert.AreEqual(true, projectsLookup.Any());
    }
    [TestMethod]
    public async Task GetUsers() {
      var rme = RestMonad.Empty();
      var users0 = await rme.GetUsersWithGroups();
      Console.WriteLine(users0.Value.ToJson(true, true));
      var users = users0.Value
        .Where(u => u != null)
        .ToDictionary(u => u?.name, u => u?.groups.items.Select(i => i?.name).ToArray());
      var groups = users.SelectMany(u => u.Value).Distinct();
      Console.WriteLine(groups.ToJson());
      Console.WriteLine(users.ToJson());
      Assert.AreEqual(true, users.Any());
    }
    [TestMethod]
    public async Task GetUserPermission() {
      var rme = "dit-7502".ToJiraTicket();
      var users = (await rme.GetUsersWithPermission("ipbldit")).Value;
      Console.WriteLine(users.ToJson());
      Assert.AreEqual(true, users.Any());
      users = (await "xxx-133".ToJiraTicket().GetUsersWithPermission("ipbldit")).Value;
      Assert.AreEqual(true, users.IsEmpty());
    }

    [TestMethod]
    public async Task TicketTransitions() {
      var field = new Dictionary<string, object> {
        { "Accepted", "Yes" } ,
            { "Applications List", "Miami" }
};
      var comment1 = "New Test Ticket";
      var ticket = await RestMonad.Empty().PostIssueAsync(projectKey, issueType, IssueClasses.Issue.DoSkip("Test", true), comment1, "", "", new[] { "Miami" }, field, comment1, null, null);
      var jiraTicket = ticket.Value.key.ToJiraTicket();
      var issue = (await jiraTicket.GetIssueAsync()).Value;
      var fastForwardTrans = issue.FastForwardTransitions().Single();
      await jiraTicket.PostIssueTransitionAsync(fastForwardTrans, "Fast Forward transition");

      issue = (await jiraTicket.GetIssueAsync()).Value;
      Assert.AreEqual("IN PROGRESS", issue.fields.status.name.ToUpper());
      Assert.AreEqual(0, issue.FastForwardTransitions(false).Count());
      ExceptionAssert.Propagates<AggregatedException>(() => issue.FastForwardTransitions(true).Count());

      var status1 = issue.fields.status.name;

      var ticketTrans = await jiraTicket.GetIssueTransitionsAsync();
      Trace.TraceInformation(new { ticketTrans }.ToJson());
      var transNext = ticketTrans.transitions.Last();
      var comment2 = "trans comment 1".LockIt();
      await jiraTicket.PostIssueTransitionAsync(transNext.name, comment2, false, null);
      ticket = await ticket.GetIssueAsync();
      var status2 = ticket.Value.fields.status.name;
      Assert.AreEqual(transNext.to.name, status2);
      Assert.IsTrue(ticket.Value.IsLocked);
      Assert.AreEqual(comment2, ticket.Value.fields.comment.comments.Last().body);
      var history = await jiraTicket.GetIssueTransitionHistoryAsync();
      Trace.TraceInformation(history.ToJson());
      Assert.IsTrue(
        new[] { status1, status2 }
        .SequenceEqual(history.TakeLast(1)
        .SelectMany(h => new[] { h.FromState, h.ToState })));
      issue = (await jiraTicket.GetIssueAsync()).Value;
      Assert.AreEqual("Yes", issue.fields.custom["Accepted"][0] + "");

      string propKey = "counter-up", propValue = "1";
      IList<IssueTransitions.Transition> transitions = issue.transitions;
      Console.WriteLine(transitions);
      Assert.IsTrue(transitions.Any());
      Passager.ThrowIf(() => !transitions.Any(trans => trans.Properties.Any(prop => prop.key == propKey && prop.value == propValue)));




      transitions.ForEach(trans => trans.PropertiesGetter = LazyMe(() => Task.FromResult(new Workflow.Transition.Property[0])));

      await ticket.FillIssueTransitionProperties(transitions);
      Passager.ThrowIf(() => issue.SmsTransition(Rest.SmsValue.Next, true) == null);

      ExceptionAssert.Propagates<Rest.TransitionPropertyNotFound>(
        () => Passager.ThrowIf(() => issue.SmsTransition("bad value", true) == null),
        exc => Debug.WriteLine(exc)
        );
      Passager.ThrowIf(() => issue.SmsTransition("bad value", false) != null);
      //var props = await TransProps(ticket,(workflow, transition,properties) =>  new { workflow, transition = transition.name, properties = properties.Select(tp => new { tp.key, tp.value }) });
      //Console.WriteLine(props.ToJson());
      //Assert.IsTrue(props.Any(x => x.properties.Any(prop => prop.key == "sms" && prop.value == "next")));
    }
    static Lazy<T> LazyMe<T>(Func<T> func) { return new Lazy<T>(func); }

    [TestMethod]
    public async Task GetTicketTransitions() {
      var rm = new JiraRest<Rest.JqlFilter>(new Rest.JqlFilter {
        MaxResults = 1,
        Jql = new Rest.JqlFilter.JQL {
          Project = projectKey,
          IssueType = issueType,
          FieldsQuery = new[] { "Status = \"OPEN\"", Rest.JqlFilter.ExactDateFilter("updated", DateTime.Parse("2016-08-15")) }
        }
      });
      var ticketKey = (await rm.GetTicketsAsync()).Value.issues.First().key;
      var ticket = await ticketKey.ToJiraTicket().GetIssueAsync();
      Trace.TraceInformation(ticket.ToJson());
      rm.Value.Jql.IssueType += "2";
      var error = new Func<Task>(() =>
       rm.GetTicketsAsync((rm2, rt) => {
         ExceptionDispatchInfo.Capture(rm2.Value).Throw();
         throw new Exception();
       }));
      await ExceptionAssert.Propagates<HttpResponseMessageException>(error, exc => {
        exc.Json.Contains(rm.Value.Jql.IssueType);
      });
    }
    [TestMethod]
    public async Task GetIssueAsync() {
      var ticket = await "DIT-19314".ToJiraTicket().GetIssueAsync();
      Trace.WriteLine(ticket);
    }
    [TestMethod]
    public async Task FastForwardTransitions_M() {
      var ticket = (await "PIZ-109".ToJiraTicket().GetIssueAsync()).Value.FastForwardTransitions(false);
      Trace.TraceInformation(ticket.ToString());
    }
    [TestMethod]
    public async Task TransitionHistory_CreateIssue() {
      var fields = new Dictionary<string, object> { ["Account Number"] = "RiskMgmt" };
      var issue = (await RestMonad.Empty()
        .PostIssueAsync("DIT", "Fix Data", IssueClasses.Issue.DoSkip("Test Create Issue History"),
        "", "", "", null, fields, "", null, null)).Value;
      var error = new Func<Task>(async () => (await issue.key.ToJiraTicket().GetIssueAsync()).Value.TransitionHistory());
      await ExceptionAssert.Propagates<PassagerException>(error, exc => {
        Passager.ThrowIf(() => !exc.Message.Contains("issue.changelog == null"));
      });
      var history = (await issue.key.ToJiraTicket().GetIssueAsync(Rest.IssueExpander.changelog)).Value.TransitionHistory().Single();
      Assert.AreEqual(IssueClasses.Issue.START_STATE, history.FromState);
      Assert.AreEqual(issue.fields.status.name, history.ToState);
      await issue.key.ToJiraTicket().PostIssueTransitionAsync("", false);
      var histories = (await issue.key.ToJiraTicket().GetIssueAsync(Rest.IssueExpander.changelog)).Value.TransitionHistory().Counter(2).ToArray();
      Console.WriteLine(histories.ToJson());
      history = histories.First();
      Assert.AreEqual(IssueClasses.Issue.START_STATE, history.FromState);
      Assert.AreEqual(issue.fields.status.name, history.ToState);
      await issue.key.ToJiraTicket().DeleteIssueAsync();
    }

    //[TestMethod]
    public async Task GetTicketsTransitionNext() {
      var rm = new JiraRest<Rest.JqlFilter>(new Rest.JqlFilter {
        MaxResults = 1,
        Jql = new Rest.JqlFilter.JQL {
          Project = "OPS",
          IssueType = "CIP - Review",
          FieldsQuery = new[] { "Status = \"OPEN\"", "created >= 2016-08-14 AND created >= 2016-08-15" }
        }
      });
      try {
        var test = (await rm.GetTicketsAsync()).Value.issues.Select(i => i.key)
          .Select(async ticketKey => {
            var transition = await ticketKey.ToJiraTicket().GetIssueTransitionNextAsync();
            Assert.IsNotNull(transition);
            Trace.TraceInformation(transition.ToJson());
          })
          .DefaultIfEmpty(async () => await Task.Run(() => {
            Assert.Inconclusive("No issues found to test for " + rm.Value.ToJson());
          }));
        Task.WaitAll(test.ToArray());
      } catch (Exception exc) {
        Trace.TraceInformation(exc.ToString());
        throw;
      }
    }
    [TestMethod]
    public async Task SearchTest() {
      var fieldName = "Accepted";
      Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<SearchResult<IssueClasses.Issue[]>>> onError = (exc, t) => {
        Trace.WriteLine(exc.Value);
        ExceptionDispatchInfo.Capture(new Exception(exc.Value + "")).Throw();
        throw exc.Value;
      };
      var issues = await RestMonad.Empty().Search("SUI", "Questionnaire", "Questionnaire Rejected", 1000, false, onError, new[] { fieldName });
      Console.WriteLine(string.Join("\n", issues.Select(i => i.ToJson())));
      Assert.IsTrue(issues.Any());
      return;
      issues.Select(issue => issue.fields.custom)
        .ForEach(c => c.Counter(1).ForEach(kv => {
          Assert.AreEqual(fieldName, kv.Key);
          kv.Value.Counter(1).Cast<string>().ForEach(v => Assert.IsTrue(v == "Yes" || v == "No"));
        }));
      var accepted = issues[0].fields.custom[fieldName][0] + "" == "Yes" ? "No" : "Yes";
      //var field = new Dictionary<string, object> { { fieldName, accepted } };
      await issues[0].key.ToJiraTicket().PutIssueAsync(null, new object[] { fieldName, accepted });
      var issue2 = (await issues[0].key.ToJiraTicket().GetIssueAsync()).Value;
      Assert.AreEqual(accepted, issue2.fields.custom[fieldName][0]);
    }

    [TestMethod]
    public async Task SearchIssue() {
      var res = await Rest.SearchIssueAsync("hlp-323");
      Assert.AreEqual(1, res.Value.Count);
      var issue = await res.Switch(res.Value.First()).GetIssueAsync();
      Assert.AreEqual("HLP-323", issue.Value.key);
      res = await Rest.SearchIssueAsync("hlps-323");
      Assert.AreEqual(0, res.Value.Count);
      //Console.WriteLine(issue);
    }

  }
}
