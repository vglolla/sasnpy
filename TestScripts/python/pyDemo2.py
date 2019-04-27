import pandas as pd

# Induced from SAS
# air: pandas data frame
# baseball: pandas data frame
# max_iter: numeric scalar
# some_number: numeric scalar
# myname: string scalar

# use data tables induced from SAS
air.describe()
baseball.describe()

# use scalars induced from SAS
print('max_iter', max_iter)
print('some_number', some_number)
print('myname', myname)

# automatically captured:
#  -- any console output 
#  -- any matplotlib plots 

data1 = {'Name': ['Alpha', 'Bravo', 'Charlie'], 'Age': [22, 35, 29]}

data2 = {'Name': ['Alpha', 'Bravo', 'Charlie'], 'Code': ['AG', 'BT', 'CQ']}		

# make some data tables in Python		
cars_dup = pd.DataFrame.from_dict(data1)
baseball_dup = pd.DataFrame.from_dict(data2)

# set some scalars in Python
some_str = "Hello"
some_num = 123.45

# Available for retrieval from SAS
# cars_dup, baseball_dup: data frames
# some_str, some_num: scalars
