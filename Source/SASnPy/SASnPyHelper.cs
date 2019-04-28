/* --------------------------------------------------------------------------------- 
 * MIT License
 * 
 * Copyright(c) 2019; Venu Gopal Lolla
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *  
--------------------------------------------------------------------------------- */


using System;
using System.IO;
using System.Text;
using System.Web.UI;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using RGiesecke.DllExport;

namespace SASnPy
{
    public class SASnPyHelper
    {
        #region Fields

        static string sPythonPath = string.Empty;
        static string sPlotTemplate = string.Empty;

        static string sTempDirectory = string.Empty;
        static string sTempDataInDir = string.Empty;
        static string sTempDataOutDir = string.Empty;
        static string sTempDisplayContentDir = string.Empty;
        static string sTempDataPlotsDir = string.Empty;

        static long iScriptTimeOutInSeconds = 3600;

        static StringBuilder sbOutputStream;
        static StringBuilder sbErrorStream;
        static int iScriptCounter = 0;
        static Process procObj = null;

        static string sDLLPath;
        static bool bSessionActive = false;

        static Stack<string> stackErrors;
        static StreamWriter procInputStream;
        static object streamLock = new object();

        static bool bSkipOneLogEntry = false;
        static bool bProduceLog;
        static bool bUseRandomTempDir;
        static string sPresetWorkingDir;
        static string sLogFileName;

        const string errorPyPathNotSet = "Python path not set.";
        const string errorPyEXENotFound = "Python interpreter not found at specified path : {0}.";
        const string errorPySessionNotStarted = "Python session not started.";
        const string errorPyScriptTimedOut = "Python script execution timed out. Current Maximum Time : {0} seconds.";
        const string errorPyInjectHTMLFailed = "Failed to inject HTML for session into SAS ODS HTML report.";
        const string errorPyHTMLTemplateNotFound = "Could not find HTML template file : {0}.";

        const string sInfoPythonPath = "Python Path: {0}";
        const string sInfoScriptPath = "Script Path: {0}";
        private const string sInfoExecutionTime = "Execution Took: {0}";


        #endregion Fields


        /// <summary>
        /// The methods having the DLLExport attribute become available as C-style functions through the DLL through "UnmanagedExports" magic.
        /// However,  runtime errors occur if one of these "decorated" functions has to call another "decorated" function.
        /// Hence all the decorated methods are merely wrapper around *Core() methods that conduct the actual work.
        /// </summary>
        /// <returns></returns>

        #region Exported Methods

        [DllExport("PyStartSession", CallingConvention = CallingConvention.StdCall)]
        public static int PyStartSession()
        {
            return PyStartSessionCore();
        }

        [DllExport("PyEndSession", CallingConvention = CallingConvention.StdCall)]
        public static int PyEndSession()
        {
            return PyEndSessionCore();
        }

        [DllExport("PySetPath", CallingConvention = CallingConvention.StdCall)]
        public static int PySetPath(string sPath)
        {
            return PySetPathCore(sPath);
        }

        [DllExport("PyExecuteScript", CallingConvention = CallingConvention.StdCall)]
        public static int PyExecuteScript(string sScript)
        {
            return PyExecuteScriptCore(sScript);
        }

        [DllExport("PySessionTempLocation", CallingConvention = CallingConvention.StdCall)]
        public static string PySessionTempLocation()
        {
            return PySessionTempLocationCore();
        }

        [DllExport("PySetInputTable", CallingConvention = CallingConvention.StdCall)]
        public static int PySetInputTable(string sTableName, string sTableFile)
        {
            return PySetInputTableCore(sTableName, sTableFile);
        }

        [DllExport("PySetInputScalar", CallingConvention = CallingConvention.StdCall)]
        public static int PySetInputScalar(string sScalarName, string sScalarValue, string sScalarType)
        {
            return PySetInputScalarCore(sScalarName, sScalarValue, sScalarType);
        }

        [DllExport("PyGetOutputTable", CallingConvention = CallingConvention.StdCall)]
        public static string PyGetOutputTable(string sTableName)
        {
            return PyGetOutputTableCore(sTableName);
        }

