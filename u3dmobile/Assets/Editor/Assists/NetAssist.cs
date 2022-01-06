using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace U3DMobile.Editor
{
    static class NetAssist
    {
        public static void HttpGet(string remoteUrl, string localPath, Action completion)
        {
            CoroutineAssist.StartCoroutine(HttpGetRoutine(remoteUrl, localPath, completion));
        }

        private static IEnumerator HttpGetRoutine(string remoteUrl, string localPath, Action completion)
        {
            FileAssist.DeletePath(localPath);

            UnityWebRequest session = UnityWebRequest.Get(remoteUrl);
            session.downloadHandler = new DownloadHandlerFile(localPath);

            yield return session.SendWebRequest();
            while (!session.isDone)
            {
                yield return null;
            }

            if (session.result != UnityWebRequest.Result.Success)
            {
                Debug.LogErrorFormat("get url error: {0}", session.result);
            }
            completion?.Invoke();
        }
    }
}
