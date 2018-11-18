//------------------------------------------------------------------------------
// <copyright file="CSSqlClassFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Jira.Json;
using System.Configuration;
using System.Net.Http.Headers;
using Wcf.ProxyMonads;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using CommonExtensions;
using Newtonsoft.Json.Linq;

namespace Jira {
  public static class Core {
    public static async Task IsJiraDev() {
      const string jiraServer = "usmpokwjird01";
      var jiraHost = (await RestMonad.Empty().GetAuthSession()).BaseAddress.Host.ToLower();
      Passager.ThrowIf(() => !jiraHost.ToLower().Contains(jiraServer), new { jiraHost } + "");
    }
    public static async Task IsJiraQA() {
      var jiraHost = (await RestMonad.Empty().GetAuthSession()).BaseAddress.Host.ToLower();
      Passager.ThrowIf(() => !jiraHost.ToLower().Contains("usmrtpwjirq01"), new { jiraHost } + "");
    }
    #region Factories
    static HttpClient HttpClientFactory(Func<string, RestMonad.PasswordClass, string, HttpClient> customFactory) {
      var client = customFactory(JiraMonad.JiraPowerUser(), JiraMonad.JiraPowerPassword(), JiraMonad.JiraServiceBaseAddress());
      //if (client.DefaultRequestHeaders.Authorization == null && !string.IsNullOrWhiteSpace(JiraMonad.JiraPowerUser()))
      //  client.InitBasicAuthenticationHeader(JiraMonad.JiraPowerUser(), JiraMonad.JiraPowerPassword());
      //if (client.BaseAddress == null)
      //  client.BaseAddress = new Uri(JiraMonad.JiraServiceBaseAddress()); //jira/rest/api/2/issue/test-1/comment?_=1396294689706
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      return client;
    }

    public static StringContent ContentFactory(this string text) {
      var content = new StringContent(text);
      content.Headers.Remove("Content-Type");
      content.Headers.Add("Content-Type", "application/json; charset=utf-8");
      return content;
    }
    #endregion

    #region Very Low Order Methods
    #region Filters
    public static Func<string, string, bool> FilterCompare = (string1, string2) => string1.Equals(string2, StringComparison.OrdinalIgnoreCase);
    public static Func<IEnumerable<string>, string, bool> FilterCompareAny = (texts, search) => texts.Any(text => FilterCompare(text, search));
    public static Func<IEnumerable<string>, object, bool> FilterCompareAll = (texts, search) => {
      return search == null
        ? false
        : search.GetType().IsArray
        ? ((object[])search).Any(s => FilterCompareAny(texts, s + ""))
        : FilterCompareAny(texts, search + "");
    };
    public static Func<string, string, bool> FilterContains = (text, search) => text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    public static Func<IEnumerable<string>, string, bool> FilterContainsAny = (texts, search) => texts.Any(text => FilterContains(text, search));

    public static T[] FilterJson<T>(string[] filter, string json, bool isEqual, Func<T, IEnumerable<string>> valueToFilter) {
      return FilterJson<T>(filter, json, isEqual ? FilterCompareAny : FilterContainsAny, valueToFilter);
    }
    private static T[] FilterJson<T>(string[] filter, string json, Func<IEnumerable<string>, string, bool> comparer, Func<T, IEnumerable<string>> valueToFilter) {
      var projectsFiltered = JsonConvert.DeserializeObject<T[]>(json)
        .Where(p => !filter.Any() || filter.Any(key => comparer(valueToFilter(p), key))).ToArray();
      return projectsFiltered;
    }
    #endregion

    #region Get Array
    public static async Task<RestMonad<T[]>> GetArrayAsync<T>(this RestMonad jiraPost, Func<string, string> pathFactory, bool isEquelFilter, Func<T, string> valuetoFilter, params string[] filterByKey) {
      return await jiraPost.GetArrayAsync(pathFactory, isEquelFilter, new Func<T, IEnumerable<string>>(context => new[] { valuetoFilter(context) }), filterByKey);
    }
    public static async Task<RestMonad<T[]>> GetArrayAsync<T>(this RestMonad jiraPost, Func<string, string> pathFactory, bool isEquelFilter, Func<T, IEnumerable<string>> valuetoFilter, params string[] filterByKey) {
      return await jiraPost.GetArrayAsync(pathFactory, isEquelFilter, valuetoFilter, false, filterByKey);
    }
    public static async Task<RestMonad<T[]>> GetArrayAsync<T>(this RestMonad jiraPost, Func<string, string> pathFactory, bool isEquelFilter, Func<T, IEnumerable<string>> valuetoFilter, bool throwIfMissing, params string[] filterByKey) {
      return await jiraPost.GetAsync<T[]>(pathFactory, (rm, json) => {
        var projectsFiltered = Core.FilterJson<T>(filterByKey, json, isEquelFilter, valuetoFilter);
        //if(throwIfMissing && projectsFiltered.Length == 0)
        var ret = rm.Switch(projectsFiltered);
        return ret;
      }, null, null);
    }
    #endregion

