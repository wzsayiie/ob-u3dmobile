//use the menu item "U3DMOBILE/Install Puerts" to install puerts,
//and add "U3DMOBILE_USE_PUERTS" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_PUERTS

using Puerts;

namespace U3DMobile
{
    public class JsLauncher : Singleton<JsLauncher>
    {
        public static JsLauncher instance { get { return GetInstance(); } }

        private bool  _isStared;
        private JsEnv _jsEnv;

        public JsEnv GetJsEnv()
        {
            if (_jsEnv == null)
            {
                _jsEnv = new JsEnv();
            }
            return _jsEnv;
        }

        public JsEnv jsEnv
        {
            get { return GetJsEnv(); }
        }

        public void Start()
        {
            if (_isStared)
            {
                return;
            }

            string file = ProjectConfig.BundleManuscriptFile;
            string code = AssetManager.instance.LoadString(file);
            if (!string.IsNullOrWhiteSpace(code))
            {
                jsEnv.Eval(code, file);
            }

            _isStared = true;
        }
    }
}

#endif
