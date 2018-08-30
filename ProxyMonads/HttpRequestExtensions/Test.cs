using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Wcf.ProxyMonads {
  static class Test {
    static async Task TestAsync() {
      var handler = new TimeoutHandler {
        DefaultTimeout = TimeSpan.FromSeconds(10),
        InnerHandler = new HttpClientHandler()
      };

      using(var cts = new CancellationTokenSource())
      using(var client = new HttpClient(handler)) {
        client.Timeout = Timeout.InfiniteTimeSpan;

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:8888/");

        // Uncomment to test per-request timeout
        //request.SetTimeout(TimeSpan.FromSeconds(5));

        // Uncomment to test that cancellation still works properly
        //cts.CancelAfter(TimeSpan.FromSeconds(2));

        using(var response = await client.SendAsync(request, cts.Token)) {
          Console.WriteLine(response.StatusCode);
        }
      }
    }
  }
}