using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace Fix.Editor
{
    public static class UnityPackageUtils
    {
        public static void UnPack(string packagePath, string destinationFolderPath)
        {
            var tempFolder = Path.Combine("UnPack_TEMP".GetWorkspaceFolder(),
                "UnityPackage_7bafca7a237c");

            tempFolder.Rmdir();
            tempFolder.Mkdir();
            Unzip(packagePath, tempFolder);
            foreach (var sourceFolderPath in Directory.GetDirectories(tempFolder))
            {
                var pathNameData = File.ReadAllLines(Path.Combine(sourceFolderPath, "pathname"));
                var pathname = pathNameData[0];

                var outputPath = Path.Combine(destinationFolderPath, pathname);

                var assetPath = Path.Combine(sourceFolderPath, "asset");
                if (File.Exists(assetPath))
                {
                    var outputFolder = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                    outputFolder.Mkdir();
                    if (!File.Exists(outputPath)) File.Copy(assetPath, outputPath, true);
                }
                else
                {
                    var outputFolder = outputPath;
                    outputFolder.Mkdir();
                }

                // var assetMetaPath = Path.Combine(sourceFolderPath, "asset.meta");
                var assetMetaPath = Path.Combine(sourceFolderPath, "metaData");
                var metaPath = outputPath + ".meta";
                if (!File.Exists(assetMetaPath)) continue;
                if (!File.Exists(metaPath))
                    File.Copy(assetMetaPath, metaPath, true);
            }
        }

        public static void Unzip(string source, string target)
        {
            string arguments = $"-xzf \"{source}\" -C \"{target}\"";
            using (var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "tar",
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            })
            {
                process.ErrorDataReceived += (o, e) => Debug.LogError(e.Data);
                process.Start();
                process.StandardInput.WriteLine($@"exit");
                process.WaitForExit(3000);
            }
        }
    }
}