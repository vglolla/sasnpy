%let dllpath=;

%macro initsasnpy(path);
%let dllpath=&path;

PROC PROTO package = Work.sasnpy.dotnet
		   label = "SASnPY .NET Module"
		   stdcall;

LINK "&dllpath\sasnpy.dll";

char * SessionTempLocation(void) label="Get temp working directory";
double TestPI(void) label="Get me PI";
void SetPythonPath(char *) label="Set path to Python executable";
int ExecuteScript(char *) label="Execute script";

RUN;

/* --------------------------------------
   -------------------------------------- */

PROC FCMP inlib = Work.sasnpy outlib = Work.sasnpy.wrapper;

FUNCTION sasnpy_workingdir() $ 1024;
	length x $1024;
	x = SessionTempLocation();
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
