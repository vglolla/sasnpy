using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web.UI;

using RGiesecke.DllExport;



namespace SASnPy
{
    public class SASnPyHelper
    {
        static string sPythonPath = string.Empty;
        static string sPlotTemplate = string.Empty;

        static string sTempDirectory = string.Empty;
        static string sTempDataInDir = string.Empty;
        static string sTempDataOutDir = string.Empty;
        static string sTempDisplayConentDir = string.Empty;
        static string sTempDataPlotsDir = string.Empty;

        static StringBuilder sbOutputStream;
        static StringBuilder sbErrorStream;
        static int iScriptCounter = 0;
        static Process procObj = null;

        static string sDLLPath;
        static bool bSessionActive = false;

        static Stack<string> stackErrors;
        static StreamWriter procInputStream;
        static object streamLock = new object();

        static SASnPyHelper()
        {
            sDLLPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            stackErrors = new Stack<string>();
        }

        // ----------------------

        [DllExport("PyStartSession", CallingConvention = CallingConvention.StdCall)]
        public static int PyStartSession()
        {
            stackErrors.Clear();

            if (string.IsNullOrEmpty(sPythonPath))
                stackErrors.Push("Python path not set");

            CreateTempDirectories();

            if (procObj != null && !procObj.HasExited)
                procObj.Kill();

            procObj = new Process();

            procObj.StartInfo.FileName = sPythonPath;
            procObj.StartInfo.Arguments = "-i";
            procObj.StartInfo.UseShellExecute = false;
            procObj.StartInfo.RedirectStandardInput = true;
            procObj.StartInfo.RedirectStandardOutput = true;
            procObj.StartInfo.RedirectStandardError = true;
            procObj.StartInfo.CreateNoWindow = true;

            sbOutputStream = new StringBuilder();
            sbErrorStream = new StringBuilder();

            procObj.OutputDataReceived += Process_OutputDataReceived;
            procObj.ErrorDataReceived += Process_ErrorDataReceived;
            iScriptCounter = -1;

            procObj.Start();
            procObj.BeginOutputReadLine();
            procObj.BeginErrorReadLine();

            string sPythonScriptDir = @"C:/GHRepositories/sasnpy/Python-Scripts";

            procInputStream = procObj.StandardInput;
            procInputStream.AutoFlush = true;

            procInputStream.WriteLine("import sys");
            procInputStream.WriteLine("sys.path.append('{0}')", sDLLPath.Replace('\\', '/'));
            procInputStream.WriteLine("sys.path.append('{0}')", sPythonScriptDir);
            procInputStream.WriteLine("import sasnpy");
            procInputStream.WriteLine("sasnpy._sasnpy_symbol_map['tempdir_datain'] = '{0}'", sTempDataInDir.Replace('\\', '/'));
            procInputStream.WriteLine("sasnpy._sasnpy_symbol_map['tempdir_dataout'] = '{0}'", sTempDataOutDir.Replace('\\', '/'));
            procInputStream.WriteLine("sasnpy._sasnpy_symbol_map['tempdir_displaycontent'] = '{0}'", sTempDisplayConentDir.Replace('\\', '/'));
            procInputStream.WriteLine("sasnpy._sasnpy_symbol_map['tempdir_plots'] = '{0}'", sTempDataPlotsDir.Replace('\\', '/'));

            procInputStream.Flush();
            bSessionActive = true;

            PyExecuteScriptCore("pass");

            return 0;
        }

        [DllExport("PyEndSession", CallingConvention = CallingConvention.StdCall)]
        public static int PyEndSession()
        {
            if (bSessionActive)
            {
                procInputStream.WriteLine("exit()");
                procInputStream.Flush();
                procInputStream.Close();
                procInputStream = null;

                bSessionActive = false;
                sTempDirectory = string.Empty;

                if (procObj != null && !procObj.HasExited)
                    procObj.Kill();
            }

            return 0;
        }

        [DllExport("PySetPath", CallingConvention = CallingConvention.StdCall)]
        public static int PySetPath(string sPath)
        {
            if (!File.Exists(sPath))
            {
                stackErrors.Push("Python path does not exist");
                return 1;
            }

            sPythonPath = sPath;
            return 0;
        }

