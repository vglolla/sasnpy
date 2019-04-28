dm 'log;clear;';

%include 'C:/Experiments/sasnpy-x64/sasnpy.sas';

%PyInitialize("C:/Experiments/sasnpy-x64/");

%PySetPath("C:/Python/Python3.6/Python.exe");


data _null_;


	%PyStartSession();

	%PySetInputScalar("max_iter", 42);
	%PySetInputScalar("some_number", 123.4567);
	%PySetInputScalar("myname", "sasnpy");

	%PyExecuteScript('C:/GHRepositories/sasnpy/TestScripts/python/pyScalarTest1.py');
	
	%PyGetOutputScalar("some_str", abc);
	%PyGetOutputScalar("some_num", def);

	put 'abc (some_str) = ' abc;
	put 'def (some_num) = ' def;

	%PyEndSession();

run;

/*
proc print data=work.carsdup;
run;

proc print data=work.baseballdup;
run;
*/

