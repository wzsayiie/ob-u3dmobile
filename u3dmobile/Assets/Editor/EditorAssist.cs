using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace U3DMobile.Editor
{
    static class CoroutineAssist
    {
        private static HashSet<IEnumerator> s_routines;

        public static void StartCoroutine(IEnumerator routine)
        {
            if (s_routines == null)
            {
                s_routines = new HashSet<IEnumerator>();

                long count = 0;
                EditorApplication.update += () =>
                {
                    //the frequency of EditorApplication.update is about 330 times per second.
                    //here to reduce the call frequency.
                    if (++count % 330 == 0)
                    {
                        Update();
                    }
                };
            }

            if (routine != null)
            {
                s_routines.Add(routine);
            }
        }

        private static void Update()
        {
            if (s_routines.Count == 0)
            {
                return;
            }

            //heart beat.
            Debug.Log(".");

            var routines = new HashSet<IEnumerator>(s_routines);
            foreach (IEnumerator item in routines)
            {
                bool going = item.MoveNext();
                if (!going)
                {
                    s_routines.Remove(item);
                }
            }
        }
    }

    static class FileAssist
    {
        public static void CreateDirectoryIfNeed(string path)
        {
            if (Directory.Exists(path))
            {
                return;
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
                Directory.CreateDirectory(path);
            }
            else
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void ResetDirectory(string path)
        {
            DeletePath(path);
            Directory.CreateDirectory(path);
        }

        public static void DeletePath(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static void MovePath(string src, string dst)
        {
            DeletePath(dst);

            string dstParent = Path.GetDirectoryName(dst);
            CreateDirectoryIfNeed(dstParent);

            if (Directory.Exists(src))
            {
                Directory.Move(src, dst);
            }
            else if (File.Exists(src))
            {
                File.Move(src, dst);
            }
        }

        public static void ExtractFile(string archivePath, string targetDirectory)
        {
            ResetDirectory(targetDirectory);

            //use "tar" here. the tool from System.IO.Compression doesn't support formats such as "tgz".
            //NOTE: for window, carries "tar" from windows 10.
            var info = new System.Diagnostics.ProcessStartInfo();
            {
                info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                info.FileName    = "tar";
                info.Arguments   = $"-xf {archivePath} -C {targetDirectory}";
            }

            var process = new System.Diagnostics.Process
            {
                StartInfo = info,
            };
            process.Start();
            process.WaitForExit();
        }
    }

    static class DownloadAssist
    {
        public static void Get(string remoteUrl, string localPath, Action completion)
        {
            CoroutineAssist.StartCoroutine(GetRoutine(remoteUrl, localPath, completion));
        }

        private static IEnumerator GetRoutine(string remoteUrl, string localPath, Action completion)
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