    #region Delete
    async public static Task<RestMonad<HttpResponseMessage>> DeleteAsync(this RestMonad restMonad, Func<string> pathFactory) {
      Guard.NotNull(() => restMonad, restMonad);
      using (var client = HttpClientFactory(restMonad.BuildCustomClientFactory())) {
        return (await RestExtenssions.DeleteAsync(client.ToRestMonad(restMonad),pathFactory()));
      }
    }
    async public static Task<RestMonad<HttpResponseMessage>> DeleteAsync(this RestMonad restMonad, string path) {
      Passager.ThrowIf(() => restMonad == null);
      return await (
        from rm in restMonad.DeleteAsync(() => path)
        from rm2 in rm.HandleExecutedAsync((response, json) => response, null, null)
        select rm2);
    }
    async public static Task<RestMonad<T>> DeleteAsync<T>(this RestMonad<T> restMonad, string path) {
      Passager.ThrowIf(() => restMonad == null);
      return await (
        from rm in restMonad.DeleteAsync(() => path)
        from rm2 in rm.HandleExecutedAsync((response, json) => response, null, null)
        select rm2.Switch(restMonad.Value));
    }
    #endregion

    #region Get
    public static async Task<RestMonad<T>> GetAsync<T>(this RestMonad restMonad, Func<string, string> pathFactory, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<T>> onError) {
      return await restMonad.GetAsync(pathFactory, Return<T>, onError, null);
    }
    //public static async Task<T> GetUnrelatedAsync<T>(this RestMonad rest, Func<string, string> pathFactory, Func<RestMonad<HttpResponseMessage>, string, T> notFound = null) {
    //  Guard.NotNull(() => rest, rest);
    //  return await
    //    (await rest.GetAsync(() => pathFactory(rest.ApiAddress)))
    //    .HandleExecutedAsync((response, json) => JsonConvert.DeserializeObject<T>(json), notFound);
    //}
    public static async Task<RestMonad<T>> GetAsync<T>(this RestMonad restMonad, Func<string> pathFactory, Func<RestMonad<HttpResponseMessage>, string, RestMonad<T>> responseHandler, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<T>> onError, Func<RestMonad<HttpResponseMessage>, string, RestMonad<T>> notFoundHandler) {
      return await (await restMonad.GetAsync(pathFactory)).HandleExecutedAsync(responseHandler ?? Jira.Core.Return<T>, onError, notFoundHandler);
    }
    public static async Task<RestMonad<T>> GetAsync<T>(this RestMonad restMonad, Func<string, string> pathFactory, Func<RestMonad<HttpResponseMessage>, string, RestMonad<T>> responseHandler, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<T>> onError, Func<RestMonad<HttpResponseMessage>, string, RestMonad<T>> notFoundHandler) {
      return await (await restMonad.GetAsync(pathFactory)).HandleExecutedAsync(responseHandler ?? Jira.Core.Return<T>, onError, notFoundHandler);
    }
    async public static Task<RestMonad<HttpResponseMessage>> GetAsync(this RestMonad restMonad, Func<string, string> pathFactory) {
      return await restMonad.GetAsync(() => pathFactory(restMonad.ApiAddress));
    }
    async public static Task<RestMonad<HttpResponseMessage>> GetAsync(this RestMonad restMonad, Func<string> pathFactory) {
      Guard.NotNull(() => restMonad, restMonad);
      using (var client = HttpClientFactory(restMonad.BuildCustomClientFactory())) {
        return (await client.ToRestMonad(restMonad).GetAsync(pathFactory()));
      }
    }
    async public static Task<RestMonad<byte[]>> GetBytesAsync(this RestMonad restMonad, string path) {
      try {
        var hrm = await (from rm in restMonad.GetAsync(() => path)
                         from bytes in rm.Value.Content.ReadAsByteArrayAsync()
                         select bytes);
        return hrm.ToRestMonad(restMonad);
      } catch (Exception exc) {
        throw new Exception(new { path } + "", exc);
      }
    }
    async public static Task<RestMonad<string>> GetStringAsync(this RestMonad restMonad, string path) {
      try {
        var hrm = await (from rm in restMonad.GetAsync(() => path)
                         from bytes in rm.Value.Content.ReadAsStringAsync()
                         select bytes);
        return hrm.ToRestMonad(restMonad);
      } catch (Exception exc) {
        throw new Exception(new { path } + "", exc);
      }
    }
    #endregion

