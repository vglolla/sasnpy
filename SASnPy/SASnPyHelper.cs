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
        static string sTempDirectory = string.Empty;
        static string sPlotTemplate = string.Empty;

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
                //sTempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                sTempDirectory = Path.Combine(Path.GetTempPath(), "SASnPyDebug");
                sTempDirectory = sTempDirectory.Replace('\\', '/');
                //if (Directory.Exists(sTempDirectory))
                //    Directory.Delete(sTempDirectory, true);

                Directory.CreateDirectory(sTempDirectory);
                Directory.CreateDirectory(Path.Combine(sTempDirectory, "DataIn"));
                Directory.CreateDirectory(Path.Combine(sTempDirectory, "DataOut"));
                Directory.CreateDirectory(Path.Combine(sTempDirectory, "DisplayContent"));
                Directory.CreateDirectory(Path.Combine(sTempDirectory, "DisplayContent", "Plots"));
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

        [DllExport("SetInputTable", CallingConvention = CallingConvention.StdCall)]
        public static void SetInputTable(string sTableName, string sTableFile)
        {
        }

        [DllExport("SetInputValue", CallingConvention = CallingConvention.StdCall)]
        public static void SetInputValue(string sValueName, string sValue, string sValueType)
        {
        }

        [DllExport("ExecuteScript", CallingConvention = CallingConvention.StdCall)]
        public static int ExecuteScript(string sScript)
        {
            string sScriptFile = sScript;
            string output = string.Empty;
            string error = string.Empty;

            outputstream = new StringBuilder();
            errorstream = new StringBuilder();
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
                    errorstream.AppendLine(ex.Message);
                }
                finally
                {
                    stopwatch.Stop();
                    output = outputstream.ToString();
                    error = errorstream.ToString().Trim();
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
            errorstream.AppendLine(e.Data);
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            outputstream.AppendLine(e.Data);
        }
    }

}
