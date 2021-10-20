using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace U3DMobile.Editor
{
    public class EditorAssist
    {
        #region singleton instance

        private static EditorAssist s_instance;

        public static EditorAssist GetInstance()
        {
            if (s_instance == null)
            {
                s_instance = new EditorAssist();
            }
            return s_instance;
        }

        public static EditorAssist instance
        {
            get
            {
                return GetInstance();
            }
        }

        #endregion

        #region initialization

        private HashSet<IEnumerator> _routines = new HashSet<IEnumerator>();

        private EditorAssist()
        {
            long count = 0;
            EditorApplication.update += () =>
            {
                //the frequency of EditorApplication.update is about 300 times per second.
                //here to reduce the call frequency.
                if (++count % 10 == 0)
                {
                    Update();
                }
            };
        }

        #endregion

        #region coroutine

        public void StartCoroutine(IEnumerator routine)
        {
            if (routine != null)
            {
                _routines.Add(routine);
            }
        }

        private void Update()
        {
            if (_routines.Count == 0)
            {
                return;
            }

            var routineItems = new HashSet<IEnumerator>(_routines);
            foreach (IEnumerator item in routineItems)
            {
                bool going = item.MoveNext();
                if (!going)
                {
                    _routines.Remove(item);
                }
            }
        }

        #endregion

        #region file operation

        public void DeletePath(string path)
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

        public void CreateDirectoryIfNeed(string path)
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

        public void ResetDirectory(string path)
        {
            DeletePath(path);
            Directory.CreateDirectory(path);
        }

        public void GetRemote(string remoteUrl, string localPath, Action complete)
        {
            instance.StartCoroutine(GetRemoteRoutine(remoteUrl, localPath, complete));
        }

        private IEnumerator GetRemoteRoutine(string remoteUrl, string localPath, Action complete)
        {
            DeletePath(localPath);

            UnityWebRequest session = UnityWebRequest.Get(remoteUrl);
            session.downloadHandler = new DownloadHandlerFile(localPath);

            yield return session.SendWebRequest();
            while (!session.isDone)
            {
                yield return null;
            }

            if (session.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"get url error: {session.result}");
            }
            complete?.Invoke();
        }

        public void ExtractFile(string archivePath, string targetDirectory)
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

            var process = new System.Diagnostics.Process();
            {
                process.StartInfo = info;
            }
            process.Start();
            process.WaitForExit();
        }

        public void MovePath(string src, string dst)
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

        #endregion
    }
}
