dm 'log;clear;';

%include 'C:/GHRepositories/sasnpy/SAS-Scripts/sasnpy.sas';

%PyInitialize("C:/GHRepositories/sasnpy/SASnPy/bin/x64/Debug");

%PySetPath("C:/Python/Python3.6/Python.exe");

data _null_;

	%PyStartSession();

	%PySetInputScalar("max_iter", 42);
	%PySetInputScalar("some_number", 123.4567);
	%PySetInputScalar("myname", "sasnpy");

	%PyExecuteScript('C:/GHRepositories/sasnpy/TestScripts/pyScalarTest1.py');
	
	%PyGetOutputScalar("some_str", abc);
	%PyGetOutputScalar("some_num", def);

	put 'abc (some_str) = ' abc;
	put 'def (some_num) = ' def;

	%PyEndSession();

run;

