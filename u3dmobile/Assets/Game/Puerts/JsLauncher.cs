//use the menu item "U3DMOBILE/Install Puerts" to install puerts,
//and add "U3DMOBILE_USE_PUERTS" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_PUERTS

using Puerts;

namespace U3DMobile
{
    public class JsLauncher : Singleton<JsLauncher>
    {
        public static JsLauncher instance { get { return GetInstance(); } }

        private JsEnv _environment;

        public JsEnv GetEnvironment()
        {
            if (_environment == null)
            {
                _environment = new JsEnv();
            }
            return _environment;
        }

        public JsEnv environment
        {
            get { return GetEnvironment(); }
        }

        public void Start()
        {
            string file = ProjectConfig.BundleManuscriptFile;
            string code = AssetManager.instance.LoadString(file);

            if (string.IsNullOrWhiteSpace(code))
            {
                Log.Error("failed to load the javascript bundle.");
                return;
            }

            environment.Eval(code, file);
        }
    }
}

#endif
