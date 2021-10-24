//use the menu item "U3DMOBILE/Install FairyGUI Runtime" to install the runtime,
//and add "U3DMOBILE_USE_FAIRYGUI" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI

using FairyGUI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace U3DMobile
{
    public abstract class FUIPanel
    {
        //NOTE: these fields need assigning in the constructor of the subclass.
        protected FUIAssetFrom assetFrom   { set; get; }
        protected FUIShowStyle showStyle   { set; get; }
        protected string       packageName { set; get; }
        protected string       panelName   { set; get; }

        private FUIPanelHandle _handle = new FUIPanelHandle();

        public void Open(Action completion)
        {
            _handle.assetFrom   = assetFrom;
            _handle.showStyle   = showStyle;
            _handle.packageName = packageName;
            _handle.panelName   = panelName;

            _handle.createAction  = OnCreate;
            _handle.showAction    = OnShow;
            _handle.hideAction    = OnHide;
            _handle.destroyAction = OnDestroy;

            FUIManager.instance.Open(_handle, completion);
        }

        public void Close(Action completion)
        {
            FUIManager.instance.Close(_handle, completion);
        }

        protected virtual void OnCreate () { BindControls(); }
        protected virtual void OnShow   () {}
        protected virtual void OnHide   () {}
        protected virtual void OnDestroy() {}

        private void BindControls()
        {
            var controls = new Dictionary<string, GObject>();
            
            var count = _handle.window.contentPane.numChildren;
            for (int index = 0; index < count; ++index)
            {
                GObject control = _handle.window.contentPane.GetChildAt(index);
                controls.Add(control.name, control);
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = GetType().GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                if (!controls.ContainsKey(field.Name))
                {
                    continue;
                }

                GObject control = controls[field.Name];
                if (!field.FieldType.IsAssignableFrom(control.GetType()))
                {
                    Log.Error("the field '{0}' is incompatible with the control type", field.Name);
                    continue;
                }
                
                field.SetValue(this, control);
            }
        }
    }
}

#endif
