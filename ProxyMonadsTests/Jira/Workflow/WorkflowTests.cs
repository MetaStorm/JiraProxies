using CommonExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProxyMonads.Jira;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NVT = System.ValueTuple<string, string>;

namespace ProxyMonads.Jira.Tests {
  [TestClass()]
  public class Workflow {
    #region Workflow Defaults
    static (string name, string value)[][] defaultActionConditions = new[] {
        new[] { ("class.name", "com.atlassian.jira.workflow.condition.AllowOnlyAssignee") },
        new[] { ("class.name", "com.atlassian.jira.workflow.condition.AllowOnlyReporter") },
        new[] {
          ("hidRolesList", "Administrators@@"),
          ("class.name", "com.googlecode.jsu.workflow.condition.UserIsInAnyRolesCondition")
        }
      };
    static (string name, string value)[][] defaultPostFunctions = new[] {
        new[] { ("class.name", "com.atlassian.jira.workflow.function.issue.UpdateIssueStatusFunction") },
        new[] { ("class.name", "com.atlassian.jira.workflow.function.misc.CreateCommentFunction") },
        new[] { ("class.name", "com.atlassian.jira.workflow.function.issue.GenerateChangeHistoryFunction") },
        new[] { ("class.name", "com.atlassian.jira.workflow.function.issue.IssueReindexFunction") },
        new[] {
          ("eventTypeId", "13"),
          ("class.name", "com.atlassian.jira.workflow.function.event.FireIssueEventFunction")
        }
      };
    static (string name, string value)[][] resolutionPostFunction = new[] {
        new[] {
          ("field.name", "resolution"),
          ("full.module.key", "com.atlassian.jira.plugin.system.workflowupdate-issue-field-function"),
          ("field.value", "10000"),
          ("class.name", "com.atlassian.jira.workflow.function.issue.UpdateIssueFieldFunction")
        }
      };
    #endregion

    [TestMethod()]
    public void WorkflowCreate() {
      var wfAuthor = "ipbldit";
      var doc = new XDocument(
        new XDeclaration("1.0", "UTF-8", null),
        new XDocumentType("workflow", "-//OpenSymphony Group//DTD OSWorkflow 2.8//EN", "http://www.opensymphony.com/osworkflow/workflow_2_8.dtd", null)
      );
      var wf = new XElement("workflow",
        BuildMetaJira("update.author.key", wfAuthor),
        BuildMetaJira("updated.date", DateTime.Now.ToEpoch()));

      // Build Initial Action
      wf.Add(BuildInitialActions());

      int actionIdCounter = 11;
      int stepId = 1, statusId = 1, nextStepId = 2;
      // Build Steps
      wf.Add(BuildElement("steps",
        BuildStep(stepId, "Open Me", statusId, actionIdCounter++, nextStepId),
        BuildStep(2, "I Am Closed", 6))
        );
      doc.Add(wf);
      Console.WriteLine(doc.Declaration + "\n" + doc);
    }


    private static XObject BuildStep(int stepId, string stepName, int statusId, params XObject[] actions) =>
      BuildElement("step", new[] { ("id", stepId + ""), ("name", stepName) }, BuildMetaJiraId("status", statusId)).L(actions);
    private static XObject BuildStep(int stepId, string stepName, int statusId, int actionId, int nextStepId) =>
      BuildStep(stepId, stepName, statusId,
        BuildElement("actions",
        BuildDoneAction(actionId).L(UnconditionalResult(nextStepId).ToResults()))
        );
    private static XObject BuildInitialActions() {
      var createPostFunctions = new[] {
        new[] { ("class.name", "com.atlassian.jira.workflow.function.issue.IssueCreateFunction") },
        new[] { ("class.name", "com.atlassian.jira.workflow.function.issue.IssueReindexFunction") },
        new[] {
          ("eventTypeId", "1"),
          ("class.name", "com.atlassian.jira.workflow.function.event.FireIssueEventFunction")
        }
      }.Select(BuildFunction);

      var ia = new XElement("initial-actions",
        BuildAction(1, "Create",
        BuildValidators(BuildCreateIssueValidator()),
        new XElement("results"
        , BuildElement("unconditional-result", OldStatusStatus("null", "open", 1)
        , new XElement("post-functions", createPostFunctions)))
        ));
      return ia;
    }

