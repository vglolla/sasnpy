using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.InteropServices;
using RGiesecke.DllExport;

namespace SASnPy
{
    public class SASnPyHelper
    {
        [DllExport("SessionTempLocation", CallingConvention = CallingConvention.StdCall)]
        public static string SessionTempLocation()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        [DllExport("TestPI", CallingConvention = CallingConvention.StdCall)]
        public static double pi()
        {
            return Math.PI;
        }
    }
}
