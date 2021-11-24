//use the menu item "U3DMOBILE/Install FairyGUI Runtime" to install the runtime,
//and add "U3DMOBILE_USE_FAIRYGUI" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI

using FairyGUI;

namespace U3DMobile
{
    public class UIComDirector
    {
        private PackageSource _source ;
        private string        _pkgName;
        private string        _resName;

        private Window _window ;
        private UICom  _content;
        private bool   _created;
        private bool   _shown  ;

        public Window window  { get { return _window ; } }
        public UICom  content { get { return _content; } }
        public bool   shown   { get { return _shown  ; } }

        public void SetResource(PackageSource source, string pkgName, string resName)
        {
            _source  = source ;
            _pkgName = pkgName;
            _resName = resName;
        }

        public void Show()
        {
            if (!_created)
            {
                //actions:
                GObject element = PackageManager.instance.CreateElement(_source, _pkgName, _resName);

                _window = new Window();
                _window.contentPane = element.asCom;

                _content = new UICom(element);
                _content.BindOutlets(this);

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
                _content.SetRootElement(null);
                _content.UnbindOutlets(this);
                _content = null;

                _window.Dispose();
                _window = null;

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