        [DllExport("PyExecuteScript", CallingConvention = CallingConvention.StdCall)]
        public static int PyExecuteScript(string sScript)
        {
            if (!bSessionActive)
            {
                stackErrors.Push("Python session not started.");
                return 1;
            }

            return PyExecuteScriptCore(sScript);
        }

        public static int PyExecuteScriptCore(string sScript)
        {
            string sScriptFile = sScript;
            string output = string.Empty;
            string error = string.Empty;

            sbOutputStream = new StringBuilder();
            sbErrorStream = new StringBuilder();

            Stopwatch stopwatch = new Stopwatch();

            if (!File.Exists(sScriptFile))
            {
                string sTempFile = Path.Combine(sTempDirectory, Path.GetRandomFileName());
                File.WriteAllText(sTempFile, sScript);
                sScriptFile = sTempFile;
            }

            string sSentinelFile = Path.Combine(sTempDirectory, Path.GetRandomFileName()).Replace('\\', '/');

            try
            {
                stopwatch.Start();
                procInputStream.WriteLine("sasnpy.execute_script('{0}', '{1}')", sScriptFile, sSentinelFile);
                procInputStream.WriteLine(procInputStream.NewLine);
                procInputStream.Flush();

                while(!File.Exists(sSentinelFile))
                {
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                sbErrorStream.AppendLine(ex.Message);
            }
            finally
            {
                stopwatch.Stop();
                output = sbOutputStream.ToString();
                error = sbErrorStream.ToString().Trim();
                if (File.Exists(sSentinelFile))
                    File.Delete(sSentinelFile);
            }


            iScriptCounter++;
            string sOutputFile = string.Format("output-{0}.txt", iScriptCounter);
            string sErrorFile = string.Format("error-{0}.txt", iScriptCounter);
            string sMetaFile = string.Format("meta-{0}.txt", iScriptCounter);

            string sMeta = CreateMetaContent(sPythonPath, sScriptFile, stopwatch);

            File.WriteAllText(Path.Combine(sTempDirectory, "DisplayContent", sOutputFile), output);
            File.WriteAllText(Path.Combine(sTempDirectory, "DisplayContent", sErrorFile), error);
            File.WriteAllText(Path.Combine(sTempDirectory, "DisplayContent", sMetaFile), sMeta);

            return 0;
        }

        [DllExport("PySessionTempLocation", CallingConvention = CallingConvention.StdCall)]
        public static string PySessionTempLocation()
        {
            return TempWorkingDirectory();
        }

        [DllExport("PySetInputTable", CallingConvention = CallingConvention.StdCall)]
        public static int PySetInputTable(string sTableName, string sTableFile)
        {
            return 0;
        }

        [DllExport("PySetInputScalar", CallingConvention = CallingConvention.StdCall)]
        public static int PySetInputScalar(string sValueName, string sValue, string sValueType)
        {
            return 0;
        }

        [DllExport("PyGetInputTable", CallingConvention = CallingConvention.StdCall)]
        public static string PyGetInputTable(string sTableName)
        {
            return string.Empty;
        }

        [DllExport("PyGetInputScalar", CallingConvention = CallingConvention.StdCall)]
        public static string PyGetInputScalar(string sValueName)
        {
            return string.Empty;
        }

        [DllExport("PyGetLastError", CallingConvention = CallingConvention.StdCall)]
        public static string PyGetLastError()
        {
            if (stackErrors.Count > 0)
                return stackErrors.Pop();
            return string.Empty;
        }

        // -----------------------------------------

        static void CreateTempDirectories()
        {
            //sTempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            sTempDirectory = Path.Combine(Path.GetTempPath(), "SASnPyDebug");
            sTempDirectory = sTempDirectory.Replace('\\', '/');

            sTempDataPlotsDir = Path.Combine(sTempDirectory, "DisplayContent", "Plots");
            sTempDisplayConentDir = Path.Combine(sTempDirectory, "DisplayContent", "Plots");
            sTempDataInDir = Path.Combine(sTempDirectory, "DataIn");
            sTempDataOutDir = Path.Combine(sTempDirectory, "DataOut");

            try
            {
                Directory.CreateDirectory(sTempDirectory);
                Directory.CreateDirectory(sTempDataInDir);
                Directory.CreateDirectory(sTempDataOutDir);
                Directory.CreateDirectory(sTempDisplayConentDir);
                Directory.CreateDirectory(sTempDataPlotsDir);
            }
            catch(Exception ex)
            {
                stackErrors.Push("Failed to created temp directories: " + ex.Message);
                sTempDirectory = string.Empty;
                sTempDataPlotsDir = string.Empty;
                sTempDisplayConentDir = string.Empty;
                sTempDataInDir = string.Empty;
                sTempDataOutDir = string.Empty;
            }
        }

        static string TempWorkingDirectory()
        {
            if (string.IsNullOrEmpty(sTempDirectory))
            {
                CreateTempDirectories();
            }
            return sTempDirectory;
        }


        public static int ExecuteScript(string sScript)
        {
            string sScriptFile = sScript;
            string output = string.Empty;
            string error = string.Empty;

            sbOutputStream = new StringBuilder();
            sbErrorStream = new StringBuilder();
            Stopwatch stopwatch = new Stopwatch();

            string sHTMLOutputDir = @"C:/GHRepositories/sasnpy/HTMLOutput";
            string sPythonScriptDir = @"C:/GHRepositories/sasnpy/Python-Scripts";

            string dllPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string sTempDir = TempWorkingDirectory();

            string sTempDirPlots = Path.Combine(sTempDir, "DisplayContent", "Plots");
            string sTempDirDisplayContent = Path.Combine(sTempDir, "DisplayContent", "Plots");
            string sTempDirDataIn = Path.Combine(sTempDir, "DataIn");
            string sTempDirDataOut = Path.Combine(sTempDir, "DataOut");

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
                    //process.StartInfo.Arguments = sScriptFile;

                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.OutputDataReceived += Process_OutputDataReceived;
                    process.ErrorDataReceived += Process_ErrorDataReceived;

                    stopwatch.Start();

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    StreamWriter processInput = process.StandardInput;

                    processInput.WriteLine("import sys");
                    processInput.WriteLine("sys.path.append('{0}')", dllPath.Replace('\\', '/'));
                    processInput.WriteLine("sys.path.append('{0}')", sPythonScriptDir);
                    processInput.WriteLine("import sasnpy");
                    processInput.WriteLine("sasnpy.sasnpy_tempdir_datain = '{0}'", sTempDirDataIn.Replace('\\', '/'));
                    processInput.WriteLine("sasnpy.sasnpy_tempdir_dataout = '{0}'", sTempDirDataOut.Replace('\\', '/'));
                    processInput.WriteLine("sasnpy.sasnpy_tempdir_displaycontent = '{0}'", sTempDirDisplayContent.Replace('\\', '/'));
                    processInput.WriteLine("sasnpy.sasnpy_tempdir_plots = '{0}'", sTempDirPlots.Replace('\\', '/'));

                    processInput.WriteLine("sasnpy.run_script('{0}')", sScriptFile);

                    processInput.WriteLine("exit()");
                    processInput.Flush();
                    processInput.Close();

                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    sbErrorStream.AppendLine(ex.Message);
                }
                finally
                {
                    stopwatch.Stop();
                    output = sbOutputStream.ToString();
                    error = sbErrorStream.ToString().Trim();
                }
            }

