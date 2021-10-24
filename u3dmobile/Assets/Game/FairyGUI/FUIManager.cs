//use the menu item "U3DMOBILE/Install FairyGUI Runtime" to install the runtime,
//and add "U3DMOBILE_USE_FAIRYGUI" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI

using FairyGUI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace U3DMobile
{
    public enum FUIAssetFrom
    {
        Bundle, //dynamic bundle is default.
        Static,
    }

    public enum FUIShowStyle
    {
        Stack, //stack style is default.
        Float,
    }

    public class FUIPanelHandle
    {
        //these fields need user assigning:
        public FUIAssetFrom assetFrom;
        public FUIShowStyle showStyle;
        public string       packageName;
        public string       panelName;

        public Action createAction;
        public Action showAction;
        public Action hideAction;
        public Action destroyAction;

        //the fields managed by FUIManager.
        public Window window;
    }

    public class FUIManager : Singleton<FUIManager>
    {
        public static FUIManager instance { get { return GetInstance(); } }

        private Dictionary<string, UIPackage> _addedPackages = new Dictionary<string, UIPackage>();

        private List<FUIPanelHandle> _stackPanels = new List<FUIPanelHandle>();
        private List<FUIPanelHandle> _floatPanels = new List<FUIPanelHandle>();

        private bool _isSwitching;

        public void AddPackage(string name, FUIAssetFrom from)
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

            string descPath = DescriptDataPath(name, from);
            byte[] descData = AssetManager.instance.LoadBytes(descPath);

            UIPackage package = UIPackage.AddPackage(descData, name,
                (string atlasName, string atlasExt, Type type, PackageItem item) =>
                {
                    if (type == typeof(Texture))
                    {
                        string  texPath = TextureDataPath(atlasName, atlasExt, from);
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

        private string DescriptDataPath(string descName, FUIAssetFrom assetFrom)
        {
            switch (assetFrom)
            {
                case FUIAssetFrom.Bundle: return $"{ProjectConfig.BundleFGUIAssetDir}/{descName}_fui.bytes";
                case FUIAssetFrom.Static: return $"{ProjectConfig.StaticFGUIAssetDir}/{descName}_fui.bytes";
            }
            return null;
        }

        private string TextureDataPath(string atlasName, string atlasExt, FUIAssetFrom assetFrom)
        {
            switch (assetFrom)
            {
                case FUIAssetFrom.Bundle: return $"{ProjectConfig.BundleFGUIAssetDir}/{atlasName}{atlasExt}";
                case FUIAssetFrom.Static: return $"{ProjectConfig.StaticFGUIAssetDir}/{atlasName}{atlasExt}";
            }
            return null;
        }

        public void Open(FUIPanelHandle handle, Action completion)
        {
            if (handle == null)
            {
                Log.Error("try open panel with empty handle");
                return;
            }
            if (string.IsNullOrWhiteSpace(handle.packageName))
            {
                Log.Error("try open panel with empty package name");
                return;
            }
            if (string.IsNullOrWhiteSpace(handle.panelName))
            {
                Log.Error("try open panel with emtpy panel name");
                return;
            }
            if (_isSwitching)
            {
                Log.Error("try open '{0}' but a switching in process", handle.panelName);
                return;
            }

            AddPackage(handle.packageName, handle.assetFrom);

            BeginSwitch();

            //create panel:
            handle.window = new Window();
            handle.window.contentPane = UIPackage.CreateObject(handle.packageName, handle.panelName).asCom;

            if (handle.showStyle == FUIShowStyle.Stack)
            {
                _stackPanels.Add(handle);
                handle.window.sortingOrder = _stackPanels.Count;
            }
            else
            {
                _floatPanels.Add(handle);

                //NOTE: here "sortingOrder" offset is 1000,
                //to ensure that the float panels is on top of the stack panels.
                handle.window.sortingOrder = 1000 + _floatPanels.Count;
            }

            handle.createAction?.Invoke();

            //show panel:
            handle.showAction?.Invoke();
            handle.window.Show();

            Scheduler.instance.RunAfterSeconds(0, () =>
            {
                EndSwitch();
                completion?.Invoke();
            });
        }

        public void Close(FUIPanelHandle handle, Action completion)
        {
            if (handle == null)
            {
                Log.Error("try close a panel with empty handle");
                return;
            }
            if (_isSwitching)
            {
                Log.Error("try close '{0}' but a switching in process", handle.panelName);
                return;
            }

            if (handle.showStyle == FUIShowStyle.Stack)
            {
                if (_stackPanels.Count == 0)
                {
                    Log.Error("try close '{0}' but there is no stack panels", handle.panelName);
                    return;
                }

                int lastIndex = _stackPanels.Count - 1;
                if (handle != _stackPanels[lastIndex])
                {
                    Log.Error("try close '{0}' but it's not on the topest", handle.panelName);
                    return;
                }

                BeginSwitch();

                //hide panel.
                handle.hideAction?.Invoke();
                handle.window.Hide();

                //destroy panel:
                handle.destroyAction?.Invoke();
                handle.window.Dispose();
                handle.window = null;

                _stackPanels.RemoveAt(lastIndex);

                Scheduler.instance.RunAfterSeconds(0, () =>
                {
                    EndSwitch();
                    completion?.Invoke();
                });
            }
            else
            {
                if (_floatPanels.Count == 0)
                {
                    Log.Error("try close '{0}' there is no float panels", handle.panelName);
                    return;
                }

                int index = _floatPanels.FindIndex(0, item => (item == handle));
                if (index == -1)
                {
                    Log.Error("try close '{0}' but it's not exist", handle.panelName);
                    return;
                }

                BeginSwitch();

                //hide panel.
                handle.hideAction?.Invoke();
                handle.window.Hide();

                //destroy panel:
                handle.destroyAction?.Invoke();
                handle.window.Dispose();
                handle.window = null;

                _floatPanels.RemoveAt(index);

                Scheduler.instance.RunAfterSeconds(0, () =>
                {
                    EndSwitch();
                    completion?.Invoke();
                });
            }
        }

        private void BeginSwitch()
        {
            _isSwitching = true;
        }

        private void EndSwitch()
        {
            _isSwitching = false;
        }
    }
}

#endif
