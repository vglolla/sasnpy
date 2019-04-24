dm 'log;clear;';

%include 'C:/GHRepositories/sasnpy/SAS-Scripts/sasnpy.sas';

%PyInitialize("C:/GHRepositories/sasnpy/SASnPy/bin/x64/Debug");

%PySetPath("C:/Python/Python3.6/Python.exe");


data _null_;

	%PyStartSession();

	%PyExecuteScript('C:/GHRepositories/sasnpy/TestScripts/pySample1.py');

	%PySetInputTable("cars", sashelp.cars);
	%PySetInputTable("baseball", sashelp.baseball);

	%PyExecuteScript('C:/GHRepositories/sasnpy/TestScripts/pyTableTest2.py');

	
	%PyEndSession();

run;

