//use the menu item "U3DMOBILE/Install FairyGUI Runtime" to install the runtime,
//and add "U3DMOBILE_USE_FAIRYGUI" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI

using FairyGUI;

namespace U3DMobile
{
    class UIComDirector
    {
        private bool          _filled;
        private GComponent    _theGCom;
        private PackageSource _pkgSource;
        private string        _pkgName;
        private string        _comName;

        private Window _window ;
        private UICom  _com    ;
        private bool   _created;
        private bool   _shown  ;

        public Window window { get { return _window; } }
        public UICom  com    { get { return _com   ; } }
        public bool   shown  { get { return _shown ; } }

        public void SetGCom(GComponent aGCom)
        {
            if (_filled)
            {
                Log.Error($"can not set ui com repeatedly");
                return;
            }

            if (aGCom != null)
            {
                _filled  = true ;
                _theGCom = aGCom;
            }
        }

        public void SetResource(PackageSource source, string pkgName, string comName)
        {
            if (_filled)
            {
                Log.Error($"can not set package resource repeatedly");
                return;
            }

            if (string.IsNullOrWhiteSpace(pkgName))
            {
                Log.Error($"try to set empty package name");
                return;
            }
            if (string.IsNullOrWhiteSpace(comName))
            {
                Log.Error($"try to set empty component name");
                return;
            }

            _filled    = true   ;
            _pkgSource = source ;
            _pkgName   = pkgName;
            _comName   = comName;
        }

        public void Show()
        {
            if (!_filled)
            {
                return;
            }

            if (!_created)
            {
                //actions:
                if (_theGCom == null)
                {
                    PackageManager manager = PackageManager.instance;
                    GObject aGObject = manager.CreateGObject(_pkgSource, _pkgName, _comName);

                    _theGCom = aGObject.asCom;
                    if (_theGCom == null)
                    {
                        Log.Error($"failed to create '{_pkgName}/{_comName}'");
                        return;
                    }
                }

                _window = new Window
                {
                    contentPane = _theGCom,
                };

                _com = new UICom();
                _com.SetGCom(_theGCom, _comName);
                _com.Bind(this);

                //notification.
                _created = true;
                OnCreate();
            }
            if (!_shown)
            {
                //notification.
                _shown = true;
                OnShow();

                //actions.
                _window.Show();
            }
        }

        public void Hide()
        {
            if (_shown)
            {
                //actions.
                _window.Hide();
                
                //notification.
                _shown = false;
                OnHide();
            }
        }

        public void Dispose()
        {
            if (_shown)
            {
                //notification.
                _shown = false;
                OnHide();
            }
            if (_created)
            {
                //actions:
                _com = null;

                _window.Dispose();
                _window = null;

                _theGCom = null;

                //notification.
                _created = false;
                OnDestroy();
            }
        }

        protected virtual void OnCreate () {}
        protected virtual void OnShow   () {}
        protected virtual void OnHide   () {}
        protected virtual void OnDestroy() {}
    }
}

#endif