            string sMeta = CreateMetaContent(sPythonPath, sScriptFile, stopwatch);

            File.WriteAllText(Path.Combine(sTempDirectory, "DisplayContent", "meta.txt"), sMeta);
            File.WriteAllText(Path.Combine(sTempDirectory, "DisplayContent", "output.txt"), output);
            File.WriteAllText(Path.Combine(sTempDirectory, "DisplayContent", "error.txt"), error);

            string htmlReport = CreateHTMLReport(sHTMLOutputDir, output, error, sMeta);
            File.WriteAllText(Path.Combine(sTempDirectory, "DisplayContent", "report.html"), htmlReport);
            File.Copy(Path.Combine(sHTMLOutputDir, "sasnpy.css"), Path.Combine(sTempDirectory, "DisplayContent", "sasnpy.css"), true);

            return string.IsNullOrWhiteSpace(error) ? 0 : 1;
        }

        //private static StringWriter CreateHTMLReport(string sOutput, string sError, string sPythonPath, string sScriptFile, Stopwatch stopwatch)
        //{
        //    StringWriter stringWriter = new StringWriter();
        //    HtmlTextWriter htmlWriter = new HtmlTextWriter(stringWriter, string.Empty);

        //    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Html);

        //    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Head);
        //    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Title);
        //    htmlWriter.Write("SASnPy");
        //    htmlWriter.RenderEndTag();  // HtmlTextWriterTag.Title
        //    htmlWriter.RenderEndTag();  // HtmlTextWriterTag.Head

