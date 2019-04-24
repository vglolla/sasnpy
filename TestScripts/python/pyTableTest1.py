def print_table(table):
	for x in table:
		print(x)

cars_dup = None
baseball_dup = None

if 'cars' in locals():
	print_table(cars)
	cars_dup = cars
	
if 'baseball' in locals():
	print_table(baseball)
	baseball_dup = baseball

	
	
	