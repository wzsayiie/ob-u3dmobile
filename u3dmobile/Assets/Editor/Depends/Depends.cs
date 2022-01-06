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

            var sources = new DependConfig
            {
                logsWord = "puerts-sources",

                thirdUrl = "https://github.com/Tencent/puerts/archive/refs/tags/Unity_Plugin_1.1.2.zip",
                localZip = "DEPENDS/puerts-sources.zip",
                unzipDir = "DEPENDS/puerts-sources",

                srcFiles = new List<string>
                {
                    "DEPENDS/puerts-sources/puerts-Unity_Plugin_1.1.2/unity/Assets/Puerts/Src",
                    "DEPENDS/puerts-sources/puerts-Unity_Plugin_1.1.2/unity/Assets/Puerts/Typing",
                },
                dstFiles = new List<string>
                {
                    "Assets/Puerts/Src",
                    "Assets/Puerts/Typing",
                },
            };
            InstallDepend(sources);

            var plugins = new DependConfig
            {
                logsWord = "puerts-plugins",

                thirdUrl = "https://github.com/Tencent/puerts/releases/download/Unity_Plugin_1.1.2/Plugins_V8_ver14.tgz",
                localZip = "DEPENDS/puerts-plugins.tgz",
                unzipDir = "DEPENDS/puerts-plugins",

                srcFiles = new List<string>
                {
                    "DEPENDS/puerts-plugins/Plugins",
                },
                dstFiles = new List<string>
                {
                    "Assets/Puerts/Plugins",
                },
            };
            InstallDepend(plugins);
        }

        [MenuItem("U3DMOBILE/Install FairyGUI Runtime")]
        public static void InstallFairyGUIRuntime()
        {
            FileAssist.ResetDirectory("Assets/FairyGUI");

            var sources = new DependConfig
            {
                logsWord = "fairygui-runtime",

                thirdUrl = "https://github.com/fairygui/FairyGUI-unity/archive/refs/tags/4.2.0.zip",
                localZip = "DEPENDS/fairygui.zip",
                unzipDir = "DEPENDS/fairygui",

                srcFiles = new List<string>
                {
                    "DEPENDS/fairygui/FairyGUI-unity-4.2.0/Assets/Resources",
                    "DEPENDS/fairygui/FairyGUI-unity-4.2.0/Assets/Scripts",
                },
                dstFiles = new List<string>
                {
                    "Assets/FairyGUI/Resources",
                    "Assets/FairyGUI/Scripts",
                },
            };
            InstallDepend(sources);
        }

        private class DependConfig
        {
            public string logsWord;

            public string thirdUrl;
            public string localZip;
            public string unzipDir;

            public List<string> srcFiles;
            public List<string> dstFiles;
        }

        private static void InstallDepend(DependConfig config)
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
