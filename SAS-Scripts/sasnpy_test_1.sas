%include 'C:\GHRepositories\sasnpy\SAS-Scripts\sasnpy.sas';

%initsasnpy(C:\GHRepositories\sasnpy\SASnPy\bin\x64\Debug);

data _null_;

pi_res = sasnpy_pi();	
tempdir_res = sasnpy_workingdir();

put "PI : " pi_res;
put "TempDir : " tempdir_res;

call sasnpy_pypath("C:/Python/Python3.6/Python.exe");
ex_res = sasnpy_execute("C:/GHRepositories/sasnpy/TestScripts/pySample1.py");
put "Exit Code : " ex_res;


run;
