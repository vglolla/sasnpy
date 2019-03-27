%let sasnpydllpath=;

%macro PyInit(path);
%let sasnpydllpath=%str(%sysfunc(dequote(&path)));
%put &sasnpydllpath;

PROC PROTO package = Work.sasnpy.dotnet
		   label = "SASnPY .NET Module"
		   stdcall;

LINK "&sasnpydllpath/sasnpy.dll"; 


char * SessionTempLocation(void) label="Get temp working directory";
double TestPI(void) label="Get me PI";
void SetPythonPath(char *) label="Set path to Python executable";
int ExecuteScript(char *) label="Execute script";
void SetInputTable(char *, char *) label="Set input table";
void SetInputValue(char *, char *, char *) label="Set input value";

RUN;

/* --------------------------------------
   -------------------------------------- */

PROC FCMP inlib = Work.sasnpy outlib = Work.sasnpy.wrapper;

FUNCTION sasnpy_workingdir() $ 1024;
	length x $1024;
	x = TRIM(SessionTempLocation());
	return(x);
ENDSUB;

FUNCTION sasnpy_pi();
	x = TestPI();
	return(x);
ENDSUB;

SUBROUTINE sasnpy_pypath(path $);
	CALL SetPythonPath(TRIM(path));
ENDSUB;

FUNCTION sasnpy_execute(script $);
	x = ExecuteScript(TRIM(script));
	return(x);
ENDSUB;

QUIT;

options append=(cmplib=Work.sasnpy);

%mend;

%macro PyExecute(script);
sasnpy_execute(&script);
%mend;

%macro PySetPath(path);
data _null_;
call sasnpy_pypath(&path);
run;
%mend;

%macro PyResultsPath();
sasnpy_workingdir();
%mend;

%macro PySetInputTable(name, ds);

data _null_;
length tempfile $ 2048;
length tempdir $ 1024;
length tempname $ 36;
tempname = uuidgen(); 
tempdir = sasnpy_workingdir();
tempfile = TRIM(tempdir) || '/DataIn/' || TRIM(tempname) ||'.xpt';
call symput("sasnpydifile", TRIM(tempfile));
run;

libname sasnpydi xport "&sasnpydifile";
data sasnpydi.data;
set &ds;
run;

%mend;


