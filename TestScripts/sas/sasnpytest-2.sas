dm 'log;clear;';

%include 'C:/Experiments/sasnpy-x64/sasnpy.sas';

%PyInitialize("C:/Experiments/sasnpy-x64/");

%PySetPath("C:/Python/Python3.6/Python.exe");


data _null_;

	%PyStartSession();

	%PyExecuteScript('C:/GHRepositories/sasnpy/TestScripts/python/pySample1.py');
	
	%PySetInputTable("cars", sashelp.cars);
	%PySetInputTable("baseball", sashelp.baseball);

	%PyExecuteScript('C:/GHRepositories/sasnpy/TestScripts/python/pyTableTest2.py');

	%PySetInputScalar("max_iter", 42);
	%PySetInputScalar("some_number", 123.4567);
	%PySetInputScalar("myname", "sasnpy");

	%PyExecuteScript('C:/GHRepositories/sasnpy/TestScripts/python/pyScalarTest1.py');
	
	%PyGetOutputTable('cars_dup', work.carsdup);
	%PyGetOutputTable('baseball_dup', work.baseballdup);

	%PyEndSession();

run;

proc print data=work.carsdup;
run;

proc print data=work.baseballdup;
run;

