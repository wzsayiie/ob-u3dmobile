using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace U3DMobile.Editor
{
    static class CoroutineAssist
    {
        private static HashSet<IEnumerator> s_routines;

        public static void StartCoroutine(IEnumerator routine)
        {
            if (s_routines == null)
            {
                Initialize();
            }

            if (routine != null)
            {
                s_routines.Add(routine);
            }
        }

        private static void Initialize()
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
}
