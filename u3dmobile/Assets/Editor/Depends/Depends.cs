using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace U3DMobile.Editor
{
    static class Depends
    {
        [MenuItem("U3DMOBILE/Install Puerts")]
        public static void InstallPuerts()
        {
            FileAssist.ResetDirectory("Assets/Puerts");

            var sources = new DependenceConfig
            {
                logsWord = "puerts-sources",

                thirdUrl = "https://github.com/Tencent/puerts/archive/refs/tags/Unity_Plugin_1.1.2.zip",
                localZip = "DEPENDENCE/puerts-sources.zip",
                raiseDir = "DEPENDENCE/puerts-sources",

                srcFiles = new List<string>
                {
                    "DEPENDENCE/puerts-sources/puerts-Unity_Plugin_1.1.2/unity/Assets/Puerts/Src",
                    "DEPENDENCE/puerts-sources/puerts-Unity_Plugin_1.1.2/unity/Assets/Puerts/Typing",
                },
                dstFiles = new List<string>
                {
                    "Assets/Puerts/Src",
                    "Assets/Puerts/Typing",
                },
            };
            InstallDependence(sources);

            var plugins = new DependenceConfig
            {
                logsWord = "puerts-plugins",

                thirdUrl = "https://github.com/Tencent/puerts/releases/download/Unity_Plugin_1.1.2/Plugins_V8_ver14.tgz",
                localZip = "DEPENDENCE/puerts-plugins.tgz",
                raiseDir = "DEPENDENCE/puerts-plugins",

                srcFiles = new List<string>
                {
                    "DEPENDENCE/puerts-plugins/Plugins",
                },
                dstFiles = new List<string>
                {
                    "Assets/Puerts/Plugins",
                },
            };
            InstallDependence(plugins);
        }

        [MenuItem("U3DMOBILE/Install FairyGUI Runtime")]
        public static void InstallFairyGUIRuntime()
        {
            FileAssist.ResetDirectory("Assets/FairyGUI");

            var sources = new DependenceConfig
            {
                logsWord = "fairygui-runtime",

                thirdUrl = "https://github.com/fairygui/FairyGUI-unity/archive/refs/tags/4.2.0.zip",
                localZip = "DEPENDENCE/fairygui.zip",
                raiseDir = "DEPENDENCE/fairygui",

                srcFiles = new List<string>
                {
                    "DEPENDENCE/fairygui/FairyGUI-unity-4.2.0/Assets/Resources",
                    "DEPENDENCE/fairygui/FairyGUI-unity-4.2.0/Assets/Scripts",
                },
                dstFiles = new List<string>
                {
                    "Assets/FairyGUI/Resources",
                    "Assets/FairyGUI/Scripts",
                },
            };
            InstallDependence(sources);
        }

        private class DependenceConfig
        {
            public string logsWord;

            public string thirdUrl;
            public string localZip;
            public string raiseDir;

            public List<string> srcFiles;
            public List<string> dstFiles;
        }

        private static void InstallDependence(DependenceConfig config)
        {
            Debug.LogFormat("[0/3] {0} started", config.logsWord);
            NetAssist.HttpGet(config.thirdUrl, config.localZip, () =>
            {
                Debug.LogFormat("[1/3] {0} downloaded", config.logsWord);

                FileAssist.ExtractFile(config.localZip, config.raiseDir);
                Debug.LogFormat("[2/3] {0} extracted", config.logsWord);

                for (int i = 0; i < config.srcFiles.Count; ++i)
                {
                    FileAssist.MovePath(config.srcFiles[i], config.dstFiles[i]);
                }
                Debug.LogFormat("[3/3] {0} installed", config.logsWord);
            });
        }
    }
}
