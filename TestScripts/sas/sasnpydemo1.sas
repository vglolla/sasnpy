/* Include SASnPy helper script */
%include 'C:\SASnPy\sasnpy.sas';

/* Initialize/define function required for SAS-Python Integration */
%PyInitialize("C:\SASnPy\");

/* Set Python installation to use */
%PySetPath("C:/Python3.6/Python.exe");

data _null_;

/* Start Python session */
%PyStartSession();

/* Send data tables to Python */
%PySetInputTable("air_ds", sashelp.air);
%PySetInputTable("comet_ds", sashelp.comet);

/* Send scalar data to Python */
%PySetInputScalar("max_iter", 42);
%PySetInputScalar("my_name", "sasnpy");

/* Execute script - 1 */
%PyExecute("C:/scripts/test.py", result);

/* Get data tables from Python */
%PyGetOutputTable("py_ds1", work.pyd1);
%PyGetOutputTable("py_ds2", work.pyd2);

/* Get scalar data from Python */
%PyGetOutputScalar("py_res1", abc);
%PyGetOutputScalar("py_res2", xyz);

put "abc = " abc;
put "xyz = " xyz;

/* End Python session */
%PyEndSession();

run;

proc print data=work.pyd1; run;
proc print data=work.pyd2; run;

