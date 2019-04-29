dm 'log;clear;'; dm 'odsresults; clear';

/* Include sasnpy.sas */
%include 'C:/sasnpy/sasnpy.sas';

/* Initialize sasnpy */
%PyInitialize("C:/sasnpy/");

/* Set Python Path to use */
%PySetPath("C:/Python/Python3.7/Python.exe");

data _null_;

	%PyStartSession();

	%PySetInputScalar("m_from_sas", 10);
	%PySetInputScalar("n_from_sas", 20);

	%PyExecuteScript('C:/GitHub/sasnpy/TestScripts/python/ScalarDataTransferExample-1.py');

	%PyGetOutputScalar("answer_to_everything", ansev);
	%PyGetOutputScalar("whereami", currloc);

	put "ANSEV : " ansev;
	put "CURRLOC : " currloc;

	%PyEndSession();

run;



