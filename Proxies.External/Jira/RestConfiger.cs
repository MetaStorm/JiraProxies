using CommonExtensions;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Wcf.ProxyMonads;

namespace Jira {
  [JiraSettings]
  [JiraConfig]
  public class RestConfiger : Foundation.Config<RestConfiger> {
    public static string UserWildcard => KeyValue() ?? ".";
    [Foundation.CustomConfig.ConfigValue]
    public static int[] WorkflowSchemaIds {
      get {
        return WorkflowSchemaIdsProvider?.Invoke() ?? KeyValue<string>().Split(',').Select(int.Parse).ToArray();
      }
    }
    public static Func<int[]> WorkflowSchemaIdsProvider;

    protected override async Task<ExpandoObject> _RunTestAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
      return await TestHostAsync(parameters, async (p, m) => {
        var b = await base._RunTestAsync(parameters, merge);
        var pitw = (await ProjectIssueTypeWorkflowProvider())
        .Select(x => x.Value.Select(y => new { p = x.Key, it = y.Key, w = y.ToArray() }).ToArray())
        .Take(1).ToArray();
        Passager.ThrowIf(() => pitw.IsEmpty());
        {
          var pitws = (await GetProjectIssueTypeWorkflowAsync())
          .SelectMany(x => x.Value.Select(y => new { project = x.Key, issueType = y.Key, workflows = y.ToArray() })).ToArray();
          var pitwsEmpty = pitws.Where(x => x.workflows.IsEmpty()).ToArray();
          Passager.ThrowIf(() => pitwsEmpty.Any(), " " + pitwsEmpty.ToJson());
          if(false) {
            var issueTypeEx = new[] { "task", "sub-task" };
            var pitwsMany = pitws.Where(x => x.workflows.Length > 1 && !issueTypeEx.Contains(x.issueType.ToLower())).ToArray();
            Passager.ThrowIf(() => pitwsMany.Any(), " " + pitwsMany.ToJson());
          }
        }

        var users = (await (
          from rm in RestMonad.Empty().GetMySelfAsync()
          from rm2 in rm.GetUsersWithGroups(rm.Value.name)
          from ug in rm2.Value.Where(u => u.groups.items.Any(g => g.name == "jira-administrators"))
          select ug)
          ).ToArray();
        Passager.ThrowIf(() => users.IsEmpty());
        return new ExpandoObject().Merge(GetType().FullName, b.Merge(new { pitw, users }));
      }, o=> {
        var exc = o as Exception;
        if (exc != null)
          ExceptionDispatchInfo.Capture(exc).Throw();
        else throw new Exception(o.ToJson());
      }, merge);
    }


    private static Func<Task<Dictionary<string, ILookup<string, string>>>> _projectIssueTypeWorkflowProvider;
    public static Func<Task<Dictionary<string, ILookup<string, string>>>> ProjectIssueTypeWorkflowProvider {
      get { return _projectIssueTypeWorkflowProvider; }
      set {
        if (_projectIssueTypeWorkflowProvider != null && _projectIssueTypeWorkflowProvider != value)
          throw new Exception($"{nameof(ProjectIssueTypeWorkflowProvider)} is attempted to re-assign to {value}.");
        _projectIssueTypeWorkflowProvider = value;
      }
    }
    public static Task<ILookup<string, string>> IssueTypeWorkflows;
    public static Task<Dictionary<string, string[]>> ProjectIssueTypes;
    public static Func<Task> OnInit { get; set; } = () => Task.FromResult(true);
    static RestConfiger() {
      var tcs = new TaskCompletionSource<ILookup<string, string>>();
      var tcs2 = new TaskCompletionSource<Dictionary<string, string[]>>();
      IssueTypeWorkflows = tcs.Task;
      ProjectIssueTypes = tcs2.Task;
      Task.Delay(1).ContinueWith(async t => {
        var rm = RestMonad.Empty();
        try {
          var itws = await FetchIssueTypeWorkflows(rm);
          tcs.SetResult(itws.Value);
        }
        catch (Exception exc) {
          tcs.SetException(exc);
        }
        try {
          var pit = await rm.GetProjectIssueTypesAsync().WithError();
          tcs2.SetResult(pit.value.Value);
        }
        catch (Exception exc) {
          tcs2.SetException(exc);
        }
      });
    }
    public static async Task<Dictionary<string, ILookup<string, string>>> GetProjectIssueTypeWorkflowAsync() {
      return GetProjectIssueTypeWorkflow(await RestConfiger.IssueTypeWorkflows, await RestConfiger.ProjectIssueTypes);
    }
    static Dictionary<string, ILookup<string, string>> GetProjectIssueTypeWorkflow(ILookup<string, string> itwfs, Dictionary<string, string[]> projectIssues) {
      var itwfs2 = itwfs.ToDictionary(l => l.Key, l => l.Select(x => x).ToArray());
      return (from pits in projectIssues
              from it in pits.Value
              join itw in itwfs on it equals itw.Key
              group itw by pits.Key into gs
              from ws in gs
              from w in ws
              select new { p = gs.Key, it = ws.Key, w }
              )
              .ToLookup(l => l.p, l => new { l.it, l.w }, StringComparer.OrdinalIgnoreCase)
              .ToDictionary(l => l.Key, l => l.ToLookup(l1 => l1.it, l1 => l1.w), StringComparer.OrdinalIgnoreCase);
    }

    public static async Task<string> ProjectIssueTypeWorkflow(string project, string issueType) {
      Passager.ThrowIf(() => project.IsNullOrWhiteSpace());
      Passager.ThrowIf(() => issueType.IsNullOrWhiteSpace());
      Passager.ThrowIf(() => ProjectIssueTypeWorkflowProvider == null);
      var pitws = await ProjectIssueTypeWorkflowProvider();
      Passager.ThrowIf(() => !pitws.ContainsKey(project), " {0}", new { project, not = "found" });
      var x = pitws[project][issueType];
      return x
        .Counter(1
        , new Exception(new {project, issueType, error = "No workflows found" } + "")
        , new Exception(new {project, issueType, error = "Multiple workflows found", workflows = x.ToJson(false) } + ""))
        .Single();
    }
    public static async Task<ILookup<string, string>> ResetIssueTypeWorkflows() {
      await ResetProjectIssueTypes();
      return await (IssueTypeWorkflows = (from itws in FetchIssueTypeWorkflows(RestMonad.Empty())
                                          select itws.Value));
    }
    static async Task<Dictionary<string, string[]>> ResetProjectIssueTypes() {
      return await (ProjectIssueTypes = (from itws in RestMonad.Empty().GetProjectIssueTypesAsync()
                                         select itws.Value));
    }
    static async Task<RestMonad<ILookup<string, string>>> FetchIssueTypeWorkflows(RestMonad rm) {
      var workflows = (await rm.GetWorkflowShemeWorkflowsAsync()).Value.Concat().ToArray();
      //Console.WriteLine(workflows.ToJson());
      var issueTypes = (await rm.GetIssueTypesAsync()).Value;
      //Console.WriteLine(issueTypes.ToJson());
      var ret = (
        from wf in workflows
        from itId in wf.issueTypes
        join it in issueTypes on itId equals it.id
        select new { it.name, wf.workflow } into gs
        group gs by gs.name into itw
        from x in itw.Distinct(x => x.workflow)
        orderby x.name, x.workflow
        select x
        ).ToLookup(x => x.name, x => x.workflow, StringComparer.OrdinalIgnoreCase);
      await OnInit();
      return ret.ToRestMonad(rm);
    }
  }
}
