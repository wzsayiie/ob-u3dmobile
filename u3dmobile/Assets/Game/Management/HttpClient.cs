using System.Collections;
using UnityEngine.Networking;

namespace U3DMobile.Management
{
    public class HttpGetResponse
    {
        public bool success;

        public UnityWebRequest.Result resultCode;

        public int    responseCode;
        public byte[] responseBody;
    }

    public delegate void HttpGetResponder(HttpGetResponse response);

    public class HttpClient : SingletonBehaviour<HttpClient>
    {
        public static HttpClient instance { get { return GetInstance(); } }

        public void Get(string url, HttpGetResponder responder)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Log.Error("empty url for http request");
                return;
            }
            if (responder == null)
            {
                Log.Error("null responder for http request");
            }

            StartCoroutine(GetRoutine(url, responder));
        }

        private IEnumerator GetRoutine(string url, HttpGetResponder responder)
        {
            UnityWebRequest session = UnityWebRequest.Get(url);
            yield return session.SendWebRequest();

            var response = new HttpGetResponse
            {
                success      = session.result == UnityWebRequest.Result.Success,
                resultCode   = session.result,
                responseCode = (int)session.responseCode,
                responseBody = session.downloadHandler.data,
            };
            responder(response);
        }
    }
}