    #region Post
    public static async Task<string> PostFormAsync(string requestUri) => await PostFormAsync(requestUri, new Dictionary<string, string>());
    public static async Task<string> PostFormAsync(string requestUri, Dictionary<string, string> values) {
      using (var client = new HttpClient()) {
        client.BaseAddress = new Uri(JiraMonad.JiraServiceBaseAddress());
        client.InitBasicAuthenticationHeader(JiraMonad.JiraPowerUser(), JiraMonad.JiraPowerPassword());
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var requestContent = new FormUrlEncodedContent(values);
        requestContent.Headers.ContentType.MediaType = "application/x-www-form-urlencoded";
        requestContent.Headers.ContentType.CharSet = "UTF-8";
        requestContent.Headers.Add("X-Atlassian-Token", "no-check");
        return await (await from h in client.PostAsync(requestUri, requestContent)
            select h.HandleExecutedAsync((r, json) => {
              var errors = RestMonad.ParseJspError(json);
              errors.Where(error => error.Contains("name already exists"))
              .ForEach(error =>
                throw new HttpResponseMessageException(r, json, r.RequestMessage.RequestUri + "", error, null) { ResponseErrorType = ResponceErrorType.AlreadyExists });
              if (errors.IsEmpty())
                return json;
              throw new Exception(errors.Flatten("\n"));
                            }, null, null));
      }
    }
    public static async Task<string> PostFormAsync2(string requestUri, Dictionary<string, string> values) {
      using(var client = new HttpClient()) {
        client.BaseAddress = new Uri(JiraMonad.JiraServiceBaseAddress());
        client.InitBasicAuthenticationHeader(JiraMonad.JiraPowerUser(), JiraMonad.JiraPowerPassword());
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var requestContent = new MyFormUrlEncodedContent(values);
        requestContent.Headers.ContentType.MediaType = "application/x-www-form-urlencoded";
        requestContent.Headers.ContentType.CharSet = "UTF-8";
        requestContent.Headers.Add("X-Atlassian-Token", "no-check");
        return await (await from h in client.PostAsync(requestUri, requestContent)
                            select h.HandleExecutedAsync((r, json) => {
                              var errors = RestMonad.ParseJspError(json);
                              errors.Where(error => error.Contains("name already exists"))
                              .ForEach(error =>
                                throw new HttpResponseMessageException(r, json, r.RequestMessage.RequestUri + "", error, null) { ResponseErrorType = ResponceErrorType.AlreadyExists });
                              if(errors.IsEmpty())
                                return json;
                              throw new Exception(errors.Flatten("\n"));
                            }, null, null));
      }
    }
    public class MyFormUrlEncodedContent :ByteArrayContent {
      public MyFormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
          : base(MyFormUrlEncodedContent.GetContentByteArray(nameValueCollection)) {
        base.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
      }
      private static byte[] GetContentByteArray(IEnumerable<KeyValuePair<string, string>> nameValueCollection) {
        if(nameValueCollection == null) {
          throw new ArgumentNullException("nameValueCollection");
        }
        StringBuilder stringBuilder = new StringBuilder();
        foreach(KeyValuePair<string, string> current in nameValueCollection) {
          if(stringBuilder.Length > 0) {
            stringBuilder.Append('&');
          }

          stringBuilder.Append(MyFormUrlEncodedContent.Encode(current.Key));
          stringBuilder.Append('=');
          stringBuilder.Append(MyFormUrlEncodedContent.Encode(current.Value));
        }
        return Encoding.Default.GetBytes(stringBuilder.ToString());
      }
      private static string Encode(string data) {
        if(string.IsNullOrEmpty(data)) {
          return string.Empty;
        }
        return System.Net.WebUtility.UrlEncode(data).Replace("%20", "+");
      }
    }
    public static async Task<RestMonad<HttpResponseMessage>> PostAsync(this RestMonad post, Func<string> pathFactory, object jPost, JsonSerializerSettings settings, bool doPut) {
      string json = JsonConvert.SerializeObject(jPost, settings);
      var content = json.ContentFactory();
      using (var client = HttpClientFactory(post.BuildCustomClientFactory()))
        return await client.ToRestMonad(post).
          PostAsync(pathFactory(), content, doPut);
    }
    public static async Task<RestMonad<T>> PostAsync<T>(this RestMonad post, Func<string> pathFactory, object jPost, Func<RestMonad<HttpResponseMessage>, string, RestMonad<T>> responseHandler = null)
      => await post.PostAsync(pathFactory, jPost, false, responseHandler);
    public static async Task<RestMonad<T>> PostAsync<T>(this RestMonad post, Func<string> pathFactory, object jPost, bool doPut, Func<RestMonad<HttpResponseMessage>, string, RestMonad<T>> responseHandler = null) {
      var r = await post.PostAsync(pathFactory, jPost, (JsonSerializerSettings)null, doPut);
      return await r.HandleExecutedAsync(responseHandler ?? Jira.Core.Return<T>);
    }
    public static async Task<RestMonad<T>> PostAsync<T>(this RestMonad post, Func<string> pathFactory, object jPost, Func<RestMonad<HttpResponseMessage>, string, RestMonad<T>> responseHandler, Func<RestMonad<HttpResponseMessageException>, RestErrorType, RestMonad<T>> onError, bool doPut) {
      var r = await post.PostAsync(pathFactory, jPost, (JsonSerializerSettings)null, doPut);
      return await r.HandleExecutedAsync(responseHandler, onError, null);
    }

