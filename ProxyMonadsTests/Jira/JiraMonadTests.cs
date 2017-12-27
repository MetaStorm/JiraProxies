using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wcf.ProxyMonads;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommonExtensions;
namespace Wcf.ProxyMonads.Tests {
  [TestClass()]
  public class JiraMonadTests {
    [TestMethod]
    public void JiraError() {
      var json = @"{
        'errorMessages': ['provet,devky!'],
        'errors': {
          'issuetype': 'valid issue type is required',
          'dimok': 'rules'
        }
      }";
      var errors = Wcf.ProxyMonads.JiraMonad.ParseJiraError(json);
      Assert.AreEqual(errors.Split('\n')[2], "dimok:rules");
    }
    [TestMethod()]
    public void JiraServiceBaseAddressTest() {
      Assert.AreEqual("http://usmrtpwjirq01:81/", JiraMonad.JiraServiceBaseAddress());
    }
    [TestMethod()]
    public void JiraMonadConfig() {
      Console.WriteLine(new slim_fit_tests.JiraMonadTest().RunTest());
    }
  }
}
namespace slim_fit_tests {
  public class JiraMonadTest {
    public async Task<string> RunTest() {
      return (await JiraMonad.RunTestAsync()).ToJson();
    }
  }
}