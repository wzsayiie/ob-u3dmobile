using UnityEditor;
using UnityEngine;

namespace U3DMobile
{
    public class AssetManager : Singleton<AssetManager>
    {
        public static AssetManager instance { get { return GetInstance(); } }

        public byte[]     LoadBytes (string path) { return LoadAsset(path, LoadStaticBytes , LoadBundleBytes ); }
        public string     LoadString(string path) { return LoadAsset(path, LoadStaticString, LoadBundleString); }
        public GameObject LoadPrefab(string path) { return LoadAsset(path, LoadStaticPrefab, LoadBundlePrefab); }

        private byte[] LoadStaticBytes(string path)
        {
            TextAsset asset = Resources.Load<TextAsset>(path);
            return asset?.bytes;
        }

        private string LoadStaticString(string path)
        {
            TextAsset asset = Resources.Load<TextAsset>(path);
            return asset?.ToString();
        }

        private GameObject LoadStaticPrefab(string path)
        {
            return Resources.Load<GameObject>(path);
        }

        private byte[] LoadBundleBytes(string path)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            return asset?.bytes;
        }

        private string LoadBundleString(string path)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            return asset?.ToString();
        }

        private GameObject LoadBundlePrefab(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private string ConvertPath(string path, out bool isStatic)
        {
            const string keyword = "Resources/";
            int index = path.LastIndexOf(keyword);

            //not in a "resources/" directory.
            if (index == -1)
            {
                isStatic = false;
                return path;
            }

            //the asset name is missing.
            if (index + keyword.Length == path.Length)
            {
                isStatic = false;
                return null;
            }

            //get the relative path to "resources/" directory.
            isStatic = true;
            return path.Substring(index);
        }

        private delegate T Loader<T>(string path);

        private T LoadAsset<T>(string path, Loader<T> staticLoader, Loader<T> bundleLoader) where T: class
        {
            if (string.IsNullOrEmpty(path))
            {
                Log.Error("try load asset by empty path");
                return null;
            }

            string suitablePath = ConvertPath(path, out bool isStatic);
            if (suitablePath == null)
            {
                Log.Error("fatal asset path: {0}", path);
                return null;
            }

            T asset;
            if (isStatic)
            {
                asset = staticLoader(suitablePath);
            }
            else
            {
                asset = bundleLoader(suitablePath);
            }

            if (asset == null)
            {
                Log.Error("there is no asset on: {0}", path);
            }
            return asset;
        }
    }
}