        [DllExport("PyGetOutputScalar", CallingConvention = CallingConvention.StdCall)]
        public static string PyGetOutputScalar(string sScalarName)
        {
            return PyGetOutputScalarCore(sScalarName);
        }

        [DllExport("PyGetOutputScalarElement", CallingConvention = CallingConvention.StdCall)]
        public static string PyGetOutputScalarElement(string sFilename, string sComponent)
        {
            return PyGetOutputScalarElementCore(sFilename, sComponent);
        }

        [DllExport("PyGetOutputHTMLFile", CallingConvention = CallingConvention.StdCall)]
        public static string PyGetOutputHTMLFile(int iSessionID)
        {
            return PyGetOutputHTMLFileCore(iSessionID);
        }

        [DllExport("PyGetMetaDataFile", CallingConvention = CallingConvention.StdCall)]
        public static string PyGetMetaDataFile(int iSessionID)
        {
            return PyGetMetaDataFileCore(iSessionID);
        }

        [DllExport("PyInjectHTMLOutput", CallingConvention = CallingConvention.StdCall)]
        public static int PyInjectHTMLOutput(string sHTMLFileName, string sSessionID)
        {
            return PyInjectHTMLOutputCore(sHTMLFileName, sSessionID);
        }

        [DllExport("PyGetLastError", CallingConvention = CallingConvention.StdCall)]
        public static string PyGetLastError()
        {
            return PyGetLastErrorCore();
        }

        #endregion Exported Methods


        /// <summary>
        /// The *Core() methods do the actual work.
        /// </summary>
        /// <returns></returns>

        #region Core Methods

        // Start a Python session by starting a new Python process
        // Established handlers to capture output and error streams
        // Injects initial startup Python statements to the interpreter
        // including loading the sasnpy module
        static int PyStartSessionCore()
        {
            stackErrors.Clear();

            string sSettingsFile = Path.Combine(sDLLPath, "settings.xml");
            ConfigureDefaultSettings();
            if (File.Exists(sSettingsFile))
                ProcessSettingsFile(sSettingsFile);


            CreateTempDirectories();

            if (string.IsNullOrEmpty(sPythonPath))
            {
                LogError(new Exception(errorPyPathNotSet));
                return 1;
            }

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

            procInputStream = procObj.StandardInput;
            procInputStream.AutoFlush = true;

            procInputStream.WriteLine("import sys");
            procInputStream.WriteLine("sys.path.append('{0}')", sDLLPath.Replace('\\', '/'));
            procInputStream.WriteLine("import sasnpy");
            procInputStream.WriteLine("sasnpy._sasnpy_symbol_map['tempdir_datain'] = '{0}'", sTempDataInDir.Replace('\\', '/'));
            procInputStream.WriteLine("sasnpy._sasnpy_symbol_map['tempdir_dataout'] = '{0}'", sTempDataOutDir.Replace('\\', '/'));
            procInputStream.WriteLine("sasnpy._sasnpy_symbol_map['tempdir_displaycontent'] = '{0}'", sTempDisplayContentDir.Replace('\\', '/'));
            procInputStream.WriteLine("sasnpy._sasnpy_symbol_map['tempdir_plots'] = '{0}'", sTempDataPlotsDir.Replace('\\', '/'));

            procInputStream.Flush();
            bSessionActive = true;

            bSkipOneLogEntry = true;
            PyExecuteScriptCore("pass");

            return 0;
        }

        // End a Python session by killing the currently active Python process
        static int PyEndSessionCore()
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

        // Set Python interpreter to be used
        static int PySetPathCore(string sPath)
        {
            if (!File.Exists(sPath))
            {
                string sErrorMsg = string.Format(errorPyEXENotFound, sPath);
                LogError(new Exception(sErrorMsg));
                return 1;
            }

            sPythonPath = sPath;
            return 0;
        }

