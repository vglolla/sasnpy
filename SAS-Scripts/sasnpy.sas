%let sasnpydllpath=;
%let sasnpy_option_htmloutput=1;

%macro PyInitialize(path);
%let sasnpydllpath=%str(%sysfunc(dequote(&path)));

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
char * PyGetOutputScalarElement(char *, char *) label="Get output scalar components";
char * PyGetLastError() label="Get last error";
char * PyGetOutputHTMLFile(int ) label = "Get output HTML file";
char * PyGetMetaDataFile(int ) label = "Get meta output file";
int PyInjectHTMLOutput(char *, char *) label = "Inject HTML content to file";

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

FUNCTION sasnpy_getoutputscalarElement(scalarfile $, componenttype $) $ 1024;
	length x $1024;
	x = TRIM(PyGetOutputScalarElement(TRIM(scalarfile), TRIM(componenttype)));
	return(x);
ENDSUB;

FUNCTION sasnpy_getlasterror() $ 2048;
	length x $ 2048;
	x = TRIM(PyGetLastError());
	return(x);
ENDSUB;

FUNCTION sasnpy_getoutputhtmlfile(sessionid) $ 2048;
	length x $ 2048;
	x = TRIM(PyGetOutputHTMLFile(sessionid));
	return(x);
ENDSUB;

FUNCTION sasnpy_getmetadatafile(sessionid) $ 2048;
	length x $ 2048;
	x = TRIM(PyGetMetaDataFile(sessionid));
	return(x);
ENDSUB;

FUNCTION sasnpy_injecthtmloutput(htmlfile $, sessionid $);
	x = PyInjectHTMLOutput(TRIM(htmlfile), TRIM(sessionid));
	return(x);
ENDSUB;

/* ---------------------------------- */



QUIT;

options append=(cmplib=Work.sasnpy);

PROC TEMPLATE;
	DEFINE style sasnpystyle;
		parent=styles.sasweb;
		class Table, Data, Header /
			borderwidth = 0 
			;
	END;
RUN;

%mend;

%macro PySetPath(path);
data _null_;
	x = sasnpy_setpath(&path);
run;
%mend;

%macro PyStartSession();
data _null_;
	x = sasnpy_startsession();
%mend;

%macro PyEndSession();
data _null_;
	x = sasnpy_endsession();
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

proc export data=&ds
	outfile = &sasnpydifile
	dbms = csv
	replace;
run;

data _null_;
	x = sasnpy_setinputable(&name, &sasnpydifile);

%mend;

%macro PyGetOutputTable(name, ds);
data _null_;
	length tempfile $ 1024;
	tempfile = sasnpy_getoutputtable(&name);
	if missing(tempfile) then do;
		errormsg = &name || 'not accessible.';
		put 'ERROR:' errormsg;
	end;
	call symput("sasnpydofile", "'"||TRIM(tempfile)||"'");

proc import datafile=&sasnpydofile
	out=&ds
	dbms=csv
	replace;
run;

data _null_;

%mend;

%macro PySetInputScalar(name, value);
data _null_;
	%if (%datatyp(&value)=NUMERIC) %then %do;
	   x = sasnpy_setinputscalar(&name, STRIP(PUT(&value, best32.)), "float");

	%end;
	%else %do;
	   x = sasnpy_setinputscalar(&name, &value, "str");
	%end;
%mend;

%macro PyGetOutputScalar(name, varname);

	length tempfile $ 1024;
	length xtype $ 1024;
	length xvalue $ 1024;

	tempfile = sasnpy_getoutputscalar(&name);
	if missing(tempfile) then do;
		errormsg = &name || 'not accessible.';
		put 'ERROR:' errormsg;
	end;

	xtype = TRIM(sasnpy_getoutputscalarElement(TRIM(tempfile), "type"));
	xvalue = TRIM(sasnpy_getoutputscalarElement(TRIM(tempfile),"value"));
	length &varname $ 1024;
	&varname = xvalue;

%mend;

%macro PyExecuteScript(script);
data _null_;
	
	length htmlfile $ 2048;
	length metadatafile $ 2048;
	length tempdir $ 2048;

	sessionid = sasnpy_executescript(&script);
	tempdir = TRIM(sasnpy_sessiontemplocation());
	htmlfile = TRIM(tempdir) || '/DisplayContent/sashtml-' || STRIP(PUT(sessionid, best32.)) ||'.htm';
	metadatafile = sasnpy_getmetadatafile(sessionid);
	call symput("sasnpyhtmlfile", "'"||TRIM(htmlfile)||"'");
	call symput("sasnpymetadatafile", "'"||TRIM(metadatafile)||"'");
	call symput("sasnpysessionid", "'"||STRIP(PUT(sessionid, best32.))||"'");

	data _null_;
	run;

	title 'PyExecuteScript';

	filename hhandle &sasnpyhtmlfile;
	ods html3 body=hhandle style=sasnpystyle;
	ods html3 close;
	filename hhandle clear;


	data _null_;
		y = sasnpy_injecthtmloutput(&sasnpyhtmlfile, &sasnpysessionid);
	run;

	/*
	put "Injection : " y;
	%put HTML File : &sasnpyhtmlfile;
	%put Meta File : &sasnpymetadatafile;
	%put SessionID : &sasnpysessionid;
	*/

	filename hhandle &sasnpyhtmlfile mod;
	ods html3 body=hhandle style=sasnpystyle;

	filename mhandle &sasnpymetadatafile;

	data _null_;

		infile mhandle;
		input;
		title;
		footnote;
		file print;
		put _infile_;

	run;

	filename mhandle clear;

	ods html3 close;

	filename hhandle clear;

	ods html;

%mend;

