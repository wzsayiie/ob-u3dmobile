using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace U3DMobile.Editor
{
    static class Dependence
    {
        [MenuItem("U3DMOBILE/Install Puerts")]
        public static void InstallPuerts()
        {
            EditorAssist.instance.ResetDirectory("Assets/Puerts");

            var sources = new DependenceConfig();
            {
                sources.logsWord = "puerts-sources";

                sources.thirdUrl = "https://github.com/Tencent/puerts/archive/refs/tags/Unity_Plugin_1.0.18.zip";
                sources.localZip = "DEPENDENCE/puerts-sources.zip";
                sources.raiseDir = "DEPENDENCE/puerts-sources";

                sources.srcFiles = new List<string>
                {
                    "DEPENDENCE/puerts-sources/puerts-Unity_Plugin_1.0.18/unity/Assets/Puerts/Src",
                    "DEPENDENCE/puerts-sources/puerts-Unity_Plugin_1.0.18/unity/Assets/Puerts/Typing",
                };
                sources.dstFiles = new List<string>
                {
                    "Assets/Puerts/Src",
                    "Assets/Puerts/Typing",
                };
            }
            InstallDependence(sources);

            var plugins = new DependenceConfig();
            {
                plugins.logsWord = "puerts-plugins";

                plugins.thirdUrl = "https://github.com/Tencent/puerts/releases/download/Unity_Plugin_1.0.18/Plugins_V8_ver13.tgz";
                plugins.localZip = "DEPENDENCE/puerts-plugins.tgz";
                plugins.raiseDir = "DEPENDENCE/puerts-plugins";

                plugins.srcFiles = new List<string>
                {
                    "DEPENDENCE/puerts-plugins/Plugins",
                };
                plugins.dstFiles = new List<string>
                {
                    "Assets/Puerts/Plugins",
                };
            }
            InstallDependence(plugins);
        }

        [MenuItem("U3DMOBILE/Install FairyGUI Runtime")]
        public static void InstallFairyGUIRuntime()
        {
            EditorAssist.instance.ResetDirectory("Assets/FairyGUI");

            var sources = new DependenceConfig();
            {
                sources.logsWord = "fairygui-runtime";

                sources.thirdUrl = "https://github.com/fairygui/FairyGUI-unity/archive/refs/tags/4.2.0.zip";
                sources.localZip = "DEPENDENCE/fairygui.zip";
                sources.raiseDir = "DEPENDENCE/fairygui";

                sources.srcFiles = new List<string>
                {
                    "DEPENDENCE/fairygui/FairyGUI-unity-4.2.0/Assets/Resources",
                    "DEPENDENCE/fairygui/FairyGUI-unity-4.2.0/Assets/Scripts",
                };
                sources.dstFiles = new List<string>
                {
                    "Assets/FairyGUI/Resources",
                    "Assets/FairyGUI/Scripts",
                };
            }
            InstallDependence(sources);
        }

        private class DependenceConfig
        {
            public string       logsWord;
            public string       thirdUrl;
            public string       localZip;
            public string       raiseDir;
            public List<string> srcFiles;
            public List<string> dstFiles;
        }

        private static void InstallDependence(DependenceConfig config)
        {
            Debug.LogFormat("[0/3] {0} processing", config.logsWord);
            EditorAssist.instance.GetRemote(config.thirdUrl, config.localZip, () =>
            {
                Debug.LogFormat("[1/3] {0} downloaded", config.logsWord);

                EditorAssist.instance.ExtractFile(config.localZip, config.raiseDir);
                Debug.LogFormat("[2/3] {0} extracted", config.logsWord);

                for (int i = 0; i < config.srcFiles.Count; ++i)
                {
                    EditorAssist.instance.MovePath(config.srcFiles[i], config.dstFiles[i]);
                }
                Debug.LogFormat("[3/3] {0} moved", config.logsWord);
            });
        }
    }
}
