using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.InteropServices;
using RGiesecke.DllExport;

using System.Diagnostics;

namespace SASnPy
{
    public class SASnPyHelper
    {
        static string sPythonPath = string.Empty;
        static string sTempDirectory = string.Empty;

        static StringBuilder outputstream;
        static StringBuilder errorstream;

        // ----------------------

        [DllExport("SessionTempLocation", CallingConvention = CallingConvention.StdCall)]
        public static string SessionTempLocation()
        {
            return TempWorkingDirectory();
        }

        static string TempWorkingDirectory()
        {
            if (string.IsNullOrEmpty(sTempDirectory))
            {
                sTempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(sTempDirectory);
                Directory.CreateDirectory(Path.Combine(sTempDirectory, "DataIn"));
                Directory.CreateDirectory(Path.Combine(sTempDirectory, "DataOut"));
                Directory.CreateDirectory(Path.Combine(sTempDirectory, "DisplayContent"));
            }
            return sTempDirectory;
        }

        [DllExport("TestPI", CallingConvention = CallingConvention.StdCall)]
        public static double pi()
        {
            return Math.PI;
        }

        [DllExport("SetPythonPath", CallingConvention = CallingConvention.StdCall)]
        public static void SetPythonPath(string sPath)
        {
            sPythonPath = sPath;
        }

        [DllExport("ExecuteScript", CallingConvention = CallingConvention.StdCall)]
        public static int ExecuteScript(string sScript)
        {
            string sScriptFile = sScript;
            string output = string.Empty;
            string error = string.Empty;

            outputstream = new StringBuilder();
            errorstream = new StringBuilder();

            string sTempDir = TempWorkingDirectory();
            outputstream.AppendLine("Attempting to execute:[" + sScriptFile + "]");

            if (!File.Exists(sScriptFile))
            {
                string sTempFile = Path.Combine(sTempDir, Path.GetRandomFileName());
                File.WriteAllText(sTempFile, sScript);
                sScriptFile = sTempFile;
            }

            using (Process process = new Process())
            {
                try
                {
                    process.StartInfo.FileName = sPythonPath;
                    process.StartInfo.Arguments = sScriptFile;

                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.OutputDataReceived += Process_OutputDataReceived;
                    process.ErrorDataReceived += Process_ErrorDataReceived;

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    errorstream.AppendLine(ex.Message);
                }
                finally
                {
                    output = outputstream.ToString();
                    error = errorstream.ToString().Trim();
                }
            }

            File.WriteAllText(Path.Combine(sTempDirectory, "DisplayContent", "output.txt"), output);
            File.WriteAllText(Path.Combine(sTempDirectory, "DisplayContent", "error.txt"), error);

            return string.IsNullOrWhiteSpace(error) ? 0 : 1;
        }

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            errorstream.AppendLine(e.Data);
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            outputstream.AppendLine(e.Data);
        }
    }

}
