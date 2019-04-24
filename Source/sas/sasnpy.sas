/* --------------------------------------------------------------------------------- 
 * MIT License
 * 
 * Copyright(c) 2019; Venu Gopal Lolla
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *  
--------------------------------------------------------------------------------- */


/* ----------------------------------------------------------------------------------
This SAS script should be included in the user's SAS script
-----------------------------------------------------------------------------------*/

%let sasnpydllpath=;

/* ----------------------------------------------------------------------------------
Loads SASNPY.DLL and declares function prototypes for exported functions (PROC PROTO)
Defines wrapper functions to call functions exported by SASNPY.DLL (PROC FCMP)
-----------------------------------------------------------------------------------*/
%macro PyInitialize(path);
%let sasnpydllpath=%str(%sysfunc(dequote(&path)));

PROC PROTO package = Work.sasnpy.dotnet
		   label = "SASnPY .NET Module"
		   stdcall;

LINK "&sasnpydllpath/sasnpy.dll"; 

/* Prototypes for methods exported by SASNPY.DLL */

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

/* Wrapper functions to call methods in SASNPY.DLL */

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

/* ----------------------------------------------------------------------------------
Set the Python instance to use
-----------------------------------------------------------------------------------*/
%macro PySetPath(path);
data _null_;
	x = sasnpy_setpath(&path);
run;
%mend;

/* ----------------------------------------------------------------------------------
Starts a Python session
A session needs to be started before scripts can be submitted
-----------------------------------------------------------------------------------*/
%macro PyStartSession();
data _null_;
	x = sasnpy_startsession();
%mend;

/* ----------------------------------------------------------------------------------
End Python session
-----------------------------------------------------------------------------------*/
%macro PyEndSession();
data _null_;
	x = sasnpy_endsession();
	title;
%mend;

/* ----------------------------------------------------------------------------------
Save SAS dataset as CSV in temp location; passes the information through 
the pipeline to load the data in Python
-----------------------------------------------------------------------------------*/
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

/* ----------------------------------------------------------------------------------
Calls a method to save Python table as CSV; then loads CSV in SAS
-----------------------------------------------------------------------------------*/

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

/* ----------------------------------------------------------------------------------
Sets a scalar value in the user script's local namespace
-----------------------------------------------------------------------------------*/
%macro PySetInputScalar(name, value);
data _null_;
	%if (%datatyp(&value)=NUMERIC) %then %do;
	   x = sasnpy_setinputscalar(&name, STRIP(PUT(&value, best32.)), "float");

	%end;
	%else %do;
	   x = sasnpy_setinputscalar(&name, &value, "str");
	%end;
%mend;

/* ----------------------------------------------------------------------------------
Gets a scalar value in the user script's local namespace; currently brings 
everything back as a string
-----------------------------------------------------------------------------------*/
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

/* ----------------------------------------------------------------------------------
Executes a user's Python script; creates a HTML file (using ODS); invokes a method
in SASNPY.DLL to inject HTML output from Python script into the ODS HTML file
-----------------------------------------------------------------------------------*/
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
	call symput("sasnpysessiontitle", "'sasnpy-"||STRIP(PUT(sessionid, best32.))||"'");

	data _null_;
	run;

	filename hhandle &sasnpyhtmlfile;
	ods html3 body=hhandle style=sasnpystyle;
	ods html3 close;
	filename hhandle clear;


	data _null_;
		y = sasnpy_injecthtmloutput(&sasnpyhtmlfile, &sasnpysessionid);
	run;

	filename hhandle &sasnpyhtmlfile mod;
	ods html3 body=hhandle style=sasnpystyle;

	filename mhandle &sasnpymetadatafile;

	data _null_;

		infile mhandle;
		input;
		title &sasnpysessiontitle;
		footnote;
		file print;
		put _infile_;

	run;

	filename mhandle clear;

	ods html3 close;

	filename hhandle clear;

	ods html;

%mend;

