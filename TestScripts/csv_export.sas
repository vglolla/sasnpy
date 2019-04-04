proc export data=sashelp.Baseball
	outfile = 'C:\temp\Baseball.csv'
	dbms = csv
	replace;
run;

proc export data=sashelp.cars
	outfile = 'C:\temp\cars.csv'
	dbms = csv
	replace;
run;

