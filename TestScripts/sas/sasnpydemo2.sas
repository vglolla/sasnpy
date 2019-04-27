/* Include SASnPy helper script */
%include 'C:/Experiments/sasnpy-x64/sasnpy.sas';

/* Initialize/define function required for SAS-Python Integration */
%PyInitialize("C:/Experiments/sasnpy-x64/");

/* Set Python installation to use */
%PySetPath("C:/Python/Python3.6/Python.exe");

data _null_;

	/* Start Python session */
	%PyStartSession();

	/* Send data tables to Python */
	%PySetInputTable("cars", sashelp.cars);
	%PySetInputTable("baseball", sashelp.baseball);

	/* Send scalar data to Python */
	%PySetInputScalar("max_iter", 42);
	%PySetInputScalar("some_number", 123.4567);
	%PySetInputScalar("myname", "sasnpy");

	/* Execute script */
	%PyExecuteScript('C:/GHRepositories/sasnpy/TestScripts/python/pyDemo1.py');
	
	/* Get scalar data from Python */
	%PyGetOutputScalar("some_str", abc);
	%PyGetOutputScalar("some_num", def);

	put 'abc (some_str) = ' abc;
	put 'def (some_num) = ' def;
	
	/* Get data tables from Python */
	%PyGetOutputTable('cars_dup', work.carsdup);
	%PyGetOutputTable('baseball_dup', work.baseballdup);

	/* End Python session */
	%PyEndSession();

run;

proc print data=work.carsdup; run;
proc print data=work.baseballdup; run;

