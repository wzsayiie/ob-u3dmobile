//use the menu item "U3DMOBILE/Install FairyGUI Runtime" to install the runtime,
//and add "U3DMOBILE_USE_FAIRYGUI" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI

using FairyGUI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace U3DMobile
{
    public enum PackageSource
    {
        Bundle = 0,
        Static = 1,
    }

    public class PackageManager : Singleton<PackageManager>
    {
        public static PackageManager instance { get { return GetInstance(); } }

        private Dictionary<string, UIPackage> _addedPackages = new Dictionary<string, UIPackage>();

        public void AddPackage(PackageSource source, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Log.Error("try load package with empty name");
                return;
            }
            if (_addedPackages.ContainsKey(name))
            {
                return;
            }

            string descPath = GetDescriptionPath(source, name);
            byte[] descData = AssetManager.instance.LoadBytes(descPath);

            UIPackage package = UIPackage.AddPackage(descData, name,
                (string atlasName, string atlasExt, Type type, PackageItem item) =>
                {
                    if (type == typeof(Texture))
                    {
                        string  texPath = GetTexturePath(source, atlasName, atlasExt);
                        Texture texData = AssetManager.instance.LoadTexture(texPath);

                        item.owner.SetItemAsset(item, texData, DestroyMethod.Custom);
                    }
                });

            if (package == null)
            {
                Log.Error("there is no package {0}", name);
                return;
            }
            _addedPackages.Add(name, package);
        }

        private string GetDescriptionPath(PackageSource source, string descName)
        {
            string directory = GetAssetDirectory(source);
            return $"{directory}/{descName}_fui.bytes";
        }

        private string GetTexturePath(PackageSource source, string atlasName, string atlasExt)
        {
            string directory = GetAssetDirectory(source);
            return $"{directory}/{atlasName}{atlasExt}";
        }

        private string GetAssetDirectory(PackageSource source)
        {
            if (source == PackageSource.Bundle)
            {
                return ProjectConfig.BundleFGUIAssetDir;
            }
            else
            {
                return ProjectConfig.StaticFGUIAssetDir;
            }
        }

        public GObject CreateElement(PackageSource source, string pkgName, string resName)
        {
            if (string.IsNullOrWhiteSpace(pkgName))
            {
                Log.Error("try create a ui object with empty package name");
                return null;
            }
            if (string.IsNullOrWhiteSpace(resName))
            {
                Log.Error("try create a ui object with empty resource name");
                return null;
            }

            AddPackage(source, pkgName);

            GObject element = UIPackage.CreateObject(pkgName, resName);
            if (element == null)
            {
                Log.Error("failed to create '{0}/{1}'", pkgName, resName);
            }
            return element;
        }
    }
}

#endif
