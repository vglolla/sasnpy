dm 'log;clear;';

%include 'C:/GHRepositories\sasnpy/SAS-Scripts/sasnpy.sas';

%PyInitialize("C:/GHRepositories/sasnpy/SASnPy/bin/x64/Debug");

%PySetPath("C:/Python/Python3.6/Python.exe");


data _null_;

	%PyStartSession();

	%PyExecuteScript('C:/GHRepositories/sasnpy/TestScripts/pyFigSample1.py');

	%PyExecuteScript('C:/GHRepositories/sasnpy/TestScripts/pyFigSample2.py');

	%PyEndSession();

run;

