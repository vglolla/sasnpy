
# function to print multiplication table
def print_mult_table(m, n):
	for i in range(0, n + 1):
		print("%d x %d = %d" % (i, m, i * m))
		

def function_a(m, n):
	i_like_42
	print_mult_table(m, n)
	
def function_b(m, n):
	function_a(m, n)
	
def function_c(m, n):
	function_b(m, n)
		
function_b(5, 3)
