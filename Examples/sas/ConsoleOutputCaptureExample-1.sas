dm 'log;clear;'; dm 'odsresults; clear';

/* Include sasnpy.sas */
%include 'C:/sasnpy/sasnpy.sas';

/* Initialize sasnpy */
%PyInitialize("C:/sasnpy/");

/* Set Python Path to use */
%PySetPath("C:/Python/Python3.7/Python.exe");


data _null_;

	%PyStartSession();

	%PyExecuteScript('C:/GitHub/sasnpy/TestScripts/python/ConsoleOutputCaptureExample-1.py');

	%PyEndSession();

run;

