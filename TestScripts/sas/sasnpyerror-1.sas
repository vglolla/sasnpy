dm 'log;clear;';

%include 'C:/Experiments/sasnpy-x64/sasnpy.sas';

%PyInitialize("C:/Experiments/sasnpy-x64/");

/* %PySetPath("C:/Python/Python3.6/Python.exe"); */
%PySetPath("C:/Python/Anaconda2/python.exe");


data _null_;

	%PyStartSession();

	%PyExecuteScript('C:/GHRepositories/sasnpy/TestScripts/python/pyError1.py');

	%PyEndSession();

run;