    #endregion
    #endregion

    async public static Task<RestMonad<HttpResponseMessage>> GetIssueAsync(this JiraTicket<string> ticket, Func<string, string> ticketPathFactory) {
      using (var client = HttpClientFactory(ticket.BuildCustomClientFactory()))
        return (await client.ToRestMonad(ticket).GetAsync(ticketPathFactory(ticket.Value)));
    }
    /// <summary>
    /// Post content into existing JIRA Ticket
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ticket"></param>
    /// <param name="ticketPathFactory"></param>
    /// <param name="postObject"></param>
    /// <param name="jsonSettings"></param>
    /// <returns></returns>
    async public static Task<JiraTicket<HttpResponseMessage>> PostIssueAsync<T>(this JiraTicket<string> ticket, Func<string, string> ticketPathFactory, T postObject, JsonSerializerSettings jsonSettings = null) {
      var settings = jsonSettings ?? RestMonad.JsonSerializerSettingsFactory();
      var content = JsonConvert.SerializeObject(postObject, settings).ContentFactory();
      using (var client = HttpClientFactory(ticket.BuildCustomClientFactory()))
        return (await client.ToRestMonad(ticket).PostAsync(ticketPathFactory(ticket.Value), content, false))
          .Transform<JiraTicket<HttpResponseMessage>>();
    }

    public static RestMonad<TOut> ReturnDebug<TOut>(RestMonad<HttpResponseMessage> responseMonad, string json) 
      => Return<TOut>(responseMonad, json);
    public static RestMonad<TOut> Return<TOut>(RestMonad<HttpResponseMessage> responseMonad, string json) { return responseMonad.CastJson<TOut>(json); }
    static RestMonad<TOut> CastJson<TOut>(this RestMonad<HttpResponseMessage> responseMonad, string json) { return responseMonad.Switch(JsonConvert.DeserializeObject<TOut>(json)); }

    async public static Task<T> HandleExecutedAsync<T>(this RestMonad<HttpResponseMessage> response, Func<RestMonad<HttpResponseMessage>, string, T> handleResponse) {
      return await (response.HandleExecutedAsync(handleResponse, null, null));
    }
    async public static Task<T> HandleExecutedAsync<T>(this RestMonad<HttpResponseMessage> response, Func<RestMonad<HttpResponseMessage>, string, T> handleResponse, Func<RestMonad<HttpResponseMessageException>, RestErrorType, T> onError, Func<RestMonad<HttpResponseMessage>, string, T> handleNotFound) {
      var json = await response.Value.Content.ReadAsStringAsync();
      var address = new { response.Value.RequestMessage.RequestUri } + "\n";
      try {
        response.Value.EnsureSuccessStatusCode();
      } catch (Exception exc) {
        if (string.IsNullOrWhiteSpace(json)) {
          var url = response.Value.RequestMessage != null && response.Value.RequestMessage.RequestUri != null
            ? response.Value.RequestMessage.RequestUri.OriginalString
            : response.FullAddress();
          if (!string.IsNullOrWhiteSpace(url)) {
            var rex = new HttpResponseMessageException(response.Value, "", url + "", exc.Message, exc);
            if (onError == null) throw rex;
            return onError(response.Switch(rex), RestErrorType.Http);
          }
          throw;
        }
        if (handleNotFound != null && response.Value.StatusCode == System.Net.HttpStatusCode.NotFound) {
          try {
            var jObject = Newtonsoft.Json.JsonConvert.DeserializeObject(json) as Newtonsoft.Json.Linq.JObject;
            var sc = jObject["status-code"];
            if (sc == null) return handleNotFound(response, json);
          } catch(Exception hxc)when(! (hxc is HttpResponseMessageException)){
            throw;
          }
        }
        string errorMessage = "";
        try {
          errorMessage = JiraMonad.ParseJiraError(json);
        } catch { }
        if (string.IsNullOrWhiteSpace(errorMessage))
          try {
            errorMessage = RestMonad.ParseError(json);
          } catch { }
        if (string.IsNullOrWhiteSpace(errorMessage))
          errorMessage = json;
        {
          var rex = new HttpResponseMessageException(response.Value, json, address, errorMessage, exc);
          if (onError == null) {
            throw rex;
          }
          return onError(response.Switch(rex), RestErrorType.Json);
        }
      }
      var ret = handleResponse(response, json);
      return ret;
    }
    async public static Task<T> HandleExecutedAsync<T>(this HttpResponseMessage response, Func<HttpResponseMessage, string, T> handleResponse, Func<HttpResponseMessageException, RestErrorType, T> onError, Func<HttpResponseMessage, string, T> handleNotFound) {
      var json = await response.Content.ReadAsStringAsync();
      var address = new { response.RequestMessage.RequestUri } + "\n";
      try {
        response.EnsureSuccessStatusCode();
      } catch (Exception exc) {
        if (string.IsNullOrWhiteSpace(json)) {
          var url = response.RequestMessage != null && response.RequestMessage.RequestUri != null
            ? response.RequestMessage.RequestUri.OriginalString
            : response + "";
          if (!string.IsNullOrWhiteSpace(url)) {
            var rex = new HttpResponseMessageException(response, "", url + "", exc.Message, exc);
            if (onError == null) throw rex;
            return onError(rex, RestErrorType.Http);
          }
          throw;
        }
        if (handleNotFound != null && response.StatusCode == System.Net.HttpStatusCode.NotFound) {
          try {
            var jObject = Newtonsoft.Json.JsonConvert.DeserializeObject(json) as Newtonsoft.Json.Linq.JObject;
            var sc = jObject["status-code"];
            if (sc == null) return handleNotFound(response, json);
          } catch { }
        }
        string errorMessage = "";
        try {
          errorMessage = JiraMonad.ParseJiraError(json);
        } catch { }
        if (string.IsNullOrWhiteSpace(errorMessage))
          try {
            errorMessage = RestMonad.ParseError(json);
          } catch { }
        if (string.IsNullOrWhiteSpace(errorMessage))
          errorMessage = json;
        {
          var rex = new HttpResponseMessageException(response, json, address, errorMessage, exc);
          if (onError == null) {
            throw rex;
          }
          return onError(rex, RestErrorType.Json);
        }
      }
      var ret = handleResponse(response, json);
      return ret;
    }

  }
  static class Mixins {
    static T Default<T>(this T o, T defaultValue) { return o.Equals(default(T)) ? defaultValue : o; }
    static T Default<T>(this T o, Func<T> defaultFactory) { return o.Equals(default(T)) ? defaultFactory() : o; }
  }
}
