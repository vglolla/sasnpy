%let sasnpydllpath=;

%macro PyInitialize(path);
%let sasnpydllpath=%str(%sysfunc(dequote(&path)));
%put &sasnpydllpath;

PROC PROTO package = Work.sasnpy.dotnet
		   label = "SASnPY .NET Module"
		   stdcall;

LINK "&sasnpydllpath/sasnpy.dll"; 


int PyStartSession() label="Start a new Python session";
int PyEndSession() label="End current Python session";
int PySetPath(char *) label="Set path to Python executable";
int PyExecuteScript(char *) label="Execute script";
char * PySessionTempLocation(void) label="Get temp working directory";
int PySetInputTable(char *, char *) label="Set input table";
int PySetInputScalar(char *, char *, char *) label="Set input value";
char * PyGetOutputTable(char *) label="Get output table";
char * PyGetOutputScalar(char *) label="Get output scalar";
char * PyGetLastError() label="Get last error";

RUN;

/* --------------------------------------
   -------------------------------------- */

PROC FCMP inlib = Work.sasnpy outlib = Work.sasnpy.wrapper;

FUNCTION sasnpy_startsession();
	x = PyStartSession();
	return(x);
ENDSUB;

FUNCTION sasnpy_endsession();
	x = PyEndSession();
	return(x);
ENDSUB;

FUNCTION sasnpy_setpath(path $);
	x = PySetPath(TRIM(path));
	return(x);
ENDSUB;

FUNCTION sasnpy_executescript(script $);
	x = PyExecuteScript(TRIM(script));
	return(x);
ENDSUB;

FUNCTION sasnpy_sessiontemplocation() $ 1024;
	length x $1024;
	x = TRIM(PySessionTempLocation());
	return(x);
ENDSUB;

FUNCTION sasnpy_setinputable(tablename $, tablefile $);
	x = PySetInputTable(TRIM(tablename), TRIM(tablefile));
	return(x);
ENDSUB;

FUNCTION sasnpy_setinputscalar(scalarname $, scalarvalue $, scalartype $);
	x = PySetInputScalar(TRIM(scalarname), TRIM(scalarvalue), TRIM(scalartype));
	return(x);
ENDSUB;

FUNCTION sasnpy_getoutputtable(tablename $) $ 1024;
	length x $1024;
	x = TRIM(PyGetOutputTable(TRIM(tablename)));
	return(x);
ENDSUB;

FUNCTION sasnpy_getoutputscalar(scalarname $) $ 1024;
	length x $1024;
	x = TRIM(PyGetOutputScalar(TRIM(scalarname)));
	return(x);
ENDSUB;

FUNCTION sasnpy_getlasterror() $ 2048;
	length x $ 2048;
	x = TRIM(PyGetLastError());
	return(x);
ENDSUB;

/* ---------------------------------- */



QUIT;

options append=(cmplib=Work.sasnpy);

%mend;

%macro PySetPath(path);
data _null_;
x = sasnpy_setpath(&path);
run;
%mend;

%macro PyStartSession();
data _null_;
x = sasnpy_startsession();
run;
%mend;

%macro PyEndSession();
data _null_;
x = sasnpy_endsession();
run;
%mend;

%macro PySetInputTable(name, ds);

data _null_;
	length tempfile $ 2048;
	length tempdir $ 1024;
	length tempname $ 36;
	tempname = uuidgen(); 
	tempdir = sasnpy_sessiontemplocation();
	tempfile = TRIM(tempdir) || '/DataIn/' || TRIM(tempname) ||'.csv';
	call symput("sasnpydifile", "'"||TRIM(tempfile)||"'");
run;

proc export data=&ds
	outfile = &sasnpydifile
	dbms = csv
	replace;
run;

data _null_;
	x = sasnpy_setinputable(&name, &sasnpydifile);
run;

%mend;

%macro PySetInputScalar(name, value);
data _null_;
%if (%datatyp(&value)=NUMERIC) %then %do;
   x = sasnpy_setinputscalar(&name, "'"||&value||"''", "float");
%end;
%else %do;
   x = sasnpy_setinputscalar(&name, &value, "str");
%end;
run;
%mend;

%macro PyExecuteScript(script);
data _null_;
x = sasnpy_executescript(&script);
run;
%mend;

/*

%macro PyResultsPath();
sasnpy_workingdir();
%mend;


*/
