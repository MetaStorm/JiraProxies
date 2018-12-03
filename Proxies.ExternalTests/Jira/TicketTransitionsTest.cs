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
    private const string TEST_CATEGORY_MANUAL = "Manual";

    [TestMethod]
    public async Task ScreenDeleteByProject() {
      Assert.Inconclusive();
      await Rest.ScreenDeleteByProject("PM");
    }
    [TestMethod]
    [TestCategory("Manual")]
    public async Task ScreenDeleteById() {
      Assert.Inconclusive();
      await Rest.IsJiraDev();
      var screenIds = new[] {12316,
12317,
12318,
12410,
12411,
12412,
12413,
12414,
12415,
12416,
12417,
12418,
12419,
12420,
12421,
12422,
12425,
12426,
12427,
12428,
12429,
12430,
12431,
12434,
12435,
12436,
12437,
12440,
12441,
12442,
12443,
12444,
12445,
12446,
12447,
12448,
12449,
12450,
12451,
12452,
12453,
12454,
12455,
12456,
12457,
12458,
12459,
12460,
12461,
12462,
12463,
12464,
12465,
12466,
12467,
12468,
12469,
12470,
12471,
12472,
12473,
12474,
12475,
12476,
12477,
12478,
12479,
12480,
12481,
12482,
12483,
12484,
12485,
12486,
12487,
12488,
12489,
12490,
12491,
12492,
12493,
12494,
12495,
12496,
12497,
12498,
12499,
12500,
12501,
12502,
12503,
12504,
12505,
12506,
12507,
12508,
12509,
12510,
12511,
12512,
12513,
12514,
12515,
12516,
12517,
12518,
12519,
12520,
12521,
12522,
12523,
12524,
12525,
12526,
12527,
12528,
12529,
12530,
12531,
12532,
12533,
12534,
12535,
12536,
12537,
12538,
12539,
12540,
12541,
12542,
12543,
12544,
12545,
12546,
12547,
12548,
12549,
12550,
12551,
12552,
12553,
12554,
12555,
12556,
12557,
12558,
12559,
12560,
12561,
12562,
12563,
12564,
12565,
12566,
12567,
12568,
12569,
12570,
12571,
12572,
12573,
12574,
12575,
12576,
12577,
12578,
12579,
12580,
12581,
12582,
12583,
12584,
12585,
12586,
12587,
12588,
12589,
12590,
12591,
12592,
12593,
12594,
12595,
12596,
12597,
12598,
12599,
12600,
12601,
12602,
12603,
12604,
12605,
12606,
12607,
12608,
12609,
12610,
12611,
12612,
12613,
12614,
12615,
12616,
12617,
12618,
12619,
12620,
12621,
12622,
12623,
12624,
12625,
12626,
12627,
12628,
12629,
12630,
12631,
12632,
12633,
12634,
12635,
12710,
12711,
12712,
12713,
12714,
12715
};
      foreach(var id in screenIds)
        (await Rest.DeleteScreen(id).WithError()).SideEffect(t => {
          Debug.WriteLine(new { id, t.error, t.value });
        });
    }
    [TestMethod]
    public async Task GetScreensByProject() {
      var screens = (await RestMonad.Empty().GetScreensByProjectPrefix("DIT", "copy")).Value;
      Assert.IsTrue(screens.Length > 0);
    }
    [TestMethod]
    [TestCategory("Manual")]
    public async Task ProjectDestroy() {
      Assert.Inconclusive("To be used manually destroy the project");
      await Rest.IsJiraDev();
      var jo = await RestMonad.Empty().DestroyProjectAsync("AMEX");
      Console.WriteLine(jo.ToJson());
      Assert.IsTrue(jo.errors.IsEmpty());
    }
    [TestMethod]
    [TestCategory(TEST_CATEGORY_MANUAL)]
    public async Task ProjectCreate_M() {
      Assert.Inconclusive();
      var x = (await from lead in RestMonad.Empty().GetMySelfAsync()
                     from p in lead.CreateDefaultProjectAsync("Import5", "IM5", "Impotred Project\nDELETE_ME", lead.Value.name)
                     from d in p.DestroyProjectAsync(p.Value.key)
                     select new { p = p.Value, d.jObject, e = d.errors.Select(e => e + "").ToList() }
                     .SideEffect(se => Console.WriteLine(se.ToJson())));
      Assert.AreEqual(0, x.e.Count);
      Assert.AreEqual(0, x.jObject.Value<JArray>("errors").Values<string>().Count());
    }

    [TestMethod]
    public async Task ProjectsGet() {
      var rme = RestMonad.Empty();
      var projectsLookup = (await rme.GetProjectIssueTypesAsync()).Value;
      Console.WriteLine(projectsLookup.ToJson());
      Assert.AreEqual(true, projectsLookup.Any());
    }
    [TestMethod]
    public async Task RunTests() {
      var rc = await RestConfiger.RunTestAsync(null);
      Console.WriteLine(rc.ToJson());
      rc = await Rest.RunTestAsync();
      Console.WriteLine(rc.ToJson());
    }
    [TestMethod]
    public async Task GetUsers() {
      var rme = RestMonad.Empty();
      var users0 = await rme.GetUsersWithGroups(10);
      Assert.IsTrue(users0.Value.Any(), "users0.Any()");
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
    [TestCategory("Manual")]
    public async Task TicketTransitions() {
      Assert.Inconclusive();
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




      transitions.ForEach(trans => trans.PropertiesGetter = LazyMe(() => Task.FromResult((new Workflow.Transition.Property[0], (Exception)null))));

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
    [TestCategory("Manual")]
    public async Task FastForwardTransitions_M() {
      Assert.Inconclusive();
      var ticket = (await "PIZ-109".ToJiraTicket().GetIssueAsync()).Value.FastForwardTransitions(false);
      Trace.TraceInformation(ticket.ToString());
    }
    [TestMethod]
    [TestCategory("Manual")]
    public async Task SimpleTransition_M() {
      Assert.Inconclusive();
      await Core.IsJiraQA();
      if(false) {
        await "DIT-94609".ToJiraTicket().PostIssueTransitionAsync("done", IssueClasses.Issue.DoCode("Test by ICE", "white", "navy"), false, null);
        return;
      }
      var newIssue = JiraNewIssue.Create("DIT", "Pershing - Link Discrepancy", "Pershing - Link Discrepancy" + ": ICE Test");
      //var i = (await "DIT-94614".ToJiraTicket().GetIssueAsync());
      //var closeTrans = i.Value.FindTransitionByNameOrProperty("done", false);
      var issue = (await (from rm in newIssue.ToRestMonad().PostIssueAsync()
                          from i in rm.GetIssueAsync()
                          from closeTrans in i.Value.FindTransitionByNameOrProperty("done", false)
                          from t in i.Value.key.ToJiraTicket().PostIssueTransitionAsync(closeTrans, IssueClasses.Issue.DoCode("Test by ICE", "white", "navy"))
                          select i)).Single().Value;


      Trace.TraceInformation(new { issue } + "");
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
      } catch(Exception exc) {
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
