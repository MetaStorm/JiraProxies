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
using System.Text.RegularExpressions;
using System.Net;

namespace Proxies.ExternalTests {
  [TestClass]
  public class WorkflowTest {
    static int[] _workflowSchemeIds;
    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context) {
      _workflowSchemeIds = new RestMonad().GetWorlflowSchemeIdsAsync().GetAwaiter().GetResult().Value.Select(int.Parse).ToArray();
      RestConfiger.WorkflowSchemaIdsProvider = () => _workflowSchemeIds;
      RestConfiger.ProjectIssueTypeWorkflowProvider = RestConfiger.GetProjectIssueTypeWorkflowAsync;
    }
    [TestMethod]
    public async Task GetWorkflows() {
      var workflows = (await RestMonad.Empty().GetWorkflows()).Value;
      Console.WriteLine(workflows.ToJson());
    }
    [TestMethod]
    public async Task GetWorkflowSchemas() {
      var itws = await RestConfiger.IssueTypeWorkflows;
      Assert.IsTrue(itws.Any(), $"{nameof(RestConfiger.IssueTypeWorkflows)} is empty");
      Console.WriteLine(itws.ToJson());
      var project = "PIZ";// "ISD";
      var issueType = "Order Pizza";// "Access";
      var workflow = "Order Pizza";// "ISD - Access Request";
      var wf = await RestConfiger.ProjectIssueTypeWorkflow(project, issueType);
      Assert.AreEqual(workflow, wf);
      await ExceptionAssert.Propagates<Exception>(() => RestConfiger.ProjectIssueTypeWorkflow(project, "Task"), exc => {
        Assert.IsTrue(exc.Message.Contains("issueType = Task"));
      });
      await ExceptionAssert.Propagates<PassagerException>(() => RestConfiger.ProjectIssueTypeWorkflow("XXXXXXX", "YYY"), exc => {
        Assert.IsTrue(exc.Message.Contains("project = XXXXXXX"));
      });
      Assert.IsTrue((await RestConfiger.ResetIssueTypeWorkflows()).Any());
    }
    [TestMethod]
    public async Task GetWorkflowSchemasProvider_M() {
      Assert.Inconclusive();
      RestConfiger.WorkflowSchemaIdsProvider = () => new[] { 10093 };
      Console.WriteLine((await RestConfiger.IssueTypeWorkflows).ToJson());
      var wf = await RestConfiger.IssueTypeWorkflows;
      Assert.AreEqual(4, wf.Count());
      Assert.AreEqual("WorkFlowCNG", wf.First().Single());
    }
    [TestMethod]
    public async Task GetStatuses() {
      var statuses = (await RestMonad.Empty().GetStatuses()).Value;
      Console.WriteLine(statuses.ToJson());
      Assert.IsTrue(statuses.Length > 0);
    }
    [TestMethod]
    public async Task ProjectIssueTypeWorkflow() {
      var rt = await RestConfiger.RunTestAsync(null);
      var pitw = ((IEnumerable<object>)((IDictionary<string, object>)((IDictionary<string, object>)rt)["Jira.RestConfiger"])["pitw"]).ToArray();
      Console.WriteLine(rt.ToJson());
    }

    [TestMethod]
    public async Task DeleteStatus_M() {
      Assert.Inconclusive();
      var js = await Rest.DeleteStatusAsync(11492);
      Console.WriteLine(js.ToJson());
    }
    [TestMethod]
    public async Task PostStatusNew() {
      var statusName = "Test Status " + CommonExtensions.Helpers.RandomStringUpper(4);
      var js = await Rest.PostStatusAsync(statusName, "Delete Me");
      Console.WriteLine(new { statusName, response = js.ToJson() });
    }

    //
    [TestMethod]
    public async Task PostStatusOld() {
      await TestAlreadyExists(Rest.PostStatusAsync("Step1", "Delete Me"));
    }
    //
    [TestMethod]
    public async Task PostStatusLong() {
      var status = "Review all client documents and IES transactions to confirm no contact has been made with client";
      try {
        await Rest.PostStatusAsync(status, "Delete Me");
      } catch(Exception exc) {
        if(exc.Message.Contains("too long"))
          return;
        throw;
      }
      Assert.Fail();
    }
    [TestMethod]
    public async Task PostStatus60_M() {
      Assert.Inconclusive();
      var status = "Review all client documents and IES transactions to confirm no contact has been made with client";
      await Rest.PostStatusAsync(status.Substring(0, 60), "Delete Me");
    }

    [TestMethod]
    public async Task PostWorkflow() {
      await TestAlreadyExists(Rest.PostWorkflowAsync("jira", "Delete Me", ""));
    }
    [TestMethod]
    public async Task DeleteWorkflow_M() {
      Assert.Inconclusive();
      await Rest.DeleteWorkflowAsync("IM2: Process Management Workflow (2)");
    }

    private static async Task TestAlreadyExists(Task t) {
      await ExceptionAssert.Propagates<HttpResponseMessageException>(t,
        exc => Assert.AreEqual(ResponceErrorType.AlreadyExists, exc.ResponseErrorType));
    }

    [TestMethod]
    public async Task GetWorkflowShemeWorkflowAsync() {
      var ret = await RestMonad.Empty().GetWorkflowShemeWorkflowAsync(RestConfiger.WorkflowSchemaIds[0]);
      Console.WriteLine(ret.ToJson());
      Assert.AreEqual(2, ret.Value.Count());
    }
  }
}
