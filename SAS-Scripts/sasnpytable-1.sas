dm 'log;clear;';

%include 'C:/GHRepositories\sasnpy/SAS-Scripts/sasnpy.sas';

%PyInitialize("C:/GHRepositories/sasnpy/SASnPy/bin/x64/Debug");

%PySetPath("C:/Python/Python3.6/Python.exe");


data _null_;

	%PyStartSession();

	%PySetInputTable("cars", sashelp.cars);
	%PySetInputTable("baseball", sashelp.baseball);

	%PyExecuteScript('C:/GHRepositories/sasnpy/TestScripts/pyTableTest2.py');

	%PyGetOutputTable('cars_dup', work.carsdup);
	%PyGetOutputTable('baseball_dup', work.baseballdup);

	%PyEndSession();

run;

proc print data=work.carsdup;
run;

proc print data=work.baseballdup;
run;

