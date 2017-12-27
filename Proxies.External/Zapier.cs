using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions;

namespace Sms {
  public static class Zapier {
    public static async Task<HttpResponseMessage> SendSms(Uri serviceRestPoint, string phone, string body, params string[] nameValuePairs) {
      var values = new Dictionary<string, object>();
      Passager.ThrowIf(() => nameValuePairs.Length % 2 != 0);
      nameValuePairs.Buffer(2).ForEach(b => values.Add(b[0], b[1]));
      return await SendSms(serviceRestPoint, phone, body, values);
    }
    public static async Task<HttpResponseMessage> SendSms(Uri serviceRestPoint, string phone, string body, IDictionary<string, object> values) {
      var rest = new HttpClient();
      rest.BaseAddress = serviceRestPoint;
      var e = new { phone, body }.ToExpando();
      if (values != null)
        values.ForEach(value => e.AddOrUpdate(value.Key, value.Value + ""));
      var res = await rest.PostAsJsonAsync("", e);
      res.EnsureSuccessStatusCode();
      return res;
    }
  }
}
