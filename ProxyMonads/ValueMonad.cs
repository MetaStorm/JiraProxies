using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HAP = HtmlAgilityPack;
using Newtonsoft.Json;
using System.Net.Http;
using OAuth;
using System.Net.Http.Headers;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using Foundation;
using Foundation.Core;
using CommonExtensions;
using System.Net;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Wcf.ProxyMonads {
  #region RestMonad
  public class RestMonad {
    public static RestMonad Empty() { return new RestMonad(); }
    public static T Empty<T>() where T : RestMonad, new() { return new T(); }

    public string FullAddress() { return (BaseAddress == null ? "" : BaseAddress + "") + (ApiAddress ?? ""); }
    Uri _baseAddress;
    public Uri BaseAddress {
      get { return _baseAddress; }
      set { _baseAddress = value == null ? null : EnsurePath(value); }
    }
    string _apiAddress;
    public string ApiAddress {
      get { return _apiAddress; }
      set { _apiAddress = value; }
    }
    public string UserName { get; set; }

    public class PasswordClass {
      string _password;
      public PasswordClass(string p) {
        this._password = p;
      }
      public string GetValue() { return _password; }
      public override string ToString() {
        return "********";
      }
      public static implicit operator PasswordClass(string password) {
        return new PasswordClass(password);
      }
    }
    public PasswordClass Password { get; set; }
    public Cookie SessionID { get; set; }

    public bool HasSessionID { get { return SessionID != null && !string.IsNullOrWhiteSpace(SessionID.Value); } }

    public static bool UseSessionID {
      get {
        return _useSessionID;
      }
      set {
        _useSessionID = value;
      }
    }

    static bool _useSessionID;
    public static Func<Task> SetDefaultSessionID = () => Task.FromResult(false);
    public static ConcurrentDictionary<string, Task<string>> SessionIDs = new ConcurrentDictionary<string, Task<string>>();
    public static async Task<Cookie> GetUserCookie(string user) {
      Task<string> session;
      if (SessionIDs.TryGetValue(user, out session) && SessionIDs[user].Status == TaskStatus.RanToCompletion) {
        return CreateJiraSessionCookie(await SessionIDs[user]);
      }
      return null;
    }
    public static void ClearUserCookie(string user) {
      Task<string> v;
      SessionIDs.TryRemove(user, out v);
    }

    public RestMonad() { }
    public RestMonad(Cookie sessionID) {
      if (sessionID != null)
        this.SessionID = sessionID;
    }

    public RestMonad(Uri baseUri, Cookie sessionID = null) : this(sessionID) {
      this.BaseAddress = baseUri;
    }

    public RestMonad(string baseAddress, Cookie sessionID = null) : this(sessionID) {
      this.BaseAddress = new Uri(EnsurePath(baseAddress));
    }
    public RestMonad(string baseAddress, string apiAddress, string userName, string password)
      : this(new Uri(EnsurePath(baseAddress)), apiAddress, userName, password) {
    }
    public RestMonad(Uri baseUri, string apiAddress, string userName, string password) {
      this.BaseAddress = baseUri;
      this.ApiAddress = apiAddress;
      this.UserName = userName;
      this.Password = password;
    }

    static bool IsPathOk(string path) { return path.EndsWith("/"); }
    public static Uri EnsurePath(Uri uri) {
      Guard.NotNull(() => uri, uri);
      var path = uri.AbsoluteUri;
      return IsPathOk(path) ? uri : new Uri(EnsurePath(path));
    }
    public static string EnsurePath(string path) {
      return path + (IsPathOk(path) ? "" : "/");
    }
    /// <summary>
    /// Copy RestMonad to another RestMonad
    /// </summary>
    /// <typeparam name="TRestMonad"></typeparam>
    /// <param name="clone"></param>
    /// <returns></returns>
    public TRestMonad Copy<TRestMonad>(TRestMonad clone) where TRestMonad : RestMonad {
      clone.BaseAddress = this.BaseAddress;
      //clone.ApiAddress = this.ApiAddress;
      clone.UserName = this.UserName;
      clone.Password = this.Password;
      clone.SessionID = this.SessionID;
      return clone;
    }
    public static string ParseError(string json) {
      Func<string, string> cleanErorr = text => System.Text.RegularExpressions.Regex.Replace(text, "(\r\n)+", "$1").Trim(' ', '\r', '\n');
      Func<HAP.HtmlDocument> loadHtml = () => new[] { new HAP.HtmlDocument() }.Select(html => { html.LoadHtml(json); return html; }).First();
      Func<XDocument> loadXml = () => { try { return System.Xml.Linq.XDocument.Parse(json); } catch { return new XDocument(); } };
      IList<string> parseFault = loadXml().Descendants().Where(x => x.Name.LocalName == "Detail").Select(x => x.Value).ToArray();
      IList<string> parseHtml = new[] { loadHtml().DocumentNode.SelectNodes("//html//body") }
        .Where(c => c != null)
        .Select(n => Regex.Replace(Regex.Replace(n.First().InnerText, "<!--(.*?)-->", ""), "\\s{2,}", " "))//Regex.Replace(parseHtml[0],"\\s{2,}"," ")
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .DefaultIfEmpty(new { json } + "")
        .ToArray();

      var parsers = new[] { parseFault, parseHtml };
      var parsedErrors = parsers
        .SkipWhile(p => !p.Any())
        .Take(1);
      return cleanErorr(string.Join("\n", parsedErrors.SelectMany(a => a)));
    }
    public static string[] ParseJspError(string html) {
      var doc = new HAP.HtmlDocument();
      doc.LoadHtml(html);
      return doc.DocumentNode?.SelectNodes("//div[contains(@class,'error')]")?
        .Select(n => n?.InnerText.Trim())
        .Where(s => !s.IsNullOrWhiteSpace())
        .ToArray() ?? new string[0];
    }
    public static string FormatException<E>(E exception) where E : Exception {
      return exception.ToString().Replace("--->", "\n--->");
    }
    public static string Unwrap(string json) { return json.Substring(5, json.Length - 6); }
    public static JsonSerializerSettings JsonSerializerSettingsFactory() {
      return new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented };
    }
    public static string SerializeObject<T>(T o) { return JsonConvert.SerializeObject(o, JsonSerializerSettingsFactory()); }
    public static object DeserializeObject(string json) { return JsonConvert.DeserializeObject(json); }
    public static string FormatJson(string json) { return RestMonad.SerializeObject(RestMonad.DeserializeObject(json)); }
    public static AuthenticationHeaderValue MakeBasicAuthenticationHeader(string userName, string password) {
      return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", userName, password))));
    }
    public Func<string, PasswordClass, string, HttpClient> BuildCustomClientFactory() {
      Func<string, PasswordClass, string, HttpClient> customClient = (userName, password, baseAddress) => {
        var c = new HttpClient(new HttpClientHandler() { UseCookies = false }, true);
        if (BaseAddress == null) BaseAddress = new Uri(baseAddress);
        if (UserName == null) UserName = userName;
        if (Password == null) Password = password;
        c.BaseAddress = BaseAddress;
        if (!string.IsNullOrWhiteSpace(UserName))
          c.InitBasicAuthenticationHeader(UserName, Password.GetValue());
        return c;
      };
      return customClient;
    }
    public static string ToString(RestMonad rm) {
      return new { rm.BaseAddress, rm.ApiAddress, rm.UserName } + "";
    }
    public override string ToString() {
      return RestMonad.ToString(this);
    }
    public static RestMonad<T> Create<T>(T value) { return new RestMonad<T>(value); }
    public static RestMonad Create(Cookie sessionID) { return new RestMonad() { SessionID = sessionID }; }
    public static RestMonad CreateWithSession(string sessionID) { return new RestMonad() { SessionID = CreateJiraSessionCookie(sessionID) }; }

    public static Cookie CreateJiraSessionCookie(string sessionID) {
      return new Cookie("JSESSIONID", sessionID);
    }

  }
  #endregion

  public static class RestMonadExtensions {
    public static RestMonad<TResult> Select<T, TResult>(this RestMonad<T> self, Func<T, TResult> selector) {
      return selector(self.Value).ToRestMonad(self);
    }
    public static RestMonad<TResult> SelectMany<T, TA, TResult>(this RestMonad<T> self, Func<T, RestMonad<TA>> selector, Func<T, TA, TResult> resultSelector) {
      var first = self.Value;
      var second = selector(first).Value;
      return resultSelector(first, second).ToRestMonad(self);
    }

    public static RestMonad<TResult> SelectMany<T, TResult>(this RestMonad<T> lazy, Func<T, RestMonad<TResult>> selector) {
      return SelectMany(lazy, selector, (a, b) => b);
    }


  }

  #region RestMonad<T>
  public class RestMonad<T> : RestMonad {

    T[] _values = new T[0];
    public T Value {
      get {
        if (!_values.Any()) throw new NullReferenceException("Value property was never initialized with any value.");
        return _values[0];
      }
      set {
        if (_values.Any()) throw new InvalidOperationException("Value property has already been set.");
        _values = new T[] { value };
      }
    }
    public RestMonad() { }
    public RestMonad(T value) {
      this.Value = value;
    }
    public RestMonad(RestMonad<T> restMonad)
      : this(restMonad.Value) {
      restMonad.Copy(this);
    }
    public RestMonad(T value, RestMonad restMonad)
      : this(value) {
      restMonad.Copy(this);
    }
    public RestMonad(T value, string baseAddress, string apiAddress, string userName, string password)
      : this(value, new Uri(baseAddress), apiAddress, userName, password) {
    }
    public RestMonad(T value, Uri baseUri, string apiAddress, string userName, string password)
      : base(baseUri, apiAddress, userName, password) {
      this.Value = value;
    }

    #region Clone
    public virtual V Clone<V>() where V : RestMonad, new() {
      return this.Copy(new V());
    }
    public V Clone<V, TNew>(Func<T, TNew> valueFactory) where V : RestMonad<TNew>, new() {
      var clone = this.Clone<V>();
      if (valueFactory != null)
        clone.SetValue(valueFactory(this.Value));
      return clone;
    }
    public V Clone<V, TNew>(Func<TNew> valueFactory) where V : RestMonad<TNew>, new() {
      var clone = this.Clone<V>();
      if (valueFactory != null)
        clone.SetValue(valueFactory());
      return clone;
    }
    public U Clone<U, TNew>(TNew value) where U : RestMonad<TNew>, new() {
      return this.Clone<U, TNew>(() => value);
    }
    #endregion

    /// <summary>
    /// Clone context of RestMonad and add value
    /// </summary>
    /// <typeparam name="U">Type of the value for new RestMonad</typeparam>
    /// <param name="value">Value for new RestMonad&lt;T&gt;</param>
    /// <returns></returns>
    public RestMonad<U> Switch<U>(U value) {
      return Copy(new RestMonad<U>(value));
    }
    /// <summary>
    /// Change object of RestMonadSome type to object of RestMonadOther type
    /// </summary>
    /// <typeparam name="U">New RestMonad-derrived type</typeparam>
    /// <param name="value">New value for transformed RestMonad</param>
    /// <returns></returns>
    public U Transform<U>(T value) where U : RestMonad<T>, new() {
      return this.Clone<U>().SetValue(value);
    }
    /// <summary>
    /// Change object of RestMonadSome type to object of RestMonadOther type
    /// </summary>
    /// <typeparam name="U">New RestMonad-derrived type</typeparam>
    /// <returns></returns>
    public U Transform<U>() where U : RestMonad<T>, new() {
      return this.Transform<U>(this.Value);
    }
    public override string ToString() {
      return Value.Equals(default(T)) ? Value + "" : Value.ToString();
    }
  }
  #endregion

  #region Extensions
  public static class RestExtenssions {

    #region RestMonad<T>
    public static U SetValue<U, T>(this U restMonad, T value) where U : RestMonad<T> {
      restMonad.Value = value;
      return restMonad;
    }
    public static U Clone<U, T>(this U restMonad, T value) where U : RestMonad<T>, new() {
      var clone = new U().SetValue(value);
      return restMonad.Copy(clone);
    }
    public static RestMonad<T> ToRestMonad<T>(this T o) { return new RestMonad<T>(o); }
    public static RestMonad<T> ToRestMonad<T>(this T o, RestMonad source) { return source.Copy(new RestMonad<T>(o)); }
    async public static Task<RestMonad<HttpResponseMessage>> DeleteAsync(RestMonad<HttpClient> client, string path) {
      return await client.SendAsync(HttpMethod.Delete, path);
    }
    async public static Task<RestMonad<HttpResponseMessage>> GetAsync(this RestMonad<HttpClient> client, string path) {
      return await client.SendAsync(HttpMethod.Get, path);
    }
    async public static Task<RestMonad<HttpResponseMessage>> SendAsync(this RestMonad<HttpClient> client, HttpMethod method, string path, bool recurse = false) {
      var sw = Stopwatch.StartNew();
      try {
        if (client.BaseAddress == null) client.BaseAddress = client.Value.BaseAddress;
        CheckApiAddress(client, path);
        var message = new HttpRequestMessage(method, client.ApiAddress);
        try {
          var hrm = await SendAsync(client, message);
          if (!recurse && hrm.Value.StatusCode == HttpStatusCode.Unauthorized && RestMonad.UseSessionID) {
            await RestMonad.SetDefaultSessionID();
            return await SendAsync(client, method, "", true);
          }
          return hrm;
        } catch (HttpResponseMessageException exc) {
          if (exc.Response.StatusCode == HttpStatusCode.Unauthorized && RestMonad.UseSessionID) {
            await RestMonad.SetDefaultSessionID();
            return await SendAsync(client, message);
          }
          throw;
        }
      } catch (Exception exc) {
        throw new HttpRestException(client.Value.BaseAddress + client.ApiAddress, exc);
      } finally {
        if (JiraMonad.JiraConfig.TraceRequests)
          Debug.WriteLine(new { SendAsync = sw.ElapsedMilliseconds, path, method });
      }
    }

    private static async Task<RestMonad<HttpResponseMessage>> SendAsync(RestMonad<HttpClient> client, HttpRequestMessage message) {
      var sessionID = client.HasSessionID ? client.SessionID : await RestMonad.GetUserCookie(client.UserName);
      if (sessionID != null) {
        //Debug.WriteLine(sessionID.ToJson());
        client.Value.DefaultRequestHeaders.Authorization = null;
        message.Headers.Add("Cookie", sessionID.Name + "=" + sessionID.Value);
      }
      return client.Switch(await client.Value.SendAsync(message));
    }

    public static async Task<RestMonad<HttpResponseMessage>> PostAsync(this RestMonad<HttpClient> client, string path, StringContent content, bool doPut) {
      var sw = Stopwatch.StartNew();
      try {
        return await PostAsyncImpl(client, path, content, doPut);
      } catch (HttpResponseMessageException exc) {
        if (exc.Response.StatusCode == HttpStatusCode.Unauthorized && RestMonad.UseSessionID) {
          await RestMonad.SetDefaultSessionID();
          return await PostAsyncImpl(client, path, content, doPut);
        }
        throw;
      } catch (Exception exc) {
        throw new HttpRestException(client.Value.BaseAddress + path, exc);
      } finally {
        if (JiraMonad.JiraConfig.TraceRequests)
          Debug.WriteLine(new { PostAsync = sw.ElapsedMilliseconds, path, doPut });
      }
    }

    private static async Task<RestMonad<HttpResponseMessage>> PostAsyncImpl(RestMonad<HttpClient> client, string path, StringContent content, bool doPut) {
      var sessionID = client.HasSessionID ? client.SessionID : await RestMonad.GetUserCookie(client.UserName);
      if (sessionID != null) {
        //Debug.WriteLine(sessionID.ToJson());
        client.Value.DefaultRequestHeaders.Authorization = null;
        content.Headers.Add("Cookie", sessionID.Name + "=" + sessionID.Value);
      }
      if (client.BaseAddress == null) client.BaseAddress = client.Value.BaseAddress;
      CheckApiAddress(client, path);
      Func<Task<HttpResponseMessage>> go = async () => doPut
        ? await client.Value.PutAsync(client.ApiAddress, content)
        : await client.Value.PostAsync(client.ApiAddress, content);
      return client.Switch(await go());
    }

    private static void CheckApiAddress(RestMonad<HttpClient> client, string path) {
      if (string.IsNullOrWhiteSpace(client.ApiAddress))
        client.ApiAddress = path;
      else if (!string.IsNullOrWhiteSpace(path))
        throw new Exception(new { client = new { client.ApiAddress }, path, error = "ApiAddress or path must be empty" } + "");

      //if(Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var pathUri) && pathUri.IsAbsoluteUri) {
      //  client.BaseAddress = pathUri;
      //  client.ApiAddress = "";
      //}
    }

    //public static Func<string, RestMonad.PasswordClass, Uri, HttpClient> BuildCustomClientFactory(this RestMonad restMonad) {
    //  return restMonad == null ? (Func<HttpClient>)null : restMonad.BuildCustomClientFactory();
    //}
    #endregion

    public static HttpClient InitBasicAuthenticationHeader(this HttpClient client, string userName, string password) {
      client.DefaultRequestHeaders.Authorization = JiraMonad.MakeBasicAuthenticationHeader(userName, password);
      return client;
    }
    public static IEnumerable<Exception> All(this Exception ex, bool skipAggregate = false) {
      if (ex == null) throw new ArgumentNullException("ex");
      var innerException = ex;
      do {
        if (!(innerException is AggregateException))
          yield return innerException;
        innerException = innerException.InnerException;
      }
      while (innerException != null);
    }
    public static string FormatRest<E>(this E e) where E : Exception { return RestMonad.FormatException(e); }
    public static bool OAuthHeaderFactory(this HttpClient client) {
      var auth = client.DefaultRequestHeaders.Authorization;
      if (auth != null && auth.Scheme == "OAuth") {
        var oAuthParams = client.DefaultRequestHeaders.Authorization.Parameter;
        var oa = JsonConvert.DeserializeAnonymousType(oAuthParams, new { key = "", secret = "" });
        OAuthBase oAuth = new OAuthBase();
        string nonce = oAuth.GenerateNonce();
        string timestamp = oAuth.GenerateTimeStamp();
        string key = oa.key;
        string secret = oa.secret;
        Uri url = client.BaseAddress;
        string normedURL = "";
        string normedParams = "";

        string sig = oAuth.GenerateSignature(new Uri("http://metabank.com"), key, secret, "", "", "GET", timestamp, nonce, out normedURL, out normedParams);
        normedParams += "&oauth_signature=" + Uri.EscapeDataString(sig);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", normedParams);
        return true;
      }
      return false;
    }
    public static void OAuthPreHeaderFactory(this HttpClient httpClient, string key, string secret) {
      var header = JsonConvert.SerializeObject(new { key, secret });
      httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", header);
    }

    public static string ToJiraDate(this DateTime date) {
      return date.Date.ToString("yyyy-MM-dd");
    }
    public static string ToJiraDateTime(this DateTime date) {
      //2011-10-19T10:29:29.908+1100
      return date.ToString("yyyy-MM-ddTHH:mm:ss.fff") + date.ToString("zzzz").Replace(":", "");
      //return date.ToString("yyyy-MM-dd HH:mm");
    }
    public static string ToJiraTime(this TimeSpan span) {
      return span.ToString(@"d\d\ hh\h\ mm\m");
    }
    public static DateTime FromTimestamp(this double timestamp) =>
      new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Add(TimeSpan.FromMilliseconds(timestamp));
  }
  #endregion
}