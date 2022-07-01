using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using NUnit.Framework;
using OpenRasta.Codecs.Newtonsoft.Json;
using OpenRasta.Configuration;
using OpenRasta.Hosting.AspNet.AspNetHttpListener;
using OpenRasta.Web;

namespace OpenRasta.Hosting.AspNet.Tests.Integration
{
  public abstract class aspnet_server_context : context
  {
    HttpListenerController _http;
    public HttpWebResponse TheResponse;
    public string TheResponseAsString;
    public int _port;

    public aspnet_server_context()
    {
      TheResponseAsString = null;
      TheResponse = null;
      SelectPort();
    }

    void SelectPort()
    {
      _port = new Random().Next(49152,65535);
    }

    [OneTimeTearDown]
    public void tear()
    {
      _http?.Stop();
    }

    public void GivenARequest(string verb, string uri)
    {
      GivenARequest(verb, uri, null, null);
    }

    public void GivenATextRequest(string verb, string uri, string content, string textEncoding)
    {
      GivenATextRequest(verb, uri, content, textEncoding, "text/plain");
    }

    public void GivenATextRequest(string verb, string uri, string content, string textEncoding, string contentType)
    {
      GivenARequest(verb, uri, Encoding.GetEncoding(textEncoding).GetBytes(content),
        new MediaType(contentType) {CharSet = textEncoding});
    }

    public void GivenAUrlFormEncodedRequest(string verb, string uri, string content, string textEncoding)
    {
      GivenARequest(verb, uri, Encoding.GetEncoding(textEncoding).GetBytes(content),
        new MediaType("application/x-www-form-urlencoded") {CharSet = textEncoding});
    }

    public void GivenARequest(string verb, string uri, byte[] content, MediaType contentType)
    {
      var destinationUri = new Uri("http://127.0.0.1:" + _port + uri);

      WebRequest request = WebRequest.Create(destinationUri);
      request.Timeout = Debugger.IsAttached ? int.MaxValue :  (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
      request.Method = verb;
      
      if (content?.Length > 0)
      {
        request.ContentLength = content.Length;
        request.ContentType = contentType.ToString();
        using (var requestStream = request.GetRequestStream())
          requestStream.Write(content, 0, content.Length);
      }
      try
      {
        TheResponse = request.GetResponse() as HttpWebResponse;
      }
      catch (WebException exception) when (exception.Response is HttpWebResponse r)
      {
         TheResponse = r;
      }
    }

    public void GivenTheResponseIsInEncoding(Encoding encoding)
    {
      if (TheResponse == null)
      {
        Assert.Fail($"{nameof(TheResponse)} is null");
      }
      var data = new byte[TheResponse.ContentLength];

      var payload = TheResponse.GetResponseStream()?.Read(data, 0, data.Length);

      TheResponseAsString = payload != null ? encoding.GetString(data, 0, payload.Value) : null;
    }

    public void ConfigureServer(Action configuration)
    {
      _http = new HttpListenerController
        (new[] {"http://127.0.0.1:" + _port + "/"}, "/", TempFolder.FullName);
      _http.Start(configuration);
    }
  }
}