        // Execute a Python script in the current session
        // Capture output and error streams to disk
        static int PyExecuteScriptCore(string sScript)
        {
            if (!bSessionActive)
            {
                LogError(new Exception(errorPySessionNotStarted));
                return 1;
            }

            string sScriptFile = sScript;
            string output = string.Empty;
            string error = string.Empty;
            bool bCleanupCodeFile = false;

            sbOutputStream = new StringBuilder();
            sbErrorStream = new StringBuilder();

            Stopwatch stopwatch = new Stopwatch();

            if (!File.Exists(sScriptFile))
            {
                string sTempFile = GetRandomFileName(sTempDirectory);
                File.WriteAllText(sTempFile, sScript);
                sScriptFile = sTempFile;
                bCleanupCodeFile = true;
            }

            string sSentinelFile = GetRandomFileName(sTempDirectory);

            try
            {
                stopwatch.Start();
                procInputStream.WriteLine("sasnpy.execute_script('{0}', '{1}')", sScriptFile, sSentinelFile);
                procInputStream.WriteLine(procInputStream.NewLine);
                procInputStream.Flush();

                WaitForSentinelFile(sSentinelFile);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            finally
            {
                stopwatch.Stop();
                output = sbOutputStream.ToString();
                error = bSkipOneLogEntry ? string.Empty : sbErrorStream.ToString().Trim();
                if (bSkipOneLogEntry)
                    bSkipOneLogEntry = false;

                if (File.Exists(sSentinelFile))
                    File.Delete(sSentinelFile);
                if (bCleanupCodeFile)
                    File.Delete(sScriptFile);
            }


            iScriptCounter++;
            string sOutputFile = string.Format("output-{0}.txt", iScriptCounter);
            string sErrorFile = string.Format("error-{0}.txt", iScriptCounter);
            string sMetaFile = string.Format("meta-{0}.txt", iScriptCounter);

            string sMeta = CreateMetaContent(sPythonPath, sScriptFile, stopwatch);

            File.WriteAllText(Path.Combine(sTempDirectory, "DisplayContent", sOutputFile), output);
            File.WriteAllText(Path.Combine(sTempDirectory, "DisplayContent", sErrorFile), error);
            File.WriteAllText(Path.Combine(sTempDirectory, "DisplayContent", sMetaFile), sMeta);

            CreateOutputHTML(output, error, iScriptCounter);

            bool bErrorEmpty = string.IsNullOrWhiteSpace(error);
            if (!bErrorEmpty)
                LogError(new Exception(error));

            return iScriptCounter;
        }

        // Get the working directory being used by the current session
        static string PySessionTempLocationCore()
        {
            if (string.IsNullOrEmpty(sTempDirectory))
            {
                CreateTempDirectories();
            }
            return sTempDirectory;
        }

        // Inject table into Python
        static int PySetInputTableCore(string sTableName, string sTableFile)
        {
            if (!bSessionActive)
            {
                LogError(new Exception(errorPySessionNotStarted));
                return 1;
            }

            string sSentinelFile = GetRandomFileName(sTempDirectory);

            procInputStream.WriteLine("sasnpy.set_input_table('{0}', '{1}', '{2}')", sTableName, sTableFile, sSentinelFile);
            procInputStream.WriteLine(procInputStream.NewLine);
            procInputStream.Flush();

            WaitForSentinelFile(sSentinelFile);

            return 0;
        }

        // Inject scalarinto Python
        static int PySetInputScalarCore(string sScalarName, string sScalarValue, string sScalarType)
        {
            if (!bSessionActive)
            {
                LogError(new Exception(errorPySessionNotStarted));
                return 1;
            }

            string sSentinelFile = GetRandomFileName(sTempDirectory);
            string sFileName = GetRandomFileName(sTempDataInDir, "xml");

            XElement xEl = new XElement("object", sScalarValue);
            xEl.Add(new XAttribute("name", sScalarName));
            xEl.Add(new XAttribute("type", sScalarType));
            File.WriteAllText(sFileName, xEl.ToString());

            procInputStream.WriteLine("sasnpy.set_input_scalar('{0}', '{1}')", sFileName, sSentinelFile);
            procInputStream.WriteLine(procInputStream.NewLine);
            procInputStream.Flush();

            WaitForSentinelFile(sSentinelFile);

            return 0;
        }

        // Retrieve table from Python
        static string PyGetOutputTableCore(string sTableName)
        {
            if (!bSessionActive)
            {
                LogError(new Exception(errorPySessionNotStarted));
                return string.Empty;
            }

            string sSentinelFile = GetRandomFileName(sTempDirectory);
            string sTableFile = GetRandomFileName(sTempDataOutDir, "csv");

            procInputStream.WriteLine("sasnpy.get_output_table('{0}', '{1}', '{2}')", sTableName, sTableFile, sSentinelFile);
            procInputStream.WriteLine(procInputStream.NewLine);
            procInputStream.Flush();

            WaitForSentinelFile(sSentinelFile);

            return File.Exists(sTableFile) ? sTableFile : string.Empty;
        }

        // Retrieve scalar from Python
        static string PyGetOutputScalarCore(string sScalarName)
        {
            if (!bSessionActive)
            {
                LogError(new Exception(errorPySessionNotStarted));
                return string.Empty;
            }

            string sSentinelFile = GetRandomFileName(sTempDirectory);
            string sFileName = GetRandomFileName(sTempDataOutDir, "xml");

            procInputStream.WriteLine("sasnpy.get_output_scalar('{0}', '{1}', '{2}')", sScalarName, sFileName, sSentinelFile);
            procInputStream.WriteLine(procInputStream.NewLine);
            procInputStream.Flush();

            WaitForSentinelFile(sSentinelFile);

            return File.Exists(sFileName) ? sFileName : string.Empty;
        }

        // Helper function to parse and return individual elements of a scalar return file
        static string PyGetOutputScalarElementCore(string sFilename, string sComponent)
        {
            if (!bSessionActive)
            {
                LogError(new Exception(errorPySessionNotStarted));
                return string.Empty;
            }

            try
            {
                sFilename = sFilename.Trim();
                sComponent = sComponent.Trim();
                XDocument xDoc = XDocument.Parse(File.ReadAllText(sFilename));
                if (string.Compare(sComponent, "type", true) == 0)
                {
                    XAttribute attr = xDoc.Root.Attribute("type");
                    return attr != null ? attr.Value : string.Empty;
                }

                if (string.Compare(sComponent, "value", true) == 0)
                {
                    return xDoc.Root.Value;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            return string.Empty;
        }

        // Get HTML file containing output and error streams
        static string PyGetOutputHTMLFileCore(int iScriptID)
        {
            string sOutputHTMLFile = Path.Combine(sTempDisplayContentDir, string.Format("output-{0}.htm", iScriptID));
            return File.Exists(sOutputHTMLFile) ? sOutputHTMLFile : string.Empty;
        }

        // Get metadata file (text file) for script run
        static string PyGetMetaDataFileCore(int iScriptID)
        {
            string sMetaDataFile = Path.Combine(sTempDisplayContentDir, string.Format("meta-{0}.txt", iScriptID));
            return File.Exists(sMetaDataFile) ? sMetaDataFile : string.Empty;
        }

        // Append/inject HTML content from script run into an existing HTML file
        public static int PyInjectHTMLOutputCore(string sHTMLFileName, string sScriptID)
        {
            int iSessionID = int.Parse(sScriptID);
            int retValue = iSessionID;

            try
            {
                string sSessionHTMLFile = PyGetOutputHTMLFileCore(iSessionID);
                if (File.Exists(sSessionHTMLFile))
                {
                    int iAttempt = 0;
                    int maxAttempts = 20;
                    bool bInjectSucceeded = false;
                    while (iAttempt <= maxAttempts)
                    {
                        try
                        {
                            iAttempt++;
                            File.AppendAllText(sHTMLFileName, File.ReadAllText(sSessionHTMLFile));
                            bInjectSucceeded = true;
                            break;
                        }
                        catch (System.IO.IOException)
                        {
                            bInjectSucceeded = false;
                            System.Threading.Thread.Sleep(1000);
                        }
                    }

                    if (!bInjectSucceeded)
                        LogError(new Exception(errorPyInjectHTMLFailed));
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                retValue = -iSessionID;
            }

            return retValue;
        }

        // Return last error recorded on the error stack
        static string PyGetLastErrorCore()
        {
            if (stackErrors.Count > 0)
                return stackErrors.Pop();
            return string.Empty;
        }

        #endregion Core Methods

        /// <summary>
        /// Various helper methods used by the *Core() methods.
        /// </summary>

        #region Helper Methods

        // Static constructor for initialization
        static SASnPyHelper()
        {
            sDLLPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            stackErrors = new Stack<string>();
        }

        // Setup default settings
        static void ConfigureDefaultSettings()
        {
            bProduceLog = false;
            bUseRandomTempDir = true;
            sPresetWorkingDir = string.Empty;
            sLogFileName = string.Empty;
            iScriptTimeOutInSeconds = 3600;
        }

        // Process settings file if found
        static void ProcessSettingsFile(string sSettingsFile)
        {
            try
            {
                XDocument xDoc = XDocument.Load(sSettingsFile);

                XElement xElem1 = xDoc.Root.Element("WorkingDirectory");
                if (xElem1 != null)
                {
                    string sTempDirToUse = xElem1.Value;
                    if (!string.IsNullOrWhiteSpace(sTempDirToUse))
                    {
                        string sDir = Path.Combine(Path.GetTempPath(), sTempDirToUse);
                        if (!Directory.Exists(sDir))
                            Directory.CreateDirectory(sDir);
                        sPresetWorkingDir = sDir;
                        bUseRandomTempDir = false;
                    }
                }

                XElement xElem2 = xDoc.Root.Element("LogFile");
                if (xElem2 != null)
                {
                    string sTempName = xElem2.Value;
                    if (!string.IsNullOrWhiteSpace(sTempName))
                    {
                        sLogFileName = sTempName;
                        string sLogPath = Path.Combine(Path.GetTempPath(), sLogFileName);
                        File.AppendAllText(sLogPath, string.Empty);
                        File.Delete(sLogPath);
                        bProduceLog = true;
                    }
                }

                XElement xElem3 = xDoc.Root.Element("TimeOutInSeconds");
                if (xElem3 != null)
                {
                    long iTimeOut = 0;
                    string sTimeOut = xElem3.Value;
                    if (long.TryParse(sTimeOut, out iTimeOut))
                    {
                        iScriptTimeOutInSeconds = iTimeOut;
                    }
                }
            }
            catch
            {
            }
        }

        // Log error to stack and log file
        static void LogError(Exception ex)
        {
            if (bSkipOneLogEntry)
            {
                bSkipOneLogEntry = false;
                return;
            }

            stackErrors.Push(ex.Message);
            if (bProduceLog)
            {
                File.AppendAllText(sLogFileName, string.Format("{0}{1}", ex.ToString(), Environment.NewLine));
            }
        }

        // Gets a random file name
        static string GetRandomFileName(string sDirectory, string sPreferredExt = "")
        {
            string sFileName = Path.Combine(sDirectory, Path.GetRandomFileName()).Replace('\\', '/');
            if (!string.IsNullOrWhiteSpace(sPreferredExt))
            {
                sFileName = sFileName.Substring(0, sFileName.Length - 3) + sPreferredExt;
            }
            return sFileName;
        }

        // Saves error and output streams from a script run to disk in HTML form
        static int CreateOutputHTML(string sOutput, string sError, int iCounter)
        {
            string sHTMLOutputDir = sDLLPath;
            string sHTMLTemplateFile = Path.Combine(sHTMLOutputDir, "sasnpy.html");

            if (!File.Exists(sHTMLTemplateFile))
            {
                string sErrMsg = string.Format(errorPyHTMLTemplateNotFound, sHTMLTemplateFile);
                LogError(new Exception(sErrMsg));
            }

            string sReport = File.ReadAllText(sHTMLTemplateFile);

            if (!string.IsNullOrWhiteSpace(sOutput))
                sReport = sReport.Replace("OUTPUTCONTENT", AddPlotContent(sOutput));
            else
                sReport = sReport.Replace("OUTPUTCONTENT", string.Empty);

            if (!string.IsNullOrWhiteSpace(sError))
                sReport = sReport.Replace("ERRORCONTENT", sError);
            else
                sReport = sReport.Replace("ERRORCONTENT", string.Empty);

            string sHTMLOutputFile = Path.Combine(sTempDirectory, "DisplayContent", string.Format("output-{0}.htm", iCounter));
            File.WriteAllText(sHTMLOutputFile, sReport);

            return 0;
        }

        // Create a directory if not found
        static void CreateDirectoryIfNeeded(string sDir)
        {
            if (!Directory.Exists(sDir))
                Directory.CreateDirectory(sDir);
        }

        // Initialize working directory
        static void CreateTempDirectories()
        {
            if (bUseRandomTempDir)
                sTempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            else
                sTempDirectory = sPresetWorkingDir;

            sTempDirectory = sTempDirectory.Replace('\\', '/');

            sTempDataPlotsDir = Path.Combine(sTempDirectory, "DisplayContent", "Plots").Replace('\\', '/');
            sTempDisplayContentDir = Path.Combine(sTempDirectory, "DisplayContent").Replace('\\', '/');
            sTempDataInDir = Path.Combine(sTempDirectory, "DataIn").Replace('\\', '/');
            sTempDataOutDir = Path.Combine(sTempDirectory, "DataOut").Replace('\\', '/');

            try
            {
                CreateDirectoryIfNeeded(sTempDirectory);
                CreateDirectoryIfNeeded(sTempDataInDir);
                CreateDirectoryIfNeeded(sTempDataOutDir);
                CreateDirectoryIfNeeded(sTempDisplayContentDir);
                CreateDirectoryIfNeeded(sTempDataPlotsDir);

                if (bProduceLog)
                {
                    sLogFileName = Path.Combine(sTempDirectory, sLogFileName);
                    File.AppendAllText(sLogFileName, string.Empty);
                }
            }
            catch(Exception ex)
            {
                LogError(ex);
                sTempDirectory = string.Empty;
                sTempDataPlotsDir = string.Empty;
                sTempDisplayContentDir = string.Empty;
                sTempDataInDir = string.Empty;
                sTempDataOutDir = string.Empty;
            }
        }

        // Create metadata file for script run
        private static string CreateMetaContent(string sPythonPath, string sScriptFile, Stopwatch stopwatch)
        {
            StringBuilder sMeta = new StringBuilder();
            sMeta.AppendLine(string.Format(sInfoPythonPath, sPythonPath));
            sMeta.AppendLine(string.Format(sInfoScriptPath, sScriptFile));
            sMeta.AppendLine(string.Format(sInfoExecutionTime, GetTimeElapsed(stopwatch)));
            return sMeta.ToString();
        }

        // Inject plot paths into placeholders
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

        // Get elapsed time for script in string form
        private static string GetTimeElapsed(Stopwatch stopWatch)
        {
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            return elapsedTime;
        }

        // Error stream handler
        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            sbErrorStream.AppendLine(e.Data);
        }

        // Output stream handler
        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            sbOutputStream.AppendLine(e.Data);
        }

        // Wait for sentinel file; a naive way to block when synchronization is required
        static void WaitForSentinelFile(string sSentinelFile)
        {
            long maxInterval = iScriptTimeOutInSeconds == -1 ? -1 : iScriptTimeOutInSeconds * 1000;
            long waitTime = 0;

            while (!File.Exists(sSentinelFile) && (maxInterval == -1 || waitTime <= maxInterval))
            {
                waitTime += 500;
                System.Threading.Thread.Sleep(500);
            }

            if (File.Exists(sSentinelFile))
                File.Delete(sSentinelFile);
            else
            {
                string sErrorMsg = string.Format(errorPyScriptTimedOut, iScriptTimeOutInSeconds);
                LogError(new Exception(sErrorMsg));
            }
        }

        #endregion Helper Methods
    }
}
