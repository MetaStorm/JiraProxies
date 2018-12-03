using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jira;
using System.Diagnostics;
using CommonExtensions;
using System.Net;
using Jira.Json;

namespace Wcf.ProxyMonads.Tests {
  [TestClass()]
  public class SessionCookieTests {
    [TestMethod()]
    public async Task PostUserSessionTest() {
      var username = JiraMonad.JiraPowerUser();
      // create a new session for username 
      var session = await RestMonad.Empty().PostAuthSession(username, JiraMonad.JiraPowerPassword());
      Trace.TraceInformation(session.ToJson());
      Assert.IsNotNull(session);
      // create cookie based on returned session
      var cookie = new Cookie(session.Value.session.name, session.Value.session.value);
      session.SessionID = cookie;
      try {
        await session.GetMySelfAsync();
      } catch (Exception exc) {
        Assert.IsTrue(exc.InnerException.Message.Contains("ApiAddress"));
      }
      var cookiedRest = RestMonad.Create(cookie);
      // get user info based on SessionID cookie
      var self = await cookiedRest.GetMySelfAsync();
      Trace.TraceInformation(self.ToJson());
      Assert.AreEqual(username.ToLower(), self.Value.key.ToLower());
      // test that SessionID cookie is still valid
      var authSession = await cookiedRest.GetAuthSession();
      Trace.TraceInformation(self.ToJson());
      Assert.AreEqual(username.ToLower(), authSession.Value.name.ToLower());
      Assert.IsTrue(authSession.Value.IsAuthenticated, "Session ID cookie is not valid");

      Trace.TraceInformation("**************** Create ticket *****************");
      const string createIssueComment = "Cookie authenticated REST API test";
      var custom = new Dictionary<string, object> {["Applications List"] = "Miami" };
      var ticket = await cookiedRest.PostIssueAsync("hlp", "Password Reset", IssueClasses.Issue.DoSkip("kill it"), null, "ipbldit", JiraMonad.JiraPowerUser(), new[] { "miami" }, custom, createIssueComment, null, null);
      Assert.AreEqual(username, ticket.Value.fields.creator.name, "username and ticket creator ain't no same.");
      Assert.AreEqual(createIssueComment, ticket.Value.fields.comment.comments.Last().body);
      Trace.TraceInformation(ticket.ToJson());
      const string resolvedComment = "Issue been resolved";
      var fastForwardTrans = ticket.Value.FastForwardTransitions();
      Trace.TraceInformation((await ticket.Value.key.ToJiraTicket().PostIssueTransitionAsync(fastForwardTrans.Single(), "", null)).Value.ToLog(ticket.Value.key));
      Trace.TraceInformation((await ticket.Value.key.ToJiraTicket().PostIssueTransitionAsync("resolve issue", resolvedComment, true, null)).Value.ToLog(ticket.Value.key));
      ticket = await ticket.Value.key.ToJiraTicket(cookie).GetIssueAsync();
      Assert.IsTrue(ticket.Value.IsLocked, new { ticket.Value.IsLocked } + "");
      Assert.AreEqual(resolvedComment.LockIt(), ticket.Value.fields.comment.comments.Last().body);
      await ticket.Value.key.ToJiraTicket().DeleteIssueAsync();
      // invalidate SessionID cookie
      Trace.TraceInformation("***************************************************************");
      cookie.Value = "32135465498613165749687";
      authSession = await RestMonad.Create(cookie).GetAuthSession();
      Trace.TraceInformation(authSession.ToJson());
      Assert.IsFalse(authSession.Value.IsAuthenticated);

    }
  }
}