    private static XObject BuildConditions((string name, string value)[][] args) {
      return BuildElement("conditions", ("type", "OR"), args.Select(BuildCondition).ToArray());
    }
    #region Custom Builders
    private static XObject BuildCreateIssueValidator() =>
      BuildValidator(("permission", "Create Issue"), ("class.name", "com.atlassian.jira.workflow.validator.PermissionValidator"));
    private static XObject UnconditionalResult(int nextStepId, params XObject[] elements) =>
      BuildElement("unconditional-result", OldStatusStatus("null", "null", nextStepId), BuildElement("post-functions", BuildFunctions(defaultPostFunctions)));

    #endregion

    #region Generic Builders
    #region BuildAction
    private static XObject BuildDoneAction(int actionId) => BuildAction(actionId, "Done", defaultActionConditions);
    private static XObject BuildAction(int id, string name, (string name, string value)[][] conditions) =>
      BuildElement("action", new[] { ("id", id + ""), ("name", name) }
      , BuildConditions(conditions).ToRestrictTo()).F(BuildMetaJira("description", ""));
    private static XObject BuildAction(int id, string name, params XObject[] elements) => BuildElement("action", new[] { ("id", id + ""), ("name", name) }, elements);
    #endregion

    private static XObject BuildValidators(params XObject[] validators) => new XElement("validators", validators);
    private static XObject BuildValidator(params (string nane, string value)[] args) => BuildWithArgs("validator", args);
    private static XObject BuildFunction(params (string name, string value)[] args) => BuildWithArgs("function", args);
    private static XObject BuildCondition(params (string name, string value)[] args) => BuildWithArgs("condition", args);
    private static XObject[] BuildFunctions(params (string name, string value)[][] argss) => argss.Select(args => BuildWithArgs("function", args)).ToArray();

    private static XObject BuildWithArgs(string name, params (string name, string value)[] args) => BuildElement(name, ("type", "class"), BuildArgs(args));
    private static XObject[] BuildArgs((string nane, string value)[] args) => args.Select(arg => BuildElement("arg", arg.value, new[] { ("name", arg.nane) })).ToArray();

    #region BuildElement
    private static XObject BuildElement(string name, params XObject[] elements) => BuildElement(name, new(string name, string value)[0], elements);
    private static XObject BuildElement(string name, (string name, string value) attribute, params XObject[] elements) => BuildElement(name, new[] { attribute }, elements);
    private static XObject BuildElement(string name, (string name, string value)[] attributes, params XObject[] elements) => new XElement(name, attributes.Select(t => (XObject)new XAttribute(t.name, t.value)).Concat(elements));
    private static XObject BuildElement(string name, string value, (string name, string value)[] attributes, params XObject[] elements) {
      var e = (XElement)BuildElement(name, attributes, elements);
      e.SetValue(value);
      return e;
    }
    private static (string name, string value)[] OldStatusStatus(string oldStatus, string status, int stepId) =>
      new[] { (name: "old-status", value: oldStatus), (name: "status", value: status), (name: "step", value: stepId + "") };
    #endregion
    #region meta
    private static XObject BuildMetaJira(string text, object value) => BuildMeta("jira." + text, value);
    private static XObject BuildMetaJiraId(string text, object value) => BuildMetaJira(text + ".id", value);
    private static XObject BuildMeta(string metaName, object value) {
      var meta = new XElement("meta");
      meta.Add(new XAttribute("name", metaName));
      meta.Value = value + "";
      return meta;
    }
    #endregion
    #endregion

  }
  static class BuildWorkflowMixins {
    public static XObject F(this XObject x, XObject content) => x.F(new[] { content });
    public static XObject F(this XObject x, IEnumerable<XObject> content) {
      ((XElement)x).AddFirst(content);
      return x;
    }
    public static XObject L(this XObject x, XObject content) => x.L(new[] { content });
    public static XObject L(this XObject x, IEnumerable<XObject> content) {
      ((XElement)x).Add(content);
      return x;
    }
    public static XObject ToResults(this XObject x) => x.To("results");
    public static XObject ToRestrictTo(this XObject x) => x.To("restrict-to");
    public static XObject ToActions(this XObject x) => x.To("actions");
    public static XObject To(this XObject x, string parent) => new XElement(parent, x);
  }
}