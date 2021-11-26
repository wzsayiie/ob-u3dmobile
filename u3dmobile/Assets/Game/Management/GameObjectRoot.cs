using UnityEngine;

namespace U3DMobile
{
    public static class GameObjectRoot
    {
        private static GameObject s_root;

        public static GameObject GetRoot()
        {
            if (s_root == null)
            {
                s_root = new GameObject("GameObjectRoot");
                Object.DontDestroyOnLoad(s_root);
            }
            return s_root;
        }
    }

    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T s_instance;

        public static T GetInstance()
        {
            if (s_instance == null)
            {
                var gameObject = new GameObject(typeof(T).Name);
                gameObject.transform.parent = GameObjectRoot.GetRoot().transform;

                s_instance = gameObject.AddComponent<T>();
            }
            return s_instance;
        }
    }

    public abstract class Singleton<T> where T : class, new()
    {
        private static T s_instance;

        public static T GetInstance()
        {
            if (s_instance == null)
            {
                s_instance = new T();
            }
            return s_instance;
        }
    }
}
