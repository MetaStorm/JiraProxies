using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jira.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;
using Newtonsoft.Json;
using CommonExtensions;
using System.Diagnostics;
using static Jira.Json.IssueClasses;

namespace Jira.Json.Tests {
  [TestClass()]
  public class FieldTests {
    [TestMethod]
    public void IsSms() {
      var pattern = Comment.smsPattern;
      Assert.IsFalse(Regex.Match("sms", pattern).Success);
      var sms = " sms:[~dimok]";
      Assert.AreEqual(sms, Regex.Match(sms, pattern).Value);
      Assert.AreEqual("", new IssueClasses.Comment { body = "dimok" }.SmsName);
      Assert.AreEqual("dimok", new IssueClasses.Comment { body = sms }.SmsName);
      Assert.IsTrue( new IssueClasses.Comment { body = sms }.IsSms);
      Assert.IsTrue( new IssueClasses.Comment { body = " sms\n}}" }.IsSms);
      Assert.IsTrue( new IssueClasses.Comment { body = " sms" }.IsSms);

    }
    [TestMethod]
    public void DeserializeToDynamic() {
      dynamic d = new Dictionary<string, object> { { "Dimok", "Dimon1" } }.AddProperty(new { number = 1000, str = "string", array = new[] { 1, 2, 3, 4, 5, 6 } });
      d.dimok = "dimon";
      Console.WriteLine(JsonConvert.SerializeObject(d));
      Assert.AreEqual("{\"number\":1000,\"str\":\"string\",\"array\":[1,2,3,4,5,6],\"Dimok\":\"Dimon1\",\"dimok\":\"dimon\"}", JsonConvert.SerializeObject(d));
    }
    [TestMethod()]
    public void ValueFactoryTest() {
      var field = new Field { schema = new Field.Schema { type = "string" } };
      {
        var value = field.ValueFactory("DImok");
      Assert.AreEqual(value, "DImok");
      }
      {
        field.schema.type = "date";
        var d = DateTime.Now;
        var value = field.ValueFactory(d);
        Assert.AreEqual(value, d.Date.ToString("yyyy-MM-dd"));
      }
      {
        field.schema.type = "datetime";
        var d = DateTime.Now;
        var value = field.ValueFactory(d);
        Assert.AreEqual(DateTime.Parse(value + "").Date, d.Date);
      }
      {
        field.schema.type = "user";
        var value = (User)field.ValueFactory("Dimok");
        Assert.AreEqual(value.name, "Dimok");
      }
      field.schema.type = "XXX";
      CommonExtensions.ExceptionAssert.Propagates<InvalidEnumArgumentException>(() => field.ValueFactory(""));
    }
  }
}