        //    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Body);

        //    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Hr);
        //    htmlWriter.RenderEndTag();  // HtmlTextWriterTag.hr

        //    if (!string.IsNullOrWhiteSpace(sOutput))
        //    {
        //        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
        //        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Pre);
        //        htmlWriter.Write(AddPlotContent(sOutput));
        //        htmlWriter.RenderEndTag();  // HtmlTextWriterTag.Pre
        //        htmlWriter.RenderEndTag();  // HtmlTextWriterTag.Div
        //    }

        //    if (!string.IsNullOrWhiteSpace(sError))
        //    {
        //        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Bgcolor, "LightPink");
        //        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
        //        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Pre);
        //        htmlWriter.Write(sError);
        //        htmlWriter.RenderEndTag();  // HtmlTextWriterTag.Pre
        //        htmlWriter.RenderEndTag();  // HtmlTextWriterTag.Div
        //    }

        //    StringBuilder sMeta = new StringBuilder();
        //    sMeta.AppendLine("Python Path: " + sPythonPath);
        //    sMeta.AppendLine("Script Path: " + sScriptFile);
        //    sMeta.AppendLine("Execution Took: " + GetTimeElapsed(stopwatch));

        //    htmlWriter.AddAttribute(HtmlTextWriterAttribute.Bgcolor, "LightGrey");
        //    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
        //    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Pre);
        //    htmlWriter.Write(sMeta.ToString());
        //    htmlWriter.RenderEndTag();  // HtmlTextWriterTag.Pre
        //    htmlWriter.RenderEndTag();  // HtmlTextWriterTag.Div


        //    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Hr);
        //    htmlWriter.RenderEndTag();  // HtmlTextWriterTag.hr
        //    htmlWriter.RenderEndTag();  // HtmlTextWriterTag.Body
        //    htmlWriter.RenderEndTag();  // HtmlTextWriterTag.Html

        //    return stringWriter;
        //}

        private static String CreateHTMLReport(string sDLLPath, string sOutput, string sError, string sMeta)
        {
            string sReport = File.ReadAllText(Path.Combine(sDLLPath, "sasnpy.htm"));

            if (!string.IsNullOrWhiteSpace(sOutput))
                sReport = sReport.Replace("OUTPUTCONTENT", AddPlotContent(sOutput));
            else
                sReport = sReport.Replace("OUTPUTCONTENT", string.Empty);

            if (!string.IsNullOrWhiteSpace(sError))
                sReport = sReport.Replace("ERRORCONTENT", sError);
            else
                sReport = sReport.Replace("ERRORCONTENT", string.Empty);

            sReport = sReport.Replace("METACONTENT", sMeta.ToString());

            return sReport;
        }

        private static string CreateMetaContent(string sPythonPath, string sScriptFile, Stopwatch stopwatch)
        {
            StringBuilder sMeta = new StringBuilder();
            sMeta.AppendLine("Python Path: " + sPythonPath);
            sMeta.AppendLine("Script Path: " + sScriptFile);
            sMeta.AppendLine("Execution Took: " + GetTimeElapsed(stopwatch));
            return sMeta.ToString();
        }

        private static string AddPlotContent(string sOrigContent)
        {
            if (string.IsNullOrEmpty(sPlotTemplate))
            {
                StringWriter stringWriter = new StringWriter();
                HtmlTextWriter htmlWriter = new HtmlTextWriter(stringWriter, string.Empty);
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Src, "${imagefile}");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Img);
                htmlWriter.RenderEndTag(); 
                htmlWriter.RenderEndTag();
                sPlotTemplate = stringWriter.ToString();
            }

            string pattern = @"{{IMAGE[\|]{2}(?<imagefile>.*)}}";
            string replacePattern = sPlotTemplate;
            string sModifiedContent = Regex.Replace(sOrigContent, pattern, replacePattern, RegexOptions.IgnoreCase);
            return sModifiedContent;
        }

        private static string GetTimeElapsed(Stopwatch stopWatch)
        {
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            return elapsedTime;
        }

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            sbErrorStream.AppendLine(e.Data);
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            sbOutputStream.AppendLine(e.Data);
        }
    }

}
