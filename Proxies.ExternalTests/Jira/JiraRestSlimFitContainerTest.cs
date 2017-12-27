using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wcf.ProxyMonads;
using Jira;
using Jira.Json;
using CommonExtensions;
using System.Threading.Tasks;

namespace Proxies.ExternalTests.Jira {
  [TestClass]
  public class JiraRestSlimFitContainerTest {
    [TestMethod]
    public async Task JiraSelfie() {
      await new slim_fit_tests.JiraRest().JiraSelfie();
    }
    [TestMethod]
    public void JiraLastTicket() {
      Console.WriteLine(new slim_fit_tests.JiraRest().LastTicket());
    }
    [TestMethod]
    public async Task JiraNewTicket() {
      try {
        Console.WriteLine(await new slim_fit_tests.JiraRest().NewTicket());
      } catch (Exception exc) {
        Console.WriteLine(string.Join("\n", exc.Messages()));
        Console.WriteLine(exc + "");
        Assert.IsTrue(exc.ToMessages().Contains("Applications And Services"));
        return;
      }
      Assert.Fail("Should not be here.");
    }
  }
}
namespace slim_fit_tests {
  public class JiraRest {

    #region Life Actions
    #region Tests
    public async Task JiraSelfie() {
      RestMonad.UseSessionID = true;
      //var self = Helpers.externalPost().GetMySelfAsync().Result;
      var self = await RestMonad.Empty().GetMySelfAsync();
      Assert.AreEqual("ice_admin", self.UserName.ToLower());
    }
    #endregion
    #endregion

    public async Task<string> NewTicket() {
      //var self = (await RestMonad.Empty().GetMySelfAsync()).Value.name;
      var newIssue = Jira.Json.JiraNewIssue
        .Create("HLP", "Password Reset", "New Direct Test Ticket " + string.Format("{0:MM/dd/yy HH:mm:ss}", DateTime.Now)
        , "This ticket description does not have any useful information in it.",new[] { "Caribbean" });
      var cusomProps = new Dictionary<string, object> { 
      { "Applications And Services", "Assist CK" } ,
        {"Unique Key",DateTime.Now.Ticks+""},
        {"Applications List", "Miami"},
      //{"ioc_apps_assist_branches","BAHAMAS"} // this is another way
      //{"ioc_apps_assist_branches",new[]{"MIAMI"}}
      };
      var newTicketResult =  await(from rm in LocalHelpers.externalPost(newIssue).PostIssueAsync(cusomProps)
                              from rm1 in rm.GetIssueAsync()
                              select rm1.Value
                              );
      cusomProps.ForEach(kv => Assert.AreEqual(kv.Value, newTicketResult.ExtractCustomField<string>(kv.Key).Single()));
      return newTicketResult.key;
    }
    public string LastTicket() {
      return LocalHelpers.externalPost().GetLastTicket(true).Result.Value.issues.Select(i => i.key).DefaultIfEmpty("Not found").First();
    }
  }
  class LocalHelpers {
    #region Helpers
    [ProxyMonadsConfigAttribute]
    class JiraConfig : Foundation.Config<JiraConfig> {
      class ProxyMonadsConfigAttribute : Foundation.CustomConfig.ConfigFileAttribute { }
      public static string JiraServiceBaseAddress { get { return KeyValue(); } }
      public static string JiraPowerUser { get { return KeyValue(); } }
      public static string JiraPowerPassword { get { return KeyValue(); } }
    }
    static T externalPost<T, U>(U postValue) where T : RestMonad<U>, new() {
      return new T() { Value = postValue, BaseAddress = new Uri(JiraConfig.JiraServiceBaseAddress), UserName = JiraConfig.JiraPowerUser, Password = JiraConfig.JiraPowerPassword };
    }
    public static RestMonad<object> externalPost() { return externalPost<RestMonad<object>, object>(null); }
    public static JiraRest<T> externalPost<T>(T value) { return externalPost<JiraRest<T>, T>(value); }
    #endregion
  }
}
