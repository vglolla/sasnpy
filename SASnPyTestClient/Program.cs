using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SASnPy;

namespace SASnPyTestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            SASnPyHelper.SetPythonPath("C:/Python/Python3.6/Python.exe");
            SASnPyHelper.ExecuteScript("C:/GHRepositories/sasnpy/TestScripts/pySample1.py");
        }

    }

}
