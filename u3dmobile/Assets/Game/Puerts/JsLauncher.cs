//use the menu items "U3DMOBILE/Install XX" to install fairy-gui runtime and puerts,
//and add the macros on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_FAIRYGUI && U3DMOBILE_USE_PUERTS

using FairyGUI;
using Puerts;

namespace U3DMobile
{
    class JsLauncher : Singleton<JsLauncher>
    {
        public static JsLauncher instance { get { return GetInstance(); } }

        private JsEnv _environment;

        public JsEnv GetEnvironment()
        {
            if (_environment == null)
            {
                _environment = new JsEnv();

                //IMPORTANT: declare the delegate types need to passed from ts to c#.

                //GList.itemRenderer: (int index, GObject item) => void
                _environment.UsingAction<int, GObject>();
                //GList.itemProvider: (int index) => string
                _environment.UsingFunc<int, string>();
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
                Log.Error($"failed to load the javascript bundle.");
                return;
            }

            environment.Eval(code, file);
        }
    }
}

#endif
