using System.IO;

namespace U3DMobile.Editor
{
    static class FileAssist
    {
        public static void CreateDirectoryIfNeed(string path)
        {
            if (Directory.Exists(path))
            {
                return;
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
                Directory.CreateDirectory(path);
            }
            else
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void ResetDirectory(string path)
        {
            DeletePath(path);
            Directory.CreateDirectory(path);
        }

        public static void DeletePath(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static void MovePath(string srcPath, string dstPath)
        {
            DeletePath(dstPath);

            string dstParent = Path.GetDirectoryName(dstPath);
            CreateDirectoryIfNeed(dstParent);

            if (Directory.Exists(srcPath))
            {
                Directory.Move(srcPath, dstPath);
            }
            else if (File.Exists(srcPath))
            {
                File.Move(srcPath, dstPath);
            }
        }

        public static void ExtractFile(string archivePath, string targetDirectory)
        {
            ResetDirectory(targetDirectory);

            //use the command "tar" here.
            //the tool from System.IO.Compression doesn't support formats such as "tgz".
            //NOTE: for window, carries "tar" from windows 10.
            var info = new System.Diagnostics.ProcessStartInfo();
            {
                info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                info.FileName    = "tar";
                info.Arguments   = $"-xf {archivePath} -C {targetDirectory}";
            }

            var process = new System.Diagnostics.Process
            {
                StartInfo = info,
            };
            process.Start();
            process.WaitForExit();
        }
    }
}
