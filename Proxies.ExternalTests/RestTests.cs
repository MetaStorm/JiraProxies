using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sms.Tests {
  [TestClass()]
  public class RestTests {
    [TestMethod()]
    public async Task SendSmsTest() {
      var phone = "+13057880763";//"+13057785580"
      await Zapier.SendSms(new Uri("https://zapier.com/hooks/catch/23ydgn/"), phone, "message with token", new Dictionary<string, object> {
        { "token", "AZK432HMTF4ADS" }
      });
      await Zapier.SendSms(new Uri("https://zapier.com/hooks/catch/23ydgn/"), phone, "message with token params", "token", "AZK432HMTF4ADS");
    }
  }
}