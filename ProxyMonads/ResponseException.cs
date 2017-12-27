using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Wcf.ProxyMonads {
  public enum RestErrorType { Http, Json };
  public class HttpRestException : Exception {
    public string Address { get; protected set; }
    public HttpRestException(string address, Exception origonalException) : this(address, "", origonalException) { }
    public HttpRestException(string address, string message, Exception originalException)
      : base(message, originalException) {
      this.Address = address;
    }
    public HttpRestException(string address, string message)
      : base(message) {
      this.Address = address;
    }
    public HttpRestException(RestMonad<HttpResponseMessage> response, string message)
      : this(new[] { response.Value.RequestMessage.RequestUri + "", response.FullAddress() }.First(s => !string.IsNullOrWhiteSpace(s)), message) {
    }
    public override string ToString() {
      return new { Address, Exception = base.ToString() } + "";
    }
    public static HttpRestException<T> Create<T>(string address, T exception) where T : Exception { return new HttpRestException<T>(address, exception); }
    public static HttpRestException<T> Create<T>(string address, string message, T exception) where T : Exception { return new HttpRestException<T>(address, message, exception); }
  }
  public class HttpRestException<T> : HttpRestException where T : Exception {
    public HttpRestException(string address, T origonalException) : base(address, origonalException) { }
    public HttpRestException(string address, string message, T origonalException) : base(address, message, origonalException) { }
  }

  public enum ResponceErrorType { Unknown, AlreadyExists, NotFound };
  public class HttpResponseMessageException : HttpRestException {

    public HttpResponseMessage Response { get; protected set; }
    public string Json { get; set; }
    public ResponceErrorType ResponseErrorType { get; set; }

    public HttpResponseMessageException(HttpResponseMessage response, string json, string address, string message, Exception originalException)
      : base(address, message, originalException) {
      this.Response = response;
      this.Json = json;
      if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        this.ResponseErrorType = ResponceErrorType.NotFound;
    }

    //public HttpResponseMessageException(HttpResponseMessage response, string json, string address, Exception innerException) {
    //  Response = response;
    //  Json = json;
    //  Address = address;
    //  this.innerException = innerException;
    //}

    public override string ToString() {
      return new { Json, Response, Exception = base.ToString() } + "";
    }
  }
}
