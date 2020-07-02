using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Pinknose.DistributedWorkers.UnitTests
{
    [TestClass]
    public class KeyUtilityTests
    {
        private static string keyUtilPath = @"..\..\..\..\Pinknose.KeyUtility\bin\Debug\netcoreapp3.1\key-util.exe";

        [TestMethod]
        public void CreateUnencyptedServerKey()
        {
            string path = Path.Combine(Path.GetTempPath(), "keyUtilTest");
            Directory.CreateDirectory(path);

            var proc = StartKeyUtil($"generate-server -s testSystem --256 -j -d={path}");

            proc.StandardInput.Write("n");
            proc.WaitForExit();
            Directory.Delete(path, true);

            proc.Dispose();
        }

        private static Process StartKeyUtil(string arguments)
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo(keyUtilPath, arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                }
            };

            process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }

    }
}
