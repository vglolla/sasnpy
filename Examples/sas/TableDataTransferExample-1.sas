dm 'log;clear;'; dm 'odsresults; clear';

/* Include sasnpy.sas */
%include 'C:/sasnpy/sasnpy.sas';

/* Initialize sasnpy */
%PyInitialize("C:/sasnpy/");

/* Set Python Path to use */
%PySetPath("C:/Python/Python3.7/Python.exe");

data _null_;

	%PyStartSession();

	%PySetInputTable("cars", sashelp.cars);
	%PySetInputTable("baseball", sashelp.baseball);

	%PyExecuteScript('C:/GitHub/sasnpy/TestScripts/python/TableDataTransferExample-1.py');

	%PyGetOutputTable("age_df", work.agepy);
	%PyGetOutputTable("code_df", work.codepy);

	%PyEndSession();

run;

proc print data = work.agepy; run;
proc print data = work.codepy; run;


