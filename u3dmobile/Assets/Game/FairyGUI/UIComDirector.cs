//use the menu item "U3DMOBILE/Install FairyGUI Runtime" to install the runtime,
//and add "U3DMOBILE_USE_FAIRYGUI" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI

using FairyGUI;

namespace U3DMobile
{
    class UIComDirector
    {
        private bool          _filled ;
        private GObject       _element;
        private PackageSource _source ;
        private string        _pkgName;
        private string        _resName;

        private Window _window ;
        private UICom  _com    ;
        private bool   _created;
        private bool   _shown  ;

        public Window window { get { return _window; } }
        public UICom  com    { get { return _com   ; } }
        public bool   shown  { get { return _shown ; } }

        public UIComDirector(GObject element = null)
        {
            SetElement(element);
        }

        public UIComDirector(PackageSource source, string pkgName, string resName)
        {
            SetResource(source, pkgName, resName);
        }

        public void SetElement(GObject element)
        {
            if (element == null)
            {
                return;
            }

            //NOTE: cause the director needs to control the life cycle of the ui,
            //it can only be filled once.
            if (_filled)
            {
                return;
            }

            _filled  = true   ;
            _element = element;
        }

        public void SetResource(PackageSource source, string pkgName, string resName)
        {
            if (string.IsNullOrWhiteSpace(pkgName) || string.IsNullOrWhiteSpace(resName))
            {
                return;
            }
            if (_filled)
            {
                return;
            }

            _filled  = true   ;
            _source  = source ;
            _pkgName = pkgName;
            _resName = resName;
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
                if (_element == null)
                {
                    _element = PackageManager.instance.CreateElement(_source, _pkgName, _resName);
                }

                _window = new Window
                {
                    contentPane = _element.asCom,
                };

                _com = new UICom(_element, _resName);
                _com.BindOutlets(this);

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
                _com.UnbindOutlets(this);
                _com = null;

                _window.Dispose();
                _window = null;

                _element = null;

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
