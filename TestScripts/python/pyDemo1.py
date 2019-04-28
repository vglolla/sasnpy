def print_table(table):
	for x in table:
		print(x)

cars_dup = None
baseball_dup = None

print('max_iter', max_iter)
print('some_number', some_number)
print('myname', myname)

if 'cars' in locals():
	print(cars)
	cars_dup = cars
	
if 'baseball' in locals():
	print(baseball)
	baseball_dup = baseball

some_str = "Hello"
some_num = 123.45	
	
	