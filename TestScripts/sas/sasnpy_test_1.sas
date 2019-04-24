dm 'log;clear;';

%include "C:/GHRepositories\sasnpy/SAS-Scripts/sasnpy.sas";

%PyInit("C:\GHRepositories/sasnpy\SASnPy/bin\x64/Debug");
/*%PyInit("C:/GHRepositories/sasnpy/SASnPy/bin/x64/Debug");*/
%PySetPath("C:\Python/Python3.6\Python.exe");

data _null_;

tempdir_res = %PyResultsPath();
/* put "TempDir : " tempdir_res; */


%PySetInputTable("name", sashelp.air);
%PySetInputTable("tame", sashelp.comet);


/*

ex_res = %PyExecute("C:/GHRepositories/sasnpy/TestScripts/pySample1.py");
put "Exit Code : " ex_res;

*/


run;
