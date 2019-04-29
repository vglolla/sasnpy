import pandas as pd
import numpy as np

# use tables from SAS
print(cars)
print(baseball)

data1 = {'Name' : ['Alpha', 'Bravo', 'Charlie'], 'Age' : [22, np.nan, 29]}
data2 = {'Name' : ['Alpha', 'Bravo', 'Charlie'], 'Code' : ['AG', 'BT', 'CQ']}

# make data tables in Python
age_df = pd.DataFrame.from_dict(data1);
code_df = pd.DataFrame.from_dict(data2);