using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace U3DMobile.Editor
{
    static class Dependences
    {
        [MenuItem("U3DMOBILE/Install Puerts")]
        public static void InstallPuerts()
        {
            FileAssist.ResetDirectory("Assets/Puerts");

            var sources = new DependenceConfig
            {
                logsWord = "puerts-sources",

                thirdUrl = "https://github.com/Tencent/puerts/archive/refs/tags/Unity_Plugin_1.1.2.zip",
                localZip = "DEPENDENCES/puerts-sources.zip",
                unzipDir = "DEPENDENCES/puerts-sources",

                srcFiles = new List<string>
                {
                    "DEPENDENCES/puerts-sources/puerts-Unity_Plugin_1.1.2/unity/Assets/Puerts/Src",
                    "DEPENDENCES/puerts-sources/puerts-Unity_Plugin_1.1.2/unity/Assets/Puerts/Typing",
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
                localZip = "DEPENDENCES/puerts-plugins.tgz",
                unzipDir = "DEPENDENCES/puerts-plugins",

                srcFiles = new List<string>
                {
                    "DEPENDENCES/puerts-plugins/Plugins",
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
                localZip = "DEPENDENCES/fairygui.zip",
                unzipDir = "DEPENDENCES/fairygui",

                srcFiles = new List<string>
                {
                    "DEPENDENCES/fairygui/FairyGUI-unity-4.2.0/Assets/Resources",
                    "DEPENDENCES/fairygui/FairyGUI-unity-4.2.0/Assets/Scripts",
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
            public string unzipDir;

            public List<string> srcFiles;
            public List<string> dstFiles;
        }

        private static void InstallDependence(DependenceConfig config)
        {
            /**/Debug.Log($"[0/3] {config.logsWord} started");

            NetAssist.HttpGet(config.thirdUrl, config.localZip, () =>
            {
                Debug.Log($"[1/3] {config.logsWord} downloaded");

                FileAssist.ExtractFile(config.localZip, config.unzipDir);
                Debug.Log($"[2/3] {config.logsWord} extracted");

                for (int i = 0; i < config.srcFiles.Count; ++i)
                {
                    FileAssist.MovePath(config.srcFiles[i], config.dstFiles[i]);
                }
                Debug.Log($"[3/3] {config.logsWord} installed");
            });
        }
    }
}
