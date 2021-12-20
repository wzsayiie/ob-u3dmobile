using System.Collections.Generic;
using UnityEngine.Networking;

namespace U3DMobile
{
    public class HttpRequest
    {
        private Dictionary<string, string> _requestHeaders = new Dictionary<string, string>();

        private string _requestMethod;
        private string _requestUrl;
        private byte[] _requestBody;

        public void SetRequestHeader(string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
            {
                _requestHeaders.Add(name, value);
            }
        }

        public string requestMethod
        {
            set { _requestMethod = value; }
            get { return _requestMethod ; }
        }

        public string requestUrl
        {
            set { _requestUrl = value; }
            get { return _requestUrl ; }
        }

        public byte[] requestBody
        {
            set { _requestBody = value; }
            get { return _requestBody ; }
        }

        public void Request(HttpResponder responder)
        {
            if (string.IsNullOrWhiteSpace(_requestUrl))
            {
                Log.Error("http request url is empty");
                return;
            }
            if (responder == null)
            {
                Log.Error("the responder is null for '{0}'", _requestUrl);
                return;
            }

            //set the method, url and body.
            string method;
            if (!string.IsNullOrWhiteSpace(_requestMethod))
            {
                method = _requestMethod.ToUpper();
            }
            else
            {
                method = "GET";
            }

            UnityWebRequest session;
            if (method == "POST")
            {
                session = UnityWebRequest.Put(_requestUrl, _requestBody);
                session.method = method;
            }
            else if (method == "PUT")
            {
                session = UnityWebRequest.Put(_requestUrl, _requestBody);
            }
            else if (method == "GET")
            {
                session = UnityWebRequest.Get(_requestUrl);
            }
            else
            {
                Log.Error("unsupported method '{0}' for url '{1}'", method, _requestUrl);
                return;
            }

            //set the headers.
            foreach (KeyValuePair<string, string> pair in _requestHeaders)
            {
                session.SetRequestHeader(pair.Key, pair.Value);
            }

            //start the session.
            HttpClient.instance.Request(session, responder);
        }
    }

    public delegate void HttpResponder(HttpResponse response);

    public class HttpResponse
    {
        public bool success;
        public UnityWebRequest.Result resultCode;

        public Dictionary<string, string> responseHeaders;

        public int    responseCode;
        public byte[] responseBody;
    }

    class HttpClient : SingletonBehaviour<HttpClient>
    {
        public static HttpClient instance { get { return GetInstance(); } }

        public void Request(UnityWebRequest session, HttpResponder responder)
        {
            StartCoroutine(GetRoutine(session, responder));
        }

        private System.Collections.IEnumerator GetRoutine(UnityWebRequest session, HttpResponder responder)
        {
            yield return session.SendWebRequest();

            var response = new HttpResponse
            {
                success         = session.result == UnityWebRequest.Result.Success,
                resultCode      = session.result,
                responseHeaders = session.GetResponseHeaders(),
                responseCode    = (int)session.responseCode,
                responseBody    = session.downloadHandler.data,
            };
            responder(response);
        }
    }
}
