
# function to print multiplication table
def print_mult_table(m, n):
	for i in range(0, n + 1):
		print("%d x %d = %d" % (i, m, i * m))
		
		
		
# print multiplication table 
# based on numbers from SAS

mi = int(m_from_sas)
ni = int(n_from_sas)

print_mult_table(mi, ni)

# prepare scalars for SAS

answer_to_everything = 42
whereami = "sasgf2019"