using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pinknose.DistributedWorkers.Clients;
using System;
using System.Diagnostics;
using System.IO;

namespace Pinknose.DistributedWorkers.UnitTests
{
    [TestClass]
    public class KeyUtilityTests
    {
        #region Fields

        private static string keyUtilPath = @"..\..\..\..\Pinknose.KeyUtility\bin\Debug\netcoreapp3.1\key-util.exe";

        #endregion Fields

        #region Methods

        [TestMethod]
        [Timeout(10000)]
        public void CreateAllEncryptedServerKeyFormats()
        {
            string path = Path.Combine(Path.GetTempPath(), "keyUtilTest");

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);

            string systemName = "aSystem";
            string baseKeyFilePath = Path.Combine(path, $"{systemName}-server");

            var proc = StartKeyUtil($"generate-server -s {systemName} --256 -j -l -u -d \"{path}\"", "pass1", "pass1");

            proc.StandardInput.WriteLine("pass1");
            proc.StandardInput.WriteLine("pass1");

            proc.WaitForExit();

            Assert.IsTrue(proc.ExitCode >= 0);

            Assert.IsTrue(File.Exists(baseKeyFilePath + ".priv"));
            Assert.IsTrue(File.Exists(baseKeyFilePath + ".privu"));
            Assert.IsTrue(File.Exists(baseKeyFilePath + ".privl"));
            Assert.IsTrue(File.Exists(baseKeyFilePath + ".pub"));

            var priv = MessageClientIdentity.ImportFromFile(baseKeyFilePath + ".priv", "pass1");
            var privu = MessageClientIdentity.ImportFromFile(baseKeyFilePath + ".privu");
            var privl = MessageClientIdentity.ImportFromFile(baseKeyFilePath + ".privl");
            var pub = MessageClientIdentity.ImportFromFile(baseKeyFilePath + ".pub");

            Assert.AreEqual(priv.IdentityHash, privu.IdentityHash);
            Assert.AreEqual(priv.IdentityHash, privl.IdentityHash);
            Assert.AreEqual(priv.IdentityHash, pub.IdentityHash);

            Directory.Delete(path, true);

            proc.Dispose();
        }

        private static Process StartKeyUtil(string arguments = "", string password1 = "", string password2 = "")
        {
            int passwordPromptCount = 0;

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

            process.OutputDataReceived += (sender, e) =>
            {
                Console.WriteLine(e.Data);

                return;

                if (e.Data != null && e.Data.Contains("key-util"))
                {
                    if (passwordPromptCount == 0)
                    {
                        process.StandardInput.WriteLine(password1);
                        passwordPromptCount++;
                    }
                    else if (passwordPromptCount == 1)
                    {
                        process.StandardInput.WriteLine(password2);
                    }
                }
            }; 
            
            process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }

#endregion Methods
    }
}