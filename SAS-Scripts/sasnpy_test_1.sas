%include 'C:\Experiments\SASPython\SASScripts\sasnpy.sas';

%initsasnpy(C:\Experiments\SASPython\sasnpy-1\sasnpy-1\bin\x64\Debug);

data _null_;

pi_res = sasnpy_pi();	
tempdir_res = sasnpy_workingdir();

put "PI : " pi_res;
put "TempDir : " tempdir_res;

run;
