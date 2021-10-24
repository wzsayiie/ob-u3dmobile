using System.IO;
using UnityEditor;
using UnityEngine;

namespace U3DMobile
{
    public class AssetManager : Singleton<AssetManager>
    {
        public static AssetManager instance { get { return GetInstance(); } }

        //IMPORTANT:
        //when loading assets, AssetManager need the full path starts with "Assets/", include extension name.

        public byte[]     LoadBytes  (string path) { return LoadAsset(path, LoadStaticBytes  , LoadBundleBytes  ); }
        public string     LoadString (string path) { return LoadAsset(path, LoadStaticString , LoadBundleString ); }
        public GameObject LoadPrefab (string path) { return LoadAsset(path, LoadStaticPrefab , LoadBundlePrefab ); }
        public Texture    LoadTexture(string path) { return LoadAsset(path, LoadStaticTexture, LoadBundleTexture); }

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

        private Texture LoadStaticTexture(string path)
        {
            return Resources.Load<Texture>(path);
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

        private Texture LoadBundleTexture(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture>(path);
        }

        private string ConvertPath(string path, out bool isStatic)
        {
            const string keyword = "Resources/";
            int index = path.LastIndexOf(keyword);

            //the path is "Assets/../Resources/..".
            if (index >= 0)
            {
                isStatic = true;

                string relativePath = path.Substring(index + keyword.Length);

                //NOTE: the assets path under "resources/" need to ignore the extension name.
                string relativeName = Path.ChangeExtension(relativePath, null);
                return relativeName;
            }
            //the path is "Assets/../.."
            else
            {
                isStatic = false;
                return path;
            }
        }

        private delegate T Loader<T>(string path);

        private T LoadAsset<T>(string path, Loader<T> staticLoader, Loader<T> bundleLoader) where T: class
        {
            if (string.IsNullOrWhiteSpace(path))
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
