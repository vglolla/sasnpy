dm 'log; clear;'; dm 'odsresults; clear';

/* Include sasnpy.sas */
%include 'C:/sasnpy/sasnpy.sas';

/* Initialize sasnpy */
%PyInitialize("C:/sasnpy/");

/* Set Python Path to use */
%PySetPath("C:/Python/Python3.7/Python.exe");


data _null_;

	%PyStartSession();

	%PyExecuteScript('C:/GitHub/sasnpy/TestScripts/python/SessionMaintenanceExample-1a.py');

	put "Just finished running SessionMaintenanceExample-1a.py";

	%PyExecuteScript('C:/GitHub/sasnpy/TestScripts/python/SessionMaintenanceExample-1b.py');

	put "Just finished running SessionMaintenanceExample-1b.py";

	%PyExecuteScript('C:/GitHub/sasnpy/TestScripts/python/SessionMaintenanceExample-1c.py');

	put "Just finished running SessionMaintenanceExample-1c.py";

	%PyEndSession();

run